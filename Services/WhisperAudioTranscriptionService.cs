using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WebAvaliacaoDancando.Configuration;

namespace WebAvaliacaoDancando.Services;

public sealed class WhisperAudioTranscriptionService(
    HttpClient httpClient,
    IOptions<WhisperServerOptions> options) : IAudioTranscriptionService
{
    private readonly WhisperServerOptions whisperOptions = options.Value;

    public async Task<string> TranscribeAsync(
        ConvertedAudioFile audioFile,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(whisperOptions.BaseUrl))
        {
            throw new InvalidOperationException("Configure Whisper:BaseUrl antes de transcrever audios.");
        }

        using var formData = new MultipartFormDataContent();
        await using var fileStream = audioFile.OpenReadStream();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(audioFile.ContentType);

        formData.Add(fileContent, "file", audioFile.FileName);
        formData.Add(new StringContent(whisperOptions.Model), "model");

        if (!string.IsNullOrWhiteSpace(whisperOptions.Language))
        {
            formData.Add(new StringContent(whisperOptions.Language), "language");
        }

        formData.Add(new StringContent("json"), "response_format");

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/audio/transcriptions")
        {
            Content = formData
        };

        if (!string.IsNullOrWhiteSpace(whisperOptions.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", whisperOptions.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Nao foi possivel transcrever o audio no servidor Whisper. {(int)response.StatusCode} {response.ReasonPhrase}. {error}".Trim());
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        var text = document.RootElement.TryGetProperty("text", out var textElement)
            ? textElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("A transcricao retornou vazia. Revise o audio gravado e tente novamente.");
        }

        return text.Trim();
    }
}
