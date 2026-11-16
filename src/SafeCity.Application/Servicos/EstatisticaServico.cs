using SafeCity.Application.Abstracoes;
using SafeCity.Application.Dtos;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Repositorios;

namespace SafeCity.Application.Servicos;

/// <summary>Indicadores agregados dos relatos (painel para gestores e para a comunidade).</summary>
public interface IEstatisticaServico
{
    Task<EstatisticasResponse> ObterResumoAsync(int dias, CancellationToken cancellationToken = default);
}

public sealed class EstatisticaServico : IEstatisticaServico
{
    private const int ZonasNoRanking = 3;
    private const string SemZona = "Fora das zonas mapeadas";

    private static readonly FaixaDoDia[] Faixas =
    {
        new("Madrugada (0h–5h)", 0, 5),
        new("Manhã (6h–11h)", 6, 11),
        new("Tarde (12h–17h)", 12, 17),
        new("Noite (18h–23h)", 18, 23),
    };

    private readonly IOcorrenciaRepositorio _ocorrencias;
    private readonly IZonaRepositorio _zonas;
    private readonly IRelogio _relogio;

    public EstatisticaServico(IOcorrenciaRepositorio ocorrencias, IZonaRepositorio zonas, IRelogio relogio)
    {
        _ocorrencias = ocorrencias;
        _zonas = zonas;
        _relogio = relogio;
    }

    public async Task<EstatisticasResponse> ObterResumoAsync(int dias, CancellationToken cancellationToken = default)
    {
        var desde = _relogio.Agora.AddDays(-dias);

        // Agrupamentos feitos no banco (GROUP BY gerado pelo LINQ do EF Core).
        var porTipo = await _ocorrencias.ContarPorTipoAsync(desde, cancellationToken);
        var porZona = await _ocorrencias.ContarPorZonaAsync(desde, cancellationToken);

        // A hora local depende do fuso de São Paulo, então esse agrupamento é feito em memória.
        var horarios = await _ocorrencias.ListarHorariosDesdeAsync(desde, cancellationToken);
        var horasLocais = horarios.Select(h => _relogio.ParaHorarioLocal(h).Hour).ToList();

        var total = porTipo.Sum(c => c.Total);
        var zonas = await _zonas.ListarAsync(cancellationToken);

        return new EstatisticasResponse(
            dias,
            desde,
            total,
            TotaisPorTipo(porTipo, total),
            porZona.Select(c => new TotalPorZonaResponse(c.ZonaId, c.ZonaNome ?? SemZona, c.Total)).ToList(),
            Faixas.Select(f => new TotalPorFaixaHorariaResponse(f.Nome, horasLocais.Count(f.Contem))).ToList(),
            RankingDeZonas.Montar(zonas, _relogio.HoraLocalAtual, ZonasNoRanking));
    }

    /// <summary>Inclui os tipos sem nenhum relato (total zero) para o painel ficar completo.</summary>
    private static IReadOnlyList<TotalPorTipoResponse> TotaisPorTipo(IReadOnlyList<ContagemPorTipo> contagens, int total) =>
        Enum.GetValues<TipoOcorrencia>()
            .GroupJoin(
                contagens,
                tipo => tipo,
                contagem => contagem.Tipo,
                (tipo, encontradas) => new { Tipo = tipo, Total = encontradas.Sum(c => c.Total) })
            .Select(x => new TotalPorTipoResponse(
                x.Tipo,
                x.Total,
                total == 0 ? 0 : Math.Round(100.0 * x.Total / total, 1)))
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Tipo)
            .ToList();

    private sealed record FaixaDoDia(string Nome, int HoraInicial, int HoraFinal)
    {
        public bool Contem(int hora) => hora >= HoraInicial && hora <= HoraFinal;
    }
}
