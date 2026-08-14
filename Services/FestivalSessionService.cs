using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WebAvaliacaoDancando.Data;
using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Services;

public sealed class FestivalSessionService(FestivalDbContext context) : IFestivalSessionService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public async Task<IReadOnlyList<FestivalSessao>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var registros = await context.DatasTurnosPreferenciaisDisponiveis
            .AsNoTracking()
            .OrderBy(item => item.Data)
            .ThenBy(item => item.Ordem)
            .ThenBy(item => item.Turno)
            .Select(item => new
            {
                item.Data,
                item.Turno
            })
            .ToListAsync(cancellationToken);

        var sessoes = new List<FestivalSessao>();
        var chavesProcessadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var registro in registros)
        {
            var turnoNormalizado = NormalizeTurno(registro.Turno);
            var key = BuildKey(registro.Data, turnoNormalizado);

            if (!chavesProcessadas.Add(key))
            {
                continue;
            }

            sessoes.Add(new FestivalSessao
            {
                Key = key,
                Titulo = BuildTitulo(registro.Data, turnoNormalizado),
                Data = registro.Data,
                Turno = turnoNormalizado
            });
        }

        return sessoes;
    }

    public FestivalSessao GetByKeyOrDefault(IReadOnlyList<FestivalSessao> sessoes, string? key)
    {
        if (sessoes.Count == 0)
        {
            throw new InvalidOperationException("Nenhuma sessao foi encontrada na tabela datas_turnos_preferenciais_disponiveis.");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return sessoes[0];
        }

        return sessoes.FirstOrDefault(sessao => sessao.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?? sessoes[0];
    }

    private static string BuildKey(DateTime data, string turno)
    {
        return $"{data:yyyy-MM-dd}-{turno.ToLowerInvariant()}";
    }

    private static string BuildTitulo(DateTime data, string turno)
    {
        var diaSemana = Capitalize(data.ToString("dddd", PtBr));
        return $"{diaSemana} - {data:dd/MM/yyyy} - {FormatTurno(turno)}";
    }

    private static string NormalizeTurno(string? turno)
    {
        return string.IsNullOrWhiteSpace(turno)
            ? "NOITE"
            : turno.Trim().ToUpperInvariant();
    }

    private static string FormatTurno(string turno)
    {
        return turno switch
        {
            "MANHA" => "Manhã",
            "TARDE" => "Tarde",
            "NOITE" => "Noite",
            _ => Capitalize(turno.ToLower(PtBr))
        };
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return char.ToUpper(value[0], PtBr) + value[1..];
    }
}
