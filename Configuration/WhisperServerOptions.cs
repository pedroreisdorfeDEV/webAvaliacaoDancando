namespace WebAvaliacaoDancando.Configuration;

public sealed class WhisperServerOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:9000/";
    public string Model { get; set; } = "whisper-1";
    public string Language { get; set; } = "pt";
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 600;
}
