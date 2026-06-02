(() => {
  const MAX_RECORDING_MS = 5 * 60 * 1000;
  const AudioContextConstructor = window.AudioContext || window.webkitAudioContext;
  let activeRecorder = null;

  document.querySelectorAll("[data-recorder]").forEach(setupRecorder);

  function setupRecorder(container) {
    const form = container.closest("[data-audio-form]");
    const fileInput = form?.querySelector("[data-audio-file-input]");
    const playButton = container.querySelector("[data-record-play]");
    const pauseButton = container.querySelector("[data-record-pause]");
    const stopButton = container.querySelector("[data-record-stop]");
    const timerElement = container.querySelector("[data-record-timer]");
    const badgeElement = container.querySelector("[data-record-badge]");
    const statusElement = container.querySelector("[data-transcription-status]");
    const previewElement = container.querySelector("[data-record-preview]");
    const visualizerBars = Array.from(container.querySelectorAll("[data-visualizer-bar]"));
    const saveButton = form?.querySelector("[data-save-button]");

    if (!form || !fileInput || !playButton || !pauseButton || !stopButton || !timerElement || !badgeElement || !statusElement || !previewElement || !saveButton) {
      return;
    }

    const hasExistingParecer = form.dataset.hasExistingParecer === "true";
    const idleStatusText = hasExistingParecer
      ? "Pressione play para gravar um novo parecer."
      : "Pressione play para iniciar a gravacao.";

    let mediaRecorder = null;
    let mediaStream = null;
    let audioChunks = [];
    let recordedFile = null;
    let recordingElapsedMs = 0;
    let recordingStartedAt = 0;
    let timerIntervalId = null;
    let pendingSubmit = false;
    let statusAfterStop = "Audio pronto para envio.";
    let objectUrl = null;
    let audioContext = null;
    let analyser = null;
    let analyserData = null;
    let sourceNode = null;
    let visualizerFrameId = null;

    const formatElapsed = (elapsedMs) => {
      const totalSeconds = Math.floor(elapsedMs / 1000);
      const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
      const seconds = String(totalSeconds % 60).padStart(2, "0");
      return `${minutes}:${seconds}`;
    };

    const getElapsedMs = () => {
      if (mediaRecorder?.state === "recording") {
        return recordingElapsedMs + (Date.now() - recordingStartedAt);
      }

      return recordingElapsedMs;
    };

    const updateTimer = () => {
      timerElement.textContent = formatElapsed(getElapsedMs());
    };

    const setRecorderState = (state, badgeText, statusText) => {
      container.dataset.recordingState = state;
      badgeElement.textContent = badgeText;
      statusElement.textContent = statusText;
    };

    const syncButtons = () => {
      const state = mediaRecorder?.state ?? "inactive";
      const isRecording = state === "recording";
      const isPaused = state === "paused";

      playButton.disabled = isRecording;
      pauseButton.disabled = !isRecording;
      stopButton.disabled = !(isRecording || isPaused);
      saveButton.disabled = isRecording || isPaused;
    };

    const resetVisualizer = () => {
      visualizerBars.forEach((bar, index) => {
        const baseScale = 0.16 + ((index % 4) * 0.025);
        bar.style.setProperty("--bar-scale", baseScale.toFixed(3));
      });
    };

    const stopVisualizerLoop = () => {
      if (visualizerFrameId) {
        window.cancelAnimationFrame(visualizerFrameId);
        visualizerFrameId = null;
      }
    };

    const drawVisualizer = () => {
      if (!analyser || mediaRecorder?.state !== "recording") {
        resetVisualizer();
        return;
      }

      analyser.getByteFrequencyData(analyserData);
      const average = analyserData.reduce((sum, value) => sum + value, 0) / Math.max(analyserData.length, 1);
      const averageLevel = average / 255;

      visualizerBars.forEach((bar, index) => {
        const sample = analyserData[index % analyserData.length] / 255;
        const wave = (Math.sin((Date.now() / 130) + index) + 1) / 2;
        const scale = Math.min(1, 0.18 + sample * 0.72 + averageLevel * 0.45 + wave * 0.08);
        bar.style.setProperty("--bar-scale", scale.toFixed(3));
      });

      visualizerFrameId = window.requestAnimationFrame(drawVisualizer);
    };

    const setupVisualizer = async () => {
      if (!AudioContextConstructor || !mediaStream) {
        resetVisualizer();
        return;
      }

      audioContext = new AudioContextConstructor();
      analyser = audioContext.createAnalyser();
      analyser.fftSize = 64;
      analyser.smoothingTimeConstant = 0.8;
      analyserData = new Uint8Array(analyser.frequencyBinCount);
      sourceNode = audioContext.createMediaStreamSource(mediaStream);
      sourceNode.connect(analyser);
      await audioContext.resume();
      drawVisualizer();
    };

    const clearPreview = () => {
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
        objectUrl = null;
      }

      previewElement.removeAttribute("src");
      previewElement.classList.add("d-none");
    };

    const setRecordedFile = (blob) => {
      const extension = blob.type.includes("wav")
        ? "wav"
        : blob.type.includes("mpeg") || blob.type.includes("mp3")
          ? "mp3"
          : blob.type.includes("mp4") || blob.type.includes("m4a")
            ? "m4a"
            : "webm";
      const fileName = `parecer-${Date.now()}.${extension}`;
      recordedFile = new File([blob], fileName, { type: blob.type || "audio/webm" });

      const dataTransfer = new DataTransfer();
      dataTransfer.items.add(recordedFile);
      fileInput.files = dataTransfer.files;
    };

    const cleanupAudioGraph = async () => {
      stopVisualizerLoop();
      resetVisualizer();
      sourceNode?.disconnect();
      sourceNode = null;
      analyser = null;
      analyserData = null;

      if (audioContext) {
        try {
          await audioContext.close();
        } catch {
          // ignore close errors from disposed contexts
        }

        audioContext = null;
      }
    };

    const releaseStream = async () => {
      mediaStream?.getTracks().forEach((track) => track.stop());
      mediaStream = null;
      await cleanupAudioGraph();
    };

    const resetRecorderInternals = () => {
      window.clearInterval(timerIntervalId);
      timerIntervalId = null;
      mediaRecorder = null;
      recordingStartedAt = 0;
      recordingElapsedMs = 0;

      if (activeRecorder === stopRecording) {
        activeRecorder = null;
      }
    };

    const enterIdleState = () => {
      timerElement.textContent = "00:00";
      setRecorderState(recordedFile ? "ready" : "idle", recordedFile ? "Audio pronto" : "Pronto", recordedFile ? statusAfterStop : idleStatusText);
      syncButtons();
    };

    const createMediaRecorder = () => {
      const mimeTypes = [
        "audio/webm;codecs=opus",
        "audio/webm",
        "audio/mp4"
      ];

      const supportedMimeType = mimeTypes.find((mimeType) => window.MediaRecorder?.isTypeSupported?.(mimeType));
      return supportedMimeType ? new MediaRecorder(mediaStream, { mimeType: supportedMimeType }) : new MediaRecorder(mediaStream);
    };

    const startTimerLoop = () => {
      window.clearInterval(timerIntervalId);
      timerIntervalId = window.setInterval(() => {
        const elapsedMs = getElapsedMs();
        timerElement.textContent = formatElapsed(elapsedMs);

        if (elapsedMs >= MAX_RECORDING_MS) {
          statusAfterStop = "Tempo maximo de 5 minutos atingido.";
          stopRecording();
        }
      }, 200);
    };

    const startNewRecording = async () => {
      if (activeRecorder && activeRecorder !== stopRecording) {
        activeRecorder("Gravacao encerrada porque outra captura foi iniciada.");
      }

      if (!navigator.mediaDevices?.getUserMedia || !window.MediaRecorder) {
        setRecorderState("idle", "Indisponivel", "Esse navegador nao permite capturar audio.");
        return;
      }

      clearPreview();
      audioChunks = [];
      recordedFile = null;
      fileInput.value = "";
      pendingSubmit = false;
      recordingElapsedMs = 0;
      recordingStartedAt = 0;
      statusAfterStop = "Audio pronto para envio.";
      updateTimer();

      try {
        mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        mediaRecorder = createMediaRecorder();

        mediaRecorder.ondataavailable = (event) => {
          if (event.data.size > 0) {
            audioChunks.push(event.data);
          }
        };

        mediaRecorder.onstop = async () => {
          const finalMimeType = mediaRecorder?.mimeType || "audio/webm";
          const shouldSubmit = pendingSubmit;

          if (audioChunks.length > 0) {
            const audioBlob = new Blob(audioChunks, { type: finalMimeType });
            setRecordedFile(audioBlob);
            objectUrl = URL.createObjectURL(audioBlob);
            previewElement.src = objectUrl;
            previewElement.classList.remove("d-none");
          }

          await releaseStream();
          resetRecorderInternals();

          if (!recordedFile) {
            setRecorderState("idle", "Sem audio", "Nenhum audio foi capturado.");
            syncButtons();
          } else {
            enterIdleState();
          }

          pendingSubmit = false;

          if (shouldSubmit) {
            form.requestSubmit();
          }
        };

        await setupVisualizer();
        mediaRecorder.start();
        activeRecorder = stopRecording;
        recordingStartedAt = Date.now();
        startTimerLoop();
        setRecorderState("recording", "Gravando", "Captando sua voz agora. Fale normalmente.");
        syncButtons();
      } catch {
        await releaseStream();
        resetRecorderInternals();
        setRecorderState("idle", "Falha", "Nao foi possivel iniciar a gravacao. Verifique a permissao do microfone.");
        syncButtons();
      }
    };

    const resumeRecording = async () => {
      if (!mediaRecorder || mediaRecorder.state !== "paused") {
        return;
      }

      if (audioContext?.state === "suspended") {
        await audioContext.resume();
      }

      recordingStartedAt = Date.now();
      mediaRecorder.resume();
      startTimerLoop();
      drawVisualizer();
      setRecorderState("recording", "Gravando", "Gravacao retomada. Seguimos do ponto em que voce pausou.");
      syncButtons();
    };

    const pauseRecording = () => {
      if (!mediaRecorder || mediaRecorder.state !== "recording") {
        return;
      }

      recordingElapsedMs += Date.now() - recordingStartedAt;
      recordingStartedAt = 0;
      mediaRecorder.pause();
      stopVisualizerLoop();
      resetVisualizer();
      updateTimer();
      setRecorderState("paused", "Pausado", "Gravacao pausada. Pressione play para continuar ou stop para encerrar.");
      syncButtons();
    };

    const stopRecording = (message = "Audio pronto para envio.") => {
      statusAfterStop = message;

      if (!mediaRecorder || mediaRecorder.state === "inactive") {
        releaseStream().finally(() => {
          resetRecorderInternals();
          enterIdleState();
        });
        return;
      }

      if (mediaRecorder.state === "recording") {
        recordingElapsedMs += Date.now() - recordingStartedAt;
        recordingStartedAt = 0;
      }

      window.clearInterval(timerIntervalId);
      timerIntervalId = null;
      stopVisualizerLoop();
      resetVisualizer();
      mediaRecorder.stop();
      setRecorderState("ready", "Finalizando", "Encerrando gravacao e preparando o audio...");
      syncButtons();
    };

    playButton.addEventListener("click", async () => {
      if (mediaRecorder?.state === "paused") {
        await resumeRecording();
        return;
      }

      if (mediaRecorder?.state === "recording") {
        return;
      }

      await startNewRecording();
    });

    pauseButton.addEventListener("click", pauseRecording);

    stopButton.addEventListener("click", () => {
      stopRecording();
    });

    form.addEventListener("submit", (event) => {
      if (mediaRecorder && mediaRecorder.state !== "inactive") {
        event.preventDefault();
        pendingSubmit = true;
        stopRecording("Encerrando a gravacao para enviar o audio...");
        return;
      }

      if (!recordedFile && !fileInput.files?.length && !hasExistingParecer) {
        event.preventDefault();
        setRecorderState("idle", "Obrigatorio", "Grave o audio do parecer antes de salvar.");
      }
    });

    clearPreview();
    resetVisualizer();
    enterIdleState();
  }
})();
