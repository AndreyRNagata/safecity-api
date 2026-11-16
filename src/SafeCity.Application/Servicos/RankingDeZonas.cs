using SafeCity.Application.Dtos;
using SafeCity.Domain.Risco;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Servicos;

/// <summary>Ordena as zonas pelo risco previsto em uma hora (LINQ to Objects).</summary>
internal static class RankingDeZonas
{
    public static IReadOnlyList<RankingZonaResponse> Montar(IEnumerable<ZonaRisco> zonas, int hora, int limite) =>
        zonas
            .Select(zona => new { Zona = zona, Risco = zona.RiscoNaHora(hora) })
            .OrderByDescending(x => x.Risco)
            .ThenBy(x => x.Zona.Nome)
            .Take(limite)
            .Select((x, indice) => new RankingZonaResponse(
                indice + 1,
                x.Zona.Id,
                x.Zona.Nome,
                x.Zona.Bairro,
                x.Zona.Tipo,
                x.Risco,
                ClassificadorRisco.Classificar(x.Risco)))
            .ToList();
}
