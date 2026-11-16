using Microsoft.EntityFrameworkCore;
using SafeCity.Domain.Comum;
using SafeCity.Domain.Repositorios;
using SafeCity.Infrastructure.Dados;

namespace SafeCity.Infrastructure.Repositorios;

/// <summary>
/// Implementação genérica (herança + generics) das operações comuns a todos os repositórios.
/// Os repositórios concretos herdam daqui e acrescentam suas consultas específicas.
/// </summary>
public abstract class RepositorioBase<TEntidade, TId> : IRepositorio<TEntidade, TId>
    where TEntidade : Entidade<TId>
    where TId : notnull
{
    protected RepositorioBase(SafeCityDbContext contexto)
    {
        Contexto = contexto;
        Conjunto = contexto.Set<TEntidade>();
    }

    protected SafeCityDbContext Contexto { get; }

    protected DbSet<TEntidade> Conjunto { get; }

    public virtual async Task<TEntidade?> ObterPorIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await Conjunto.FindAsync(new object[] { id }, cancellationToken);

    public virtual async Task<IReadOnlyList<TEntidade>> ListarAsync(CancellationToken cancellationToken = default) =>
        await Conjunto.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AdicionarAsync(TEntidade entidade, CancellationToken cancellationToken = default) =>
        await Conjunto.AddAsync(entidade, cancellationToken);

    public virtual void Remover(TEntidade entidade) => Conjunto.Remove(entidade);
}
