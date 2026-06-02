using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WebAvaliacaoDancando.Configuration;

namespace WebAvaliacaoDancando.Services;

public sealed class FfmpegAudioConversionService(IOptions<AudioProcessingOptions> options) : IAudioConversionService
{
    private readonly AudioProcessingOptions audioProcessingOptions = options.Value;

    public async Task<ConvertedAudioFile> ConvertToMp3Async(
        IFormFile audioFile,
        CancellationToken cancellationToken = default)
    {
        if (audioFile.Length <= 0)
        {
            throw new InvalidOperationException("O arquivo de audio enviado esta vazio.");
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "WebAvaliacaoDancando", "audio");
        Directory.CreateDirectory(tempDirectory);

        var temporaryId = Guid.NewGuid().ToString("N");
        var inputExtension = Path.GetExtension(audioFile.FileName);
        if (string.IsNullOrWhiteSpace(inputExtension))
        {
            inputExtension = ".webm";
        }

        var inputPath = Path.Combine(tempDirectory, $"{temporaryId}{inputExtension}");
        var outputPath = Path.Combine(tempDirectory, $"{temporaryId}.mp3");

        await using (var sourceStream = File.Create(inputPath))
        {
            await audioFile.CopyToAsync(sourceStream, cancellationToken);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = audioProcessingOptions.FfmpegPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("-vn");
        startInfo.ArgumentList.Add("-acodec");
        startInfo.ArgumentList.Add("libmp3lame");
        startInfo.ArgumentList.Add("-b:a");
        startInfo.ArgumentList.Add("128k");
        startInfo.ArgumentList.Add(outputPath);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            SafeDelete(inputPath);
            throw new InvalidOperationException(
                "Nao foi possivel iniciar o ffmpeg. Confirme se ele esta instalado e disponivel no PATH.",
                ex);
        }

        _ = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        SafeDelete(inputPath);

        if (process.ExitCode != 0 || !File.Exists(outputPath))
        {
            SafeDelete(outputPath);
            var error = await errorTask;
            throw new InvalidOperationException($"Nao foi possivel converter o audio para mp3. {error}".Trim());
        }

        var fileName = $"{Path.GetFileNameWithoutExtension(audioFile.FileName)}.mp3";
        return new ConvertedAudioFile(outputPath, fileName, "audio/mpeg");
    }

    private static void SafeDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
