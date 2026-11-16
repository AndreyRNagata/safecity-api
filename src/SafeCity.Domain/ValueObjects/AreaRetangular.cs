namespace SafeCity.Domain.ValueObjects;

/// <summary>
/// Retângulo de latitude/longitude. Usado como pré-filtro rápido no banco
/// antes do cálculo exato da distância.
/// </summary>
public sealed record AreaRetangular(
    double LatitudeMinima,
    double LatitudeMaxima,
    double LongitudeMinima,
    double LongitudeMaxima);
