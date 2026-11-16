using SafeCity.Domain.Comum;

namespace SafeCity.Domain.Repositorios;

/// <summary>
/// Operações básicas de persistência, comuns a todas as entidades.
/// O domínio só conhece esta interface; quem implementa é a camada de infraestrutura (EF Core).
/// </summary>
public interface IRepositorio<TEntidade, TId>
    where TEntidade : Entidade<TId>
    where TId : notnull
{
    /// <summary>Busca a entidade para leitura ou alteração (fica rastreada pelo EF Core).</summary>
    Task<TEntidade?> ObterPorIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>Lista todas as entidades, só para leitura.</summary>
    Task<IReadOnlyList<TEntidade>> ListarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(TEntidade entidade, CancellationToken cancellationToken = default);

    void Remover(TEntidade entidade);
}

/// <summary>Confirma no banco, de uma vez, todas as alterações feitas pelos repositórios.</summary>
public interface IUnidadeDeTrabalho
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
