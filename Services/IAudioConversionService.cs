using Microsoft.AspNetCore.Http;

namespace WebAvaliacaoDancando.Services;

public interface IAudioConversionService
{
    Task<ConvertedAudioFile> ConvertToMp3Async(IFormFile audioFile, CancellationToken cancellationToken = default);
}
