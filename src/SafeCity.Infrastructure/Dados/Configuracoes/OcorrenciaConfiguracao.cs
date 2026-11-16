using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeCity.Domain.Ocorrencias;

namespace SafeCity.Infrastructure.Dados.Configuracoes;

/// <summary>
/// Mapeamento da hierarquia de <see cref="Ocorrencia"/> com TPH (Table-per-Hierarchy):
/// Furto, Roubo, Tentativa e AtitudeSuspeita ficam na mesma tabela, e a coluna "Tipo"
/// (discriminador) diz qual classe o EF Core deve criar ao ler cada linha.
/// </summary>
public sealed class OcorrenciaConfiguracao : IEntityTypeConfiguration<Ocorrencia>
{
    public void Configure(EntityTypeBuilder<Ocorrencia> builder)
    {
        builder.ToTable("Ocorrencias");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        builder.Property(o => o.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.HasDiscriminator(o => o.Tipo)
            .HasValue<Furto>(TipoOcorrencia.Furto)
            .HasValue<Roubo>(TipoOcorrencia.Roubo)
            .HasValue<Tentativa>(TipoOcorrencia.Tentativa)
            .HasValue<AtitudeSuspeita>(TipoOcorrencia.Suspeita);

        builder.OwnsOne(o => o.Local, local =>
        {
            local.Property(c => c.Latitude).HasColumnName("Latitude");
            local.Property(c => c.Longitude).HasColumnName("Longitude");
            local.HasIndex(c => new { c.Latitude, c.Longitude });
        });
        builder.Navigation(o => o.Local).IsRequired();

        builder.Property(o => o.ZonaId).HasMaxLength(60);
        builder.HasIndex(o => o.OcorridoEm);
    }
}
