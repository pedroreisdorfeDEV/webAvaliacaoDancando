using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WebAvaliacaoDancando.Models;

namespace WebAvaliacaoDancando.Data;

public sealed class FestivalDbContext(DbContextOptions<FestivalDbContext> options) : DbContext(options)
{
    public DbSet<Jurado> Jurados => Set<Jurado>();
    public DbSet<Coreografia> Coreografias => Set<Coreografia>();
    public DbSet<Apresentacao> Apresentacoes => Set<Apresentacao>();
    public DbSet<DataTurnoPreferencialDisponivel> DatasTurnosPreferenciaisDisponiveis => Set<DataTurnoPreferencialDisponivel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Jurado>(entity =>
        {
            entity.ToTable("jurados");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Numero).HasColumnName("numero");
            entity.Property(item => item.Nome).HasColumnName("nome").HasMaxLength(150);
            entity.Property(item => item.Login).HasColumnName("login").HasMaxLength(150);
            entity.Property(item => item.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255);
        });

        modelBuilder.Entity<Coreografia>(entity =>
        {
            entity.ToTable("coreografias");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.InscricaoId).HasColumnName("inscricao_id");
            entity.Property(item => item.Nome).HasColumnName("nome").HasMaxLength(150);
            entity.Property(item => item.NomeCoreografo).HasColumnName("nomecoreografo").HasMaxLength(150);
            entity.Property(item => item.TipoMostra).HasColumnName("tipomostra").HasMaxLength(50);
            entity.Property(item => item.DataPreferencial).HasColumnName("datapreferencial").HasColumnType("date");
            entity.Property(item => item.TurnoPreferencial).HasColumnName("turno_preferencial").HasMaxLength(20);
            entity.Property(item => item.ModalidadeId).HasColumnName("modalidade_id");
            entity.Property(item => item.CategoriaId).HasColumnName("categoria_id");
            entity.Property(item => item.FormacaoId).HasColumnName("formacao_id");
            entity.Property(item => item.Musica).HasColumnName("musica").HasMaxLength(150);
            entity.Property(item => item.AutorCompositor).HasColumnName("autorcompositor").HasMaxLength(150);
            entity.Property(item => item.Duracao).HasColumnName("duracao").HasColumnType("time");
            entity.Property(item => item.TipoDireitoAutoral).HasColumnName("tipodireitoautoral").HasMaxLength(50);
            entity.Property(item => item.ValorEcad).HasColumnName("valorecad").HasColumnType("numeric");
            entity.Property(item => item.PossuiElementosCenicos).HasColumnName("possuielementoscenicos");
            entity.Property(item => item.DescricaoElementosCenicos).HasColumnName("descricaoelementoscenicos").HasMaxLength(500);
            entity.Property(item => item.DataCriacao).HasColumnName("datacriacao");
        });

        modelBuilder.Entity<Apresentacao>(entity =>
        {
            entity.ToTable("apresentacoes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();
            entity.Property(item => item.Ordem).HasColumnName("ordem");
            entity.Property(item => item.Data)
                .HasColumnName("data")
                .HasColumnType("date");
            entity.Property(item => item.CoreografiaId).HasColumnName("id_coreografia");
            entity.Property(item => item.Nota1).HasColumnName("nota_1").HasColumnType("numeric(5,2)");
            entity.Property(item => item.Nota2).HasColumnName("nota_2").HasColumnType("numeric(5,2)");
            entity.Property(item => item.Nota3).HasColumnName("nota_3").HasColumnType("numeric(5,2)");
            entity.Property(item => item.Nota4).HasColumnName("nota_4").HasColumnType("numeric(5,2)");
            entity.Property(item => item.MediaFinal)
                .HasColumnName("media_final")
                .HasColumnType("numeric(5,2)")
                .ValueGeneratedOnAddOrUpdate()
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entity.Property(item => item.Parecer1).HasColumnName("parecer_1");
            entity.Property(item => item.Parecer2).HasColumnName("parecer_2");
            entity.Property(item => item.Parecer3).HasColumnName("parecer_3");
            entity.Property(item => item.Parecer4).HasColumnName("parecer_4");
            entity.Property(item => item.AudioParecer1Path).HasColumnName("audio_parecer_1_path");
            entity.Property(item => item.AudioParecer2Path).HasColumnName("audio_parecer_2_path");
            entity.Property(item => item.AudioParecer3Path).HasColumnName("audio_parecer_3_path");
            entity.Property(item => item.AudioParecer4Path).HasColumnName("audio_parecer_4_path");
            entity.Property(item => item.Turno).HasColumnName("turno").HasMaxLength(20);
            entity.Property(item => item.CriadoEm).HasColumnName("criado_em");

            entity.HasOne(item => item.Coreografia)
                .WithMany(item => item.Apresentacoes)
                .HasForeignKey(item => item.CoreografiaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DataTurnoPreferencialDisponivel>(entity =>
        {
            entity.ToTable("datas_turnos_preferenciais_disponiveis");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id");
            entity.Property(item => item.Data)
                .HasColumnName("data")
                .HasColumnType("date");
            entity.Property(item => item.Turno).HasColumnName("turno").HasMaxLength(20);
            entity.Property(item => item.Ativo).HasColumnName("ativo");
            entity.Property(item => item.Ordem).HasColumnName("ordem");
            entity.Property(item => item.Observacao).HasColumnName("observacao");
            entity.Property(item => item.DataCriacao).HasColumnName("data_criacao");
            entity.Property(item => item.DataAtualizacao).HasColumnName("data_atualizacao");
        });
    }
}
