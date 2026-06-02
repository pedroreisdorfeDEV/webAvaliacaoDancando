namespace WebAvaliacaoDancando.Services;

public interface ISupabaseStorageService
{
    Task<string> UploadAsync(
        ConvertedAudioFile audioFile,
        string objectPath,
        CancellationToken cancellationToken = default);
}
