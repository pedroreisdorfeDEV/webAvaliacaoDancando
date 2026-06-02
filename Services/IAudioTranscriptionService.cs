namespace WebAvaliacaoDancando.Services;

public interface IAudioTranscriptionService
{
    Task<string> TranscribeAsync(ConvertedAudioFile audioFile, CancellationToken cancellationToken = default);
}
