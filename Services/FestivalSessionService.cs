using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Services;

public sealed class FestivalSessionService : IFestivalSessionService
{
    private readonly IReadOnlyList<FestivalSessao> _sessoes =
    [
        new FestivalSessao
        {
            Key = "2026-08-22-tarde",
            Titulo = "Sábado à tarde - 22 de agosto de 2026",
            Data = new DateTime(2026, 8, 22),
            Turno = "TARDE"
        },
        new FestivalSessao
        {
            Key = "2026-08-22-noite",
            Titulo = "Sábado à noite - 22 de agosto de 2026",
            Data = new DateTime(2026, 8, 22),
            Turno = "NOITE"
        },
        new FestivalSessao
        {
            Key = "2026-08-23-tarde",
            Titulo = "Domingo à tarde - 23 de agosto de 2026",
            Data = new DateTime(2026, 8, 23),
            Turno = "TARDE"
        },
        new FestivalSessao
        {
            Key = "2026-08-23-noite",
            Titulo = "Domingo à noite - 23 de agosto de 2026",
            Data = new DateTime(2026, 8, 23),
            Turno = "NOITE"
        },
        new FestivalSessao
        {
            Key = "2026-08-24-noite",
            Titulo = "Segunda-feira - 24 de agosto de 2026",
            Data = new DateTime(2026, 8, 24),
            Turno = "NOITE"
        },
        new FestivalSessao
        {
            Key = "2026-08-25-noite",
            Titulo = "Terça-feira - 25 de agosto de 2026",
            Data = new DateTime(2026, 8, 25),
            Turno = "NOITE"
        },
        new FestivalSessao
        {
            Key = "2026-08-26-noite",
            Titulo = "Quarta-feira - 26 de agosto de 2026",
            Data = new DateTime(2026, 8, 26),
            Turno = "NOITE"
        },
        new FestivalSessao
        {
            Key = "2026-08-27-noite",
            Titulo = "Quinta-feira - 27 de agosto de 2026",
            Data = new DateTime(2026, 8, 27),
            Turno = "NOITE"
        },
        new FestivalSessao
        {
            Key = "2026-08-28-noite",
            Titulo = "Sexta-feira - 28 de agosto de 2026",
            Data = new DateTime(2026, 8, 28),
            Turno = "NOITE"
        }
    ];

    public IReadOnlyList<FestivalSessao> GetAll() => _sessoes;

    public FestivalSessao GetByKeyOrDefault(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return _sessoes[0];
        }

        return _sessoes.FirstOrDefault(sessao => sessao.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?? _sessoes[0];
    }
}
