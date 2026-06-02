namespace WebAvaliacaoDancando.Models;

public sealed class Coreografia
{
    public long Id { get; set; }
    public long InscricaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string NomeCoreografo { get; set; } = string.Empty;
    public string TipoMostra { get; set; } = string.Empty;
    public DateTime DataPreferencial { get; set; }
    public string? TurnoPreferencial { get; set; }
    public long ModalidadeId { get; set; }
    public long CategoriaId { get; set; }
    public long FormacaoId { get; set; }
    public string Musica { get; set; } = string.Empty;
    public string AutorCompositor { get; set; } = string.Empty;
    public TimeSpan Duracao { get; set; }
    public string TipoDireitoAutoral { get; set; } = string.Empty;
    public decimal ValorEcad { get; set; }
    public bool PossuiElementosCenicos { get; set; }
    public string? DescricaoElementosCenicos { get; set; }
    public DateTime DataCriacao { get; set; }

    public ICollection<Apresentacao> Apresentacoes { get; set; } = [];
}
