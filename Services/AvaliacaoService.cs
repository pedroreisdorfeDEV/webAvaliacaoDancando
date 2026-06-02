using WebAvaliacaoDancando.Models;
using WebAvaliacaoDancando.Repositories;
using WebAvaliacaoDancando.ViewModels;

namespace WebAvaliacaoDancando.Services;

public sealed class AvaliacaoService(
    IApresentacaoRepository apresentacaoRepository,
    IFestivalSessionService festivalSessionService,
    IAudioTranscriptionService audioTranscriptionService,
    IAudioConversionService audioConversionService,
    ISupabaseStorageService supabaseStorageService) : IAvaliacaoService
{
    public async Task<AvaliacaoViewModel> GetViewModelAsync(
        string? sessaoKey,
        short juradoNumero,
        string juradoNome,
        CancellationToken cancellationToken = default)
    {
        ValidateJuradoNumero(juradoNumero);

        var sessaoAtual = festivalSessionService.GetByKeyOrDefault(sessaoKey);
        var apresentacoes = await apresentacaoRepository.GetBySessaoAsync(sessaoAtual, juradoNumero, cancellationToken);

        return new AvaliacaoViewModel
        {
            JuradoNome = juradoNome,
            JuradoNumero = juradoNumero,
            Sessoes = festivalSessionService.GetAll(),
            SessaoAtual = sessaoAtual,
            Apresentacoes = apresentacoes
        };
    }

    public async Task SaveAsync(
        SalvarAvaliacaoViewModel model,
        short juradoNumero,
        CancellationToken cancellationToken = default)
    {
        ValidateJuradoNumero(juradoNumero);

        string? parecer = null;
        string? audioPath = null;

        if (model.AudioArquivo is { Length: > 0 })
        {
            var apresentacaoInfo = await apresentacaoRepository.GetAvaliacaoInfoAsync(
                model.ApresentacaoId,
                cancellationToken);

            if (apresentacaoInfo is null)
            {
                throw new InvalidOperationException("Apresentacao nao encontrada.");
            }

            await using var convertedAudio = await audioConversionService.ConvertToMp3Async(
                model.AudioArquivo,
                cancellationToken);

            //parecer = await audioTranscriptionService.TranscribeAsync(convertedAudio, cancellationToken);
            audioPath = await supabaseStorageService.UploadAsync(
                convertedAudio,
                BuildStoragePath(apresentacaoInfo, juradoNumero),
                cancellationToken);
        }

        try
        {
            await apresentacaoRepository.SaveAvaliacaoAsync(
                model.ApresentacaoId,
                juradoNumero,
                model.Nota,
                parecer,
                audioPath,
                cancellationToken);
        }
        catch (Exception e)
        {

            throw;
        }
    }

    public Task<bool> ApresentacaoJaTemParecerAsync(
        long apresentacaoId,
        short juradoNumero,
        CancellationToken cancellationToken = default)
    {
        ValidateJuradoNumero(juradoNumero);
        return apresentacaoRepository.TemParecerAsync(apresentacaoId, juradoNumero, cancellationToken);
    }

    private static void ValidateJuradoNumero(short juradoNumero)
    {
        if (juradoNumero is < 1 or > 3)
        {
            throw new InvalidOperationException("Nao foi possivel identificar o numero do jurado logado.");
        }
    }

    private static string BuildStoragePath(ApresentacaoAvaliacaoInfo apresentacaoInfo, short juradoNumero)
    {
        var turno = NormalizePathSegment(apresentacaoInfo.Turno);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        return string.Join(
            "/",
            apresentacaoInfo.Data.ToString("yyyy"),
            apresentacaoInfo.Data.ToString("MM"),
            apresentacaoInfo.Data.ToString("dd"),
            turno,
            $"apresentacao-{apresentacaoInfo.Id}",
            $"jurado-{juradoNumero}-{timestamp}.mp3");
    }

    private static string NormalizePathSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "noite";
        }

        var normalized = value.Trim().ToLowerInvariant();
        return string.Concat(normalized.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')).Trim('-');
    }
}
