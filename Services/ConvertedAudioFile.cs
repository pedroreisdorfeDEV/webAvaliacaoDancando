namespace WebAvaliacaoDancando.Services;

public sealed class ConvertedAudioFile(string filePath, string fileName, string contentType) : IAsyncDisposable
{
    public string FilePath { get; } = filePath;
    public string FileName { get; } = fileName;
    public string ContentType { get; } = contentType;

    public Stream OpenReadStream()
    {
        return File.OpenRead(FilePath);
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }

        return ValueTask.CompletedTask;
    }
}
