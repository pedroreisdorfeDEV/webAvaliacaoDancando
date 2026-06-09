using Microsoft.EntityFrameworkCore;
using WebAvaliacaoDancando.Data;
using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Repositories;

public sealed class ApresentacaoRepository(FestivalDbContext context) : IApresentacaoRepository
{
    public async Task<IReadOnlyList<ApresentacaoCardViewModel>> GetBySessaoAsync(
        FestivalSessao sessao,
        short juradoNumero,
        CancellationToken cancellationToken = default)
    {
        var turnoNormalizado = sessao.TurnoNormalizado;

        return await context.Apresentacoes
            .AsNoTracking()
            .Include(item => item.Coreografia)
            .Where(item =>
                item.Data == sessao.Data &&
                (item.Turno ?? "NOITE").ToUpper() == turnoNormalizado)
            .OrderBy(item => item.Id)
            .Select(item => new ApresentacaoCardViewModel
            {
                ApresentacaoId = item.Id,
                CoreografiaId = item.CoreografiaId,
                Data = item.Data,
                Turno = (item.Turno ?? "NOITE").ToUpper(),
                CoreografiaNome = item.Coreografia != null ? item.Coreografia.Nome : string.Empty,
                CoreografoNome = item.Coreografia != null && item.Coreografia.NomeCoreografo != string.Empty
                    ? item.Coreografia.NomeCoreografo
                    : "Coreografo nao informado",
                TipoMostra = item.Coreografia != null && item.Coreografia.TipoMostra != string.Empty
                    ? item.Coreografia.TipoMostra
                    : "Mostra nao informada",
                NotaAtual = juradoNumero == 1
                    ? item.Nota1
                    : juradoNumero == 2
                        ? item.Nota2
                        : juradoNumero == 3
                            ? item.Nota3
                            : item.Nota4,
                ParecerAtual = juradoNumero == 1
                    ? item.Parecer1
                    : juradoNumero == 2
                        ? item.Parecer2
                        : juradoNumero == 3
                            ? item.Parecer3
                            : item.Parecer4,
                MediaFinal = item.MediaFinal
            })
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAvaliacaoAsync(
        long apresentacaoId,
        short juradoNumero,
        decimal nota,
        string? parecer,
        string? audioPath,
        CancellationToken cancellationToken = default)
    {
        var apresentacao = await context.Apresentacoes
            .FirstOrDefaultAsync(item => item.Id == apresentacaoId, cancellationToken);

        if (apresentacao is null)
        {
            throw new InvalidOperationException("Apresentacao nao encontrada.");
        }

        var notaNormalizada = Math.Round(nota, 2, MidpointRounding.AwayFromZero);
        var parecerNormalizado = string.IsNullOrWhiteSpace(parecer) ? null : parecer.Trim();
        var audioPathNormalizado = string.IsNullOrWhiteSpace(audioPath) ? null : audioPath.Trim();

        switch (juradoNumero)
        {
            case 1:
                apresentacao.Nota1 = notaNormalizada;
                if (parecerNormalizado is not null)
                {
                    apresentacao.Parecer1 = parecerNormalizado;
                }

                if (audioPathNormalizado is not null)
                {
                    apresentacao.AudioParecer1Path = audioPathNormalizado;
                }

                break;
            case 2:
                apresentacao.Nota2 = notaNormalizada;
                if (parecerNormalizado is not null)
                {
                    apresentacao.Parecer2 = parecerNormalizado;
                }

                if (audioPathNormalizado is not null)
                {
                    apresentacao.AudioParecer2Path = audioPathNormalizado;
                }

                break;
            case 3:
                apresentacao.Nota3 = notaNormalizada;
                if (parecerNormalizado is not null)
                {
                    apresentacao.Parecer3 = parecerNormalizado;
                }

                if (audioPathNormalizado is not null)
                {
                    apresentacao.AudioParecer3Path = audioPathNormalizado;
                }

                break;
            case 4:
                apresentacao.Nota4 = notaNormalizada;
                if (parecerNormalizado is not null)
                {
                    apresentacao.Parecer4 = parecerNormalizado;
                }

                if (audioPathNormalizado is not null)
                {
                    apresentacao.AudioParecer4Path = audioPathNormalizado;
                }

                break;
            default:
                throw new InvalidOperationException("O numero do jurado precisa estar entre 1 e 4.");
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TemParecerAsync(
        long apresentacaoId,
        short juradoNumero,
        CancellationToken cancellationToken = default)
    {
        var parecer = await context.Apresentacoes
            .AsNoTracking()
            .Where(item => item.Id == apresentacaoId)
            .Select(item => juradoNumero == 1
                ? item.Parecer1
                : juradoNumero == 2
                    ? item.Parecer2
                    : juradoNumero == 3
                        ? item.Parecer3
                        : item.Parecer4)
            .FirstOrDefaultAsync(cancellationToken);

        return !string.IsNullOrWhiteSpace(parecer);
    }

    public async Task<ApresentacaoAvaliacaoInfo?> GetAvaliacaoInfoAsync(
        long apresentacaoId,
        CancellationToken cancellationToken = default)
    {
        return await context.Apresentacoes
            .AsNoTracking()
            .Where(item => item.Id == apresentacaoId)
            .Select(item => new ApresentacaoAvaliacaoInfo
            {
                Id = item.Id,
                Data = item.Data,
                Turno = item.Turno
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
