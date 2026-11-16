using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.ValueObjects;

namespace SafeCity.Domain.Repositorios;

/// <summary>Critérios de busca de ocorrências. Todos são opcionais.</summary>
public sealed record FiltroOcorrencias
{
    public const int LimitePadrao = 500;

    public TipoOcorrencia? Tipo { get; init; }

    public string? ZonaId { get; init; }

    public DateTimeOffset? Desde { get; init; }

    public DateTimeOffset? Ate { get; init; }

    /// <summary>Retângulo usado como pré-filtro geográfico.</summary>
    public AreaRetangular? Area { get; init; }

    public int Limite { get; init; } = LimitePadrao;
}

public sealed record ContagemPorTipo(TipoOcorrencia Tipo, int Total);

public sealed record ContagemPorZona(string? ZonaId, string? ZonaNome, int Total);

public interface IOcorrenciaRepositorio : IRepositorio<Ocorrencia, long>
{
    /// <summary>Busca com filtros, da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<Ocorrencia>> BuscarAsync(FiltroOcorrencias filtro, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ocorrencia>> ListarDesdeAsync(DateTimeOffset desde, CancellationToken cancellationToken = default);

    /// <summary>Total de ocorrências por tipo desde a data informada (agrupado no banco).</summary>
    Task<IReadOnlyList<ContagemPorTipo>> ContarPorTipoAsync(DateTimeOffset desde, CancellationToken cancellationToken = default);

    /// <summary>Total de ocorrências por zona desde a data informada. Relatos fora das zonas vêm com ZonaId nulo.</summary>
    Task<IReadOnlyList<ContagemPorZona>> ContarPorZonaAsync(DateTimeOffset desde, CancellationToken cancellationToken = default);

    /// <summary>Só os horários das ocorrências (projeção leve, sem carregar as entidades).</summary>
    Task<IReadOnlyList<DateTimeOffset>> ListarHorariosDesdeAsync(DateTimeOffset desde, CancellationToken cancellationToken = default);
}
