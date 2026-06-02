namespace WebAvaliacaoDancando.Configuration;

public sealed class SupabaseStorageOptions
{
    public string ProjectUrl { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "avaliacao-audio";
}
