namespace WebAvaliacaoDancando.Models;

public sealed class ApresentacaoAvaliacaoInfo
{
    public long Id { get; set; }
    public DateTime Data { get; set; }
    public string Turno { get; set; } = "NOITE";
}
