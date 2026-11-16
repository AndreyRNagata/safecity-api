using System.Globalization;
using SafeCity.Domain.Comum;

namespace SafeCity.Domain.ValueObjects;

/// <summary>
/// Objeto de valor que representa um ponto no mapa (graus decimais, WGS 84).
/// É imutável e comparado pelo valor: duas coordenadas iguais são o mesmo ponto.
/// </summary>
public sealed record Coordenada
{
    private const double RaioDaTerraEmMetros = 6_371_000;
    private const double MetrosPorGrauDeLatitude = 111_320;

    public Coordenada(double latitude, double longitude)
    {
        Guarda.Contra(!double.IsFinite(latitude) || latitude is < -90 or > 90, "A latitude deve estar entre -90 e 90.");
        Guarda.Contra(!double.IsFinite(longitude) || longitude is < -180 or > 180, "A longitude deve estar entre -180 e 180.");
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    /// <summary>Distância em linha reta até outro ponto, pela fórmula de Haversine.</summary>
    public double DistanciaEmMetrosAte(Coordenada outra)
    {
        ArgumentNullException.ThrowIfNull(outra);

        static double ParaRadianos(double graus) => graus * Math.PI / 180;

        var deltaLatitude = ParaRadianos(outra.Latitude - Latitude);
        var deltaLongitude = ParaRadianos(outra.Longitude - Longitude);
        var h = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
              + Math.Cos(ParaRadianos(Latitude)) * Math.Cos(ParaRadianos(outra.Latitude))
              * Math.Pow(Math.Sin(deltaLongitude / 2), 2);

        return 2 * RaioDaTerraEmMetros * Math.Asin(Math.Sqrt(h));
    }

    /// <summary>
    /// Arredonda a coordenada. Com 3 casas decimais a precisão fica em torno de 100 m:
    /// suficiente para o mapa de risco, sem guardar o endereço exato de quem relatou.
    /// </summary>
    public Coordenada Aproximada(int casasDecimais = 3) => new(
        Math.Round(Latitude, casasDecimais, MidpointRounding.AwayFromZero),
        Math.Round(Longitude, casasDecimais, MidpointRounding.AwayFromZero));

    /// <summary>Retângulo que contém o círculo de raio <paramref name="raioMetros"/> ao redor do ponto.</summary>
    public AreaRetangular AreaAoRedor(double raioMetros)
    {
        Guarda.Contra(raioMetros <= 0, "O raio deve ser maior que zero.");
        var deltaLatitude = raioMetros / MetrosPorGrauDeLatitude;
        var deltaLongitude = raioMetros / (MetrosPorGrauDeLatitude * Math.Cos(Latitude * Math.PI / 180));
        return new AreaRetangular(
            Latitude - deltaLatitude,
            Latitude + deltaLatitude,
            Longitude - deltaLongitude,
            Longitude + deltaLongitude);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"({Latitude:0.#####}, {Longitude:0.#####})");
}
