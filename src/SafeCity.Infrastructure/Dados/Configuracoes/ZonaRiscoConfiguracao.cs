using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafeCity.Domain.Zonas;
using SafeCity.Infrastructure.Dados.Conversores;

namespace SafeCity.Infrastructure.Dados.Configuracoes;

/// <summary>Mapeamento da entidade <see cref="ZonaRisco"/> para a tabela "Zonas" (Fluent API).</summary>
public sealed class ZonaRiscoConfiguracao : IEntityTypeConfiguration<ZonaRisco>
{
    public void Configure(EntityTypeBuilder<ZonaRisco> builder)
    {
        builder.ToTable("Zonas");
        builder.HasKey(z => z.Id);

        builder.Property(z => z.Id).HasMaxLength(ZonaRisco.TamanhoMaximoId);
        builder.Property(z => z.Nome).HasMaxLength(ZonaRisco.TamanhoMaximoNome).IsRequired();
        builder.Property(z => z.Bairro).HasMaxLength(ZonaRisco.TamanhoMaximoBairro).IsRequired();
        builder.Property(z => z.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(z => z.RaioMetros);
        builder.Property(z => z.OcorrenciasUltimos30Dias);

        // Objeto de valor Coordenada: vira as colunas Latitude e Longitude da própria tabela.
        builder.OwnsOne(z => z.Centro, centro =>
        {
            centro.Property(c => c.Latitude).HasColumnName("Latitude");
            centro.Property(c => c.Longitude).HasColumnName("Longitude");
        });
        builder.Navigation(z => z.Centro).IsRequired();

        builder.Property(z => z.Previsao)
            .HasColumnName("RiscoPorHora")
            .HasConversion(new PrevisaoHorariaConverter())
            .IsRequired();

        builder.Property(z => z.Dicas)
            .HasConversion(new ListaDeTextosConverter(), new ListaDeTextosComparer())
            .IsRequired();

        // 1 zona : N ocorrências. Ao apagar a zona, os relatos continuam (ficam sem zona).
        builder.HasMany(z => z.Ocorrencias)
            .WithOne(o => o.Zona)
            .HasForeignKey(o => o.ZonaId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Navigation(z => z.Ocorrencias).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(z => z.AlcanceMetros);
        builder.HasIndex(z => z.Bairro);
    }
}
