using Microsoft.EntityFrameworkCore;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Repositorios;
using SafeCity.Infrastructure.Dados;

namespace SafeCity.Infrastructure.Repositorios;

public sealed class OcorrenciaRepositorio : RepositorioBase<Ocorrencia, long>, IOcorrenciaRepositorio
{
    public OcorrenciaRepositorio(SafeCityDbContext contexto) : base(contexto)
    {
    }

    public async Task<IReadOnlyList<Ocorrencia>> BuscarAsync(
        FiltroOcorrencias filtro,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Ocorrencia> consulta = Conjunto.AsNoTracking();

        if (filtro.Tipo is { } tipo)
        {
            consulta = consulta.Where(o => o.Tipo == tipo);
        }

        if (!string.IsNullOrWhiteSpace(filtro.ZonaId))
        {
            consulta = consulta.Where(o => o.ZonaId == filtro.ZonaId);
        }

        if (filtro.Desde is { } desde)
        {
            consulta = consulta.Where(o => o.OcorridoEm >= desde);
        }

        if (filtro.Ate is { } ate)
        {
            consulta = consulta.Where(o => o.OcorridoEm <= ate);
        }

        if (filtro.Area is { } area)
        {
            consulta = consulta.Where(o =>
                o.Local.Latitude >= area.LatitudeMinima && o.Local.Latitude <= area.LatitudeMaxima &&
                o.Local.Longitude >= area.LongitudeMinima && o.Local.Longitude <= area.LongitudeMaxima);
        }

        return await consulta
            .OrderByDescending(o => o.OcorridoEm)
            .ThenByDescending(o => o.Id)
            .Take(filtro.Limite)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Ocorrencia>> ListarDesdeAsync(
        DateTimeOffset desde,
        CancellationToken cancellationToken = default) =>
        await Conjunto
            .AsNoTracking()
            .Where(o => o.OcorridoEm >= desde)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ContagemPorTipo>> ContarPorTipoAsync(
        DateTimeOffset desde,
        CancellationToken cancellationToken = default)
    {
        // SELECT Tipo, COUNT(*) FROM Ocorrencias WHERE OcorridoEm >= @desde GROUP BY Tipo
        var totais = await Conjunto
            .Where(o => o.OcorridoEm >= desde)
            .GroupBy(o => o.Tipo)
            .Select(grupo => new { Tipo = grupo.Key, Total = grupo.Count() })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        return totais.Select(x => new ContagemPorTipo(x.Tipo, x.Total)).ToList();
    }

    public async Task<IReadOnlyList<ContagemPorZona>> ContarPorZonaAsync(
        DateTimeOffset desde,
        CancellationToken cancellationToken = default)
    {
        // Usa a navegação Zona.Ocorrencias: o EF Core gera uma subconsulta COUNT para cada zona.
        var porZona = await Contexto.Zonas
            .Select(z => new
            {
                z.Id,
                z.Nome,
                Total = z.Ocorrencias.Count(o => o.OcorridoEm >= desde),
            })
            .Where(x => x.Total > 0)
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);

        var foraDasZonas = await Conjunto.CountAsync(o => o.ZonaId == null && o.OcorridoEm >= desde, cancellationToken);

        var resultado = porZona.Select(x => new ContagemPorZona(x.Id, x.Nome, x.Total)).ToList();
        if (foraDasZonas > 0)
        {
            resultado.Add(new ContagemPorZona(null, null, foraDasZonas));
        }

        return resultado;
    }

    public async Task<IReadOnlyList<DateTimeOffset>> ListarHorariosDesdeAsync(
        DateTimeOffset desde,
        CancellationToken cancellationToken = default) =>
        await Conjunto
            .Where(o => o.OcorridoEm >= desde)
            .Select(o => o.OcorridoEm)
            .ToListAsync(cancellationToken);
}
