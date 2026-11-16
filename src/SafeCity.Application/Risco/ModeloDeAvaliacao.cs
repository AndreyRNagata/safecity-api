using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Risco;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Risco;

/// <summary>Tudo o que os fatores de risco precisam para avaliar um ponto em uma hora.</summary>
public sealed record ContextoAvaliacao(
    Coordenada Posicao,
    int Hora,
    DateTimeOffset Agora,
    IReadOnlyCollection<ZonaRisco> Zonas,
    IReadOnlyCollection<Ocorrencia> Ocorrencias);

/// <summary>Quanto um fator contribuiu para o risco e por quê (explicabilidade para o cidadão).</summary>
public sealed record ParcelaRisco(string Fator, double Valor, string Explicacao);

/// <summary>Resultado completo de uma avaliação de risco.</summary>
public sealed record ResultadoAvaliacao(
    double Score,
    NivelRisco Nivel,
    int Hora,
    ZonaRisco? ZonaDeReferencia,
    double? DistanciaMetros,
    int RelatosProximos,
    bool DentroDaArea,
    IReadOnlyList<ParcelaRisco> Parcelas);
