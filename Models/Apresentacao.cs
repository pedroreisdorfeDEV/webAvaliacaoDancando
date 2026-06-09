namespace WebAvaliacaoDancando.Models;

public sealed class Apresentacao
{
    public long Id { get; set; }
    public DateTime Data { get; set; }
    public long CoreografiaId { get; set; }
    public decimal? Nota1 { get; set; }
    public decimal? Nota2 { get; set; }
    public decimal? Nota3 { get; set; }
    public decimal? Nota4 { get; set; }
    public decimal? MediaFinal { get; set; }
    public string? Parecer1 { get; set; }
    public string? Parecer2 { get; set; }
    public string? Parecer3 { get; set; }
    public string? Parecer4 { get; set; }
    public string? AudioParecer1Path { get; set; }
    public string? AudioParecer2Path { get; set; }
    public string? AudioParecer3Path { get; set; }
    public string? AudioParecer4Path { get; set; }
    public string Turno { get; set; } = "NOITE";
    public DateTimeOffset? CriadoEm { get; set; }

    public Coreografia? Coreografia { get; set; }
}
