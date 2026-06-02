using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WebAvaliacaoDancando.Configuration;

namespace WebAvaliacaoDancando.Services;

public sealed class SupabaseStorageService(
    HttpClient httpClient,
    IOptions<SupabaseStorageOptions> options) : ISupabaseStorageService
{
    private readonly SupabaseStorageOptions storageOptions = options.Value;

    public async Task<string> UploadAsync(
        ConvertedAudioFile audioFile,
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildObjectUri(objectPath));
        request.Headers.TryAddWithoutValidation("apikey", storageOptions.SecretKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", storageOptions.SecretKey);
        request.Headers.TryAddWithoutValidation("x-upsert", "false");

        await using var fileStream = audioFile.OpenReadStream();
        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(audioFile.ContentType);
        request.Content = content;

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Nao foi possivel enviar o audio para o Supabase Storage. {(int)response.StatusCode} {response.ReasonPhrase}. {error}".Trim());
        }

        return objectPath;
    }

    private Uri BuildObjectUri(string objectPath)
    {
        var normalizedProjectUrl = ResolveBaseStorageUrl().TrimEnd('/');
        var encodedPath = string.Join(
            "/",
            objectPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

        return new Uri(
            $"{normalizedProjectUrl}/storage/v1/object/{Uri.EscapeDataString(storageOptions.Bucket)}/{encodedPath}");
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(ResolveBaseStorageUrl()))
        {
            throw new InvalidOperationException(
                "Configure SupabaseStorage:ProjectUrl, SupabaseStorage:Url ou SupabaseStorage:Endpoint antes de enviar audios.");
        }

        if (string.IsNullOrWhiteSpace(storageOptions.SecretKey))
        {
            throw new InvalidOperationException("Configure SupabaseStorage:SecretKey antes de enviar audios.");
        }

        if (string.IsNullOrWhiteSpace(storageOptions.Bucket))
        {
            throw new InvalidOperationException("Configure SupabaseStorage:Bucket antes de enviar audios.");
        }
    }

    private string ResolveBaseStorageUrl()
    {
        var configuredUrl = FirstNonEmpty(
            storageOptions.ProjectUrl,
            storageOptions.Url,
            storageOptions.Endpoint);

        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            return string.Empty;
        }

        configuredUrl = configuredUrl.Trim();

        if (configuredUrl.Contains("/storage/v1/s3", StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeS3Endpoint(configuredUrl);
        }

        return RemoveStorageSuffix(configuredUrl);
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string NormalizeS3Endpoint(string configuredUrl)
    {
        var uri = new Uri(configuredUrl, UriKind.Absolute);
        var match = Regex.Match(
            uri.Host,
            @"^(?<project>[^.]+)\.storage\.supabase\.co$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return RemoveStorageSuffix($"{uri.Scheme}://{uri.Host}");
        }

        return $"{uri.Scheme}://{match.Groups["project"].Value}.supabase.co";
    }

    private static string RemoveStorageSuffix(string configuredUrl)
    {
        var uri = new Uri(configuredUrl, UriKind.Absolute);
        var normalized = $"{uri.Scheme}://{uri.Host}";

        if (uri.AbsolutePath.Contains("/storage/v1/object", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        if (uri.AbsolutePath.Contains("/storage/v1", StringComparison.OrdinalIgnoreCase))
        {
            return normalized;
        }

        return normalized;
    }
}
