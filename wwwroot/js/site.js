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
    const visualizerElement = container.querySelector("[data-voice-visualizer]");
    const waveformCanvas = container.querySelector("[data-voice-waveform]");
    const saveOverlay = form?.querySelector("[data-save-overlay]");
    const saveButton = form?.querySelector("[data-save-button]");
    const notaInput = form?.querySelector("[data-note-input]");

    if (!form || !fileInput || !playButton || !pauseButton || !stopButton || !timerElement || !badgeElement || !statusElement || !previewElement || !visualizerElement || !waveformCanvas || !saveOverlay || !saveButton || !notaInput) {
      return;
    }

    const waveformContext = waveformCanvas.getContext("2d");
    if (!waveformContext) {
      return;
    }

    const hasExistingParecer = form.dataset.hasExistingParecer === "true";
    const idleStatusText = hasExistingParecer
      ? "Pressione play para gravar um novo parecer."
      : "Pressione play para iniciar a gravacao.";
    const notaRegex = /^(10([.,]0+)?|[0-9]([.,][0-9]+)?)$/;

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
    let lastWaveformPoints = [];
    const saveButtonLabel = saveButton.textContent;

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

    const syncCanvasSize = () => {
      const rect = visualizerElement.getBoundingClientRect();
      const width = Math.max(1, Math.round(rect.width));
      const height = Math.max(1, Math.round(rect.height));
      const dpr = window.devicePixelRatio || 1;
      const nextWidth = Math.round(width * dpr);
      const nextHeight = Math.round(height * dpr);

      if (waveformCanvas.width !== nextWidth || waveformCanvas.height !== nextHeight) {
        waveformCanvas.width = nextWidth;
        waveformCanvas.height = nextHeight;
      }

      waveformContext.setTransform(dpr, 0, 0, dpr, 0, 0);
      return { width, height };
    };

    const getWaveColors = (state) => {
      switch (state) {
        case "recording":
          return {
            line: "#86f7b7",
            glow: "rgba(134, 247, 183, 0.22)",
            fill: "rgba(134, 247, 183, 0.08)"
          };
        case "paused":
          return {
            line: "#f6cb73",
            glow: "rgba(246, 203, 115, 0.18)",
            fill: "rgba(246, 203, 115, 0.08)"
          };
        case "ready":
          return {
            line: "#8fb1ff",
            glow: "rgba(143, 177, 255, 0.18)",
            fill: "rgba(143, 177, 255, 0.08)"
          };
        default:
          return {
            line: "rgba(236, 242, 248, 0.66)",
            glow: "rgba(255, 255, 255, 0.06)",
            fill: "rgba(255, 255, 255, 0.04)"
          };
      }
    };

    const drawWaveform = (points, state) => {
      const { width, height } = syncCanvasSize();
      const colors = getWaveColors(state);
      const centerY = height / 2;
      const leftPadding = 6;
      const drawableWidth = Math.max(1, width - (leftPadding * 2));

      waveformContext.clearRect(0, 0, width, height);
      waveformContext.lineCap = "round";
      waveformContext.lineJoin = "round";

      waveformContext.beginPath();
      waveformContext.strokeStyle = "rgba(208, 221, 234, 0.18)";
      waveformContext.lineWidth = 1;
      waveformContext.moveTo(leftPadding, centerY);
      waveformContext.lineTo(width - leftPadding, centerY);
      waveformContext.stroke();

      if (!points.length) {
        return;
      }

      waveformContext.strokeStyle = colors.line;
      waveformContext.lineWidth = 2;
      waveformContext.shadowBlur = 12;
      waveformContext.shadowColor = colors.glow;
      waveformContext.fillStyle = colors.fill;

      const bars = Math.min(points.length, 64);
      const step = drawableWidth / Math.max(bars - 1, 1);

      for (let index = 0; index < bars; index += 1) {
        const point = Math.abs(points[index] ?? 0);
        const amplitude = Math.max(3, point * height * 0.34);
        const x = leftPadding + (index * step);

        waveformContext.beginPath();
        waveformContext.moveTo(x, centerY - amplitude);
        waveformContext.lineTo(x, centerY + amplitude);
        waveformContext.stroke();
      }

      waveformContext.shadowBlur = 0;
    };

    const buildIdleWaveform = (state) => {
      const amplitude = state === "paused" ? 0.08 : state === "ready" ? 0.1 : 0.04;
      const points = [];

      for (let index = 0; index < 64; index += 1) {
        const progress = index / 63;
        const envelope = Math.sin(progress * Math.PI);
        const wave = Math.sin(progress * Math.PI * 5) * amplitude * envelope;
        const ripple = Math.sin(progress * Math.PI * 15) * (amplitude * 0.2) * envelope;
        points.push(wave + ripple);
      }

      return points;
    };

    const sampleWaveform = () => {
      if (!analyser || !analyserData) {
        return [];
      }

      analyser.getByteTimeDomainData(analyserData);
      const points = [];
      const totalPoints = 64;
      const stride = Math.max(1, Math.floor(analyserData.length / totalPoints));

      for (let index = 0; index < totalPoints; index += 1) {
        const sampleIndex = Math.min(analyserData.length - 1, index * stride);
        const normalized = (analyserData[sampleIndex] - 128) / 128;
        points.push(normalized * 0.95);
      }

      return points;
    };

    const renderCurrentWaveform = () => {
      const state = container.dataset.recordingState || "idle";

      if (state === "recording" && analyser) {
        lastWaveformPoints = sampleWaveform();
        drawWaveform(lastWaveformPoints, "recording");
        return;
      }

      if ((state === "paused" || state === "ready") && lastWaveformPoints.length > 0) {
        drawWaveform(lastWaveformPoints, state);
        return;
      }

      drawWaveform(buildIdleWaveform(state), state);
    };

    const stopVisualizerLoop = () => {
      if (visualizerFrameId) {
        window.cancelAnimationFrame(visualizerFrameId);
        visualizerFrameId = null;
      }
    };

    const drawVisualizer = () => {
      if (!analyser || mediaRecorder?.state !== "recording") {
        renderCurrentWaveform();
        return;
      }

      renderCurrentWaveform();
      visualizerFrameId = window.requestAnimationFrame(drawVisualizer);
    };

    const setRecorderState = (state, badgeText, statusText) => {
      container.dataset.recordingState = state;
      badgeElement.textContent = badgeText;
      statusElement.textContent = statusText;

      if (state !== "recording") {
        renderCurrentWaveform();
      }
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

    const setupVisualizer = async () => {
      if (!AudioContextConstructor || !mediaStream) {
        renderCurrentWaveform();
        return;
      }

      audioContext = new AudioContextConstructor();
      analyser = audioContext.createAnalyser();
      analyser.fftSize = 2048;
      analyser.smoothingTimeConstant = 0.82;
      analyserData = new Uint8Array(analyser.fftSize);
      sourceNode = audioContext.createMediaStreamSource(mediaStream);
      sourceNode.connect(analyser);
      await audioContext.resume();
    };

    const clearPreview = () => {
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
        objectUrl = null;
      }

      previewElement.removeAttribute("src");
      previewElement.classList.add("d-none");
    };

    const lockFormForSubmit = () => {
      form.dataset.submitting = "true";
      form.classList.add("is-submitting");
      form.setAttribute("aria-busy", "true");
      saveOverlay.hidden = false;
      saveButton.textContent = "Salvando...";

      // Do not disable posted fields here, otherwise the browser omits them
      // from the multipart payload, including the antiforgery token.
      form.querySelectorAll("button").forEach((element) => {
        if (!(element instanceof HTMLElement)) {
          return;
        }

        if (!element.hasAttribute("data-was-disabled")) {
          element.setAttribute("data-was-disabled", element.disabled ? "true" : "false");
        }

        element.disabled = true;
      });
    };

    const validateNota = () => {
      const valorInformado = notaInput.value.trim();

      if (!valorInformado) {
        notaInput.setCustomValidity("Informe uma nota entre 0 e 10.");
        return false;
      }

      if (!notaRegex.test(valorInformado)) {
        notaInput.setCustomValidity("Informe uma nota decimal entre 0 e 10, usando virgula ou ponto.");
        return false;
      }

      notaInput.setCustomValidity("");
      return true;
    };

    const unlockFormIfNeeded = () => {
      form.dataset.submitting = "false";
      form.classList.remove("is-submitting");
      form.removeAttribute("aria-busy");
      saveOverlay.hidden = true;
      saveButton.textContent = saveButtonLabel;

      form.querySelectorAll("[data-was-disabled]").forEach((element) => {
        if (!(element instanceof HTMLElement)) {
          return;
        }

        const wasDisabled = element.getAttribute("data-was-disabled") === "true";
        element.disabled = wasDisabled;
        element.removeAttribute("data-was-disabled");
      });
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
      setRecorderState(
        recordedFile ? "ready" : "idle",
        recordedFile ? "Audio pronto" : "Pronto",
        recordedFile ? statusAfterStop : idleStatusText
      );
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
      lastWaveformPoints = buildIdleWaveform("recording");
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
        drawVisualizer();
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
      setRecorderState("recording", "Gravando", "Gravacao retomada. Seguimos do ponto em que voce pausou.");
      syncButtons();
      drawVisualizer();
    };

    const pauseRecording = () => {
      if (!mediaRecorder || mediaRecorder.state !== "recording") {
        return;
      }

      recordingElapsedMs += Date.now() - recordingStartedAt;
      recordingStartedAt = 0;
      mediaRecorder.pause();
      stopVisualizerLoop();
      renderCurrentWaveform();
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
      renderCurrentWaveform();
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

    notaInput.addEventListener("input", validateNota);
    notaInput.addEventListener("blur", validateNota);

    form.addEventListener("submit", (event) => {
      if (form.dataset.submitting === "true") {
        event.preventDefault();
        return;
      }

      if (mediaRecorder && mediaRecorder.state !== "inactive") {
        event.preventDefault();
        pendingSubmit = true;
        stopRecording("Encerrando a gravacao para enviar o audio...");
        return;
      }

      if (!recordedFile && !fileInput.files?.length && !hasExistingParecer) {
        event.preventDefault();
        setRecorderState("idle", "Obrigatorio", "Grave o audio do parecer antes de salvar.");
        unlockFormIfNeeded();
        return;
      }

      if (!validateNota()) {
        event.preventDefault();
        unlockFormIfNeeded();
        notaInput.reportValidity();
        return;
      }

      if (typeof form.checkValidity === "function" && !form.checkValidity()) {
        unlockFormIfNeeded();
        return;
      }

      lockFormForSubmit();
    });

    window.addEventListener("resize", renderCurrentWaveform);

    clearPreview();
    unlockFormIfNeeded();
    enterIdleState();
    renderCurrentWaveform();
  }
})();
