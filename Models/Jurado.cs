namespace WebAvaliacaoDancando.Models;

public sealed class Jurado
{
    public long Id { get; set; }
    public short Numero { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Login { get; set; }
    public string? SenhaHash { get; set; }
}
