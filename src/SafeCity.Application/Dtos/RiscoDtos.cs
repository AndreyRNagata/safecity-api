using SafeCity.Domain.Risco;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Dtos;

/// <summary>Resumo da zona usada como referência na avaliação de risco.</summary>
public sealed record ZonaResumoResponse(string Id, string Nome, string Bairro, TipoLocal Tipo);

/// <summary>Contribuição de um fator para o risco, com a explicação em texto.</summary>
public sealed record ParcelaRiscoResponse(string Fator, double Valor, string Explicacao);

/// <summary>"Qual o risco aqui, agora?" — resposta para um ponto e uma hora.</summary>
/// <param name="Score">Índice de 0 a 1.</param>
/// <param name="Indice">O mesmo índice, de 0 a 100 (como o app mostra).</param>
/// <param name="Nivel">baixo, moderado, alto ou muito_alto.</param>
/// <param name="Hora">Hora avaliada (horário de São Paulo).</param>
/// <param name="Zona">Zona que mais contribui para o risco (ou a mais próxima).</param>
/// <param name="DistanciaMetros">Distância até essa zona.</param>
/// <param name="RelatosProximos">Relatos de cidadãos por perto nos últimos dias.</param>
/// <param name="DentroDaArea">false quando o ponto está longe de todas as zonas mapeadas.</param>
/// <param name="Fatores">Como o índice foi calculado, fator a fator.</param>
/// <param name="Dicas">Dicas de prevenção para o local.</param>
/// <param name="ProximasHoras">Previsão do modelo para as próximas horas no mesmo ponto.</param>
public sealed record AvaliacaoRiscoResponse(
    double Score,
    int Indice,
    NivelRisco Nivel,
    int Hora,
    ZonaResumoResponse? Zona,
    int? DistanciaMetros,
    int RelatosProximos,
    bool DentroDaArea,
    IReadOnlyList<ParcelaRiscoResponse> Fatores,
    IReadOnlyList<string> Dicas,
    IReadOnlyList<RiscoHoraResponse> ProximasHoras);
