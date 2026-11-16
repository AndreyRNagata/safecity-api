using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SafeCity.Application.Abstracoes;
using SafeCity.Domain.Comum;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Repositorios;
using SafeCity.Domain.Zonas;
using SafeCity.Infrastructure.Dados.Conversores;

namespace SafeCity.Infrastructure.Dados;

/// <summary>
/// Contexto do Entity Framework Core. Também é a Unidade de Trabalho:
/// os repositórios só preparam as mudanças, e <see cref="SalvarAlteracoesAsync"/> grava tudo de uma vez.
/// </summary>
public sealed class SafeCityDbContext : DbContext, IUnidadeDeTrabalho
{
    private readonly IRelogio _relogio;

    public SafeCityDbContext(DbContextOptions<SafeCityDbContext> options, IRelogio relogio)
        : base(options)
    {
        _relogio = relogio;
    }

    public DbSet<ZonaRisco> Zonas => Set<ZonaRisco>();

    public DbSet<Ocorrencia> Ocorrencias => Set<Ocorrencia>();

    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(cancellationToken);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PreencherAuditoria();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PreencherAuditoria();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SafeCityDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O SQLite não sabe comparar nem ordenar DateTimeOffset. Guardamos como número
        // (milissegundos desde 1970, em UTC), o que permite filtros e ORDER BY no banco.
        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetParaUnixMsConverter>();
    }

    /// <summary>Preenche CriadoEm e AtualizadoEm automaticamente em toda entidade salva.</summary>
    private void PreencherAuditoria()
    {
        var agora = _relogio.Agora;
        foreach (var entrada in ChangeTracker.Entries<Entidade>())
        {
            if (entrada.State == EntityState.Added)
            {
                entrada.Entity.MarcarCriacao(agora);
            }
            else if (entrada.State == EntityState.Modified || TemObjetoDeValorAlterado(entrada))
            {
                entrada.Entity.MarcarAtualizacao(agora);
            }
        }
    }

    /// <summary>Detecta troca de objetos de valor mapeados como owned (ex.: a coordenada da zona).</summary>
    private static bool TemObjetoDeValorAlterado(EntityEntry entrada) =>
        entrada.References.Any(r =>
            r.TargetEntry is { } alvo
            && alvo.Metadata.IsOwned()
            && alvo.State is EntityState.Added or EntityState.Modified);
}
