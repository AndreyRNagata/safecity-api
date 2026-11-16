using SafeCity.Domain.Ocorrencias;

namespace SafeCity.Application.Dtos;

public sealed record TotalPorTipoResponse(TipoOcorrencia Tipo, int Total, double Percentual);

public sealed record TotalPorZonaResponse(string? ZonaId, string ZonaNome, int Total);

public sealed record TotalPorFaixaHorariaResponse(string Faixa, int Total);

/// <summary>Painel de indicadores dos relatos (apoio a gestores públicos e à própria comunidade).</summary>
/// <param name="PeriodoDias">Quantos dias foram considerados.</param>
/// <param name="Desde">Início do período (UTC).</param>
/// <param name="TotalOcorrencias">Total de relatos no período.</param>
/// <param name="PorTipo">Totais por tipo de ocorrência.</param>
/// <param name="PorZona">Totais por zona de risco (relatos fora das zonas aparecem como "Fora das zonas mapeadas").</param>
/// <param name="PorFaixaHoraria">Totais por período do dia, no horário de São Paulo.</param>
/// <param name="ZonasMaisCriticasAgora">As 3 zonas com maior risco previsto para a hora atual.</param>
public sealed record EstatisticasResponse(
    int PeriodoDias,
    DateTimeOffset Desde,
    int TotalOcorrencias,
    IReadOnlyList<TotalPorTipoResponse> PorTipo,
    IReadOnlyList<TotalPorZonaResponse> PorZona,
    IReadOnlyList<TotalPorFaixaHorariaResponse> PorFaixaHoraria,
    IReadOnlyList<RankingZonaResponse> ZonasMaisCriticasAgora);
