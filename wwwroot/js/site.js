(() => {
  const MAX_RECORDING_MS = 10 * 60 * 1000;
  let activeRecorder = null;

  document.querySelectorAll("[data-recorder]").forEach(setupRecorder);

  function setupRecorder(container) {
    const form = container.closest("[data-audio-form]");
    const fileInput = form?.querySelector("[data-audio-file-input]");
    const startButton = container.querySelector("[data-record-start]");
    const stopButton = container.querySelector("[data-record-stop]");
    const timerElement = container.querySelector("[data-record-timer]");
    const statusElement = container.querySelector("[data-transcription-status]");
    const previewElement = container.querySelector("[data-record-preview]");
    const saveButton = form?.querySelector("[data-save-button]");

    if (!form || !fileInput || !startButton || !stopButton || !timerElement || !statusElement || !previewElement || !saveButton) {
      return;
    }

    const hasExistingParecer = form.dataset.hasExistingParecer === "true";

    let mediaRecorder = null;
    let mediaStream = null;
    let audioChunks = [];
    let recordedFile = null;
    let recordingStartedAt = 0;
    let timerIntervalId = null;
    let stopTimeoutId = null;
    let pendingSubmit = false;

    const setIdleTimer = () => {
      timerElement.textContent = "00:00";
    };

    const formatElapsed = (elapsedMs) => {
      const totalSeconds = Math.floor(elapsedMs / 1000);
      const minutes = String(Math.floor(totalSeconds / 60)).padStart(2, "0");
      const seconds = String(totalSeconds % 60).padStart(2, "0");
      return `${minutes}:${seconds}`;
    };

    const setRecordedFile = (blob) => {
      const extension = blob.type.includes("wav") ? "wav" : blob.type.includes("mpeg") || blob.type.includes("mp3") ? "mp3" : "webm";
      const fileName = `parecer-${Date.now()}.${extension}`;
      recordedFile = new File([blob], fileName, { type: blob.type || "audio/webm" });

      const dataTransfer = new DataTransfer();
      dataTransfer.items.add(recordedFile);
      fileInput.files = dataTransfer.files;
    };

    const releaseStream = () => {
      mediaStream?.getTracks().forEach((track) => track.stop());
      mediaStream = null;
    };

    const resetRecordingState = () => {
      startButton.disabled = false;
      stopButton.disabled = true;
      saveButton.disabled = false;
      clearInterval(timerIntervalId);
      clearTimeout(stopTimeoutId);
      timerIntervalId = null;
      stopTimeoutId = null;
      mediaRecorder = null;
      recordingStartedAt = 0;

      if (activeRecorder === stopRecording) {
        activeRecorder = null;
      }
    };

    const stopRecording = () => {
      if (mediaRecorder && mediaRecorder.state !== "inactive") {
        mediaRecorder.stop();
      } else {
        releaseStream();
        resetRecordingState();
      }
    };

    startButton.addEventListener("click", async () => {
      if (activeRecorder && activeRecorder !== stopRecording) {
        activeRecorder();
      }

      if (!navigator.mediaDevices?.getUserMedia) {
        statusElement.textContent = "Esse navegador não permite capturar áudio.";
        return;
      }

      try {
        audioChunks = [];
        recordedFile = null;
        fileInput.value = "";
        pendingSubmit = false;

        mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        mediaRecorder = new MediaRecorder(mediaStream);

        mediaRecorder.ondataavailable = (event) => {
          if (event.data.size > 0) {
            audioChunks.push(event.data);
          }
        };

        mediaRecorder.onstop = () => {
          if (audioChunks.length > 0) {
            const audioBlob = new Blob(audioChunks, { type: mediaRecorder?.mimeType || "audio/webm" });
            setRecordedFile(audioBlob);
            previewElement.src = URL.createObjectURL(audioBlob);
            previewElement.classList.remove("d-none");
            statusElement.textContent = "Áudio anexado. Ao salvar, ele será enviado ao backend para transcrição.";
          } else {
            statusElement.textContent = "Nenhum áudio foi capturado.";
          }

          releaseStream();
          resetRecordingState();

          if (pendingSubmit) {
            pendingSubmit = false;
            form.requestSubmit();
          }
        };

        mediaRecorder.start();
        activeRecorder = stopRecording;
        startButton.disabled = true;
        stopButton.disabled = false;
        saveButton.disabled = true;
        recordingStartedAt = Date.now();
        timerElement.textContent = "00:00";
        statusElement.textContent = "Gravando áudio...";

        timerIntervalId = window.setInterval(() => {
          timerElement.textContent = formatElapsed(Date.now() - recordingStartedAt);
        }, 1000);

        stopTimeoutId = window.setTimeout(() => {
          statusElement.textContent = "Tempo máximo de 10 minutos atingido.";
          stopRecording();
        }, MAX_RECORDING_MS);
      } catch (error) {
        releaseStream();
        resetRecordingState();
        statusElement.textContent = "Não foi possível iniciar a gravação. Verifique a permissão do microfone.";
      }
    });

    stopButton.addEventListener("click", () => {
      stopRecording();
    });

    form.addEventListener("submit", (event) => {
      if (mediaRecorder && mediaRecorder.state !== "inactive") {
        event.preventDefault();
        pendingSubmit = true;
        statusElement.textContent = "Encerrando a gravação para enviar o áudio...";
        stopRecording();
        return;
      }

      if (!recordedFile && !fileInput.files?.length && !hasExistingParecer) {
        event.preventDefault();
        statusElement.textContent = "Grave o áudio do parecer antes de salvar.";
      }
    });

    setIdleTimer();
  }
})();
