using SafeCity.Domain.ValueObjects;

namespace SafeCity.Domain.Zonas;

/// <summary>Serviço de domínio que descobre a qual zona um ponto pertence.</summary>
public static class LocalizadorDeZonas
{
    /// <summary>
    /// Entre as zonas que influenciam o ponto, devolve a mais próxima em proporção ao raio
    /// (estar a 300 m de uma zona de raio 300 m conta mais que estar a 300 m de uma de raio 100 m).
    /// Devolve <c>null</c> se o ponto estiver fora do alcance de todas.
    /// </summary>
    public static ZonaRisco? ZonaQueAbrange(IEnumerable<ZonaRisco> zonas, Coordenada ponto) =>
        zonas
            .Select(zona => new { Zona = zona, Distancia = zona.DistanciaEmMetrosAte(ponto) })
            .Where(x => x.Distancia <= x.Zona.AlcanceMetros)
            .OrderBy(x => x.Distancia / x.Zona.RaioMetros)
            .Select(x => x.Zona)
            .FirstOrDefault();
}
