using Microsoft.Extensions.Logging;
using SafeCity.Application.Abstracoes;
using SafeCity.Application.Dtos;
using SafeCity.Application.Mapeamentos;
using SafeCity.Domain.Comum;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Repositorios;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Servicos;

/// <summary>Casos de uso dos relatos de cidadãos.</summary>
public interface IOcorrenciaServico
{
    Task<IReadOnlyList<OcorrenciaResponse>> ListarAsync(FiltroOcorrenciasRequest filtro, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OcorrenciaResponse>> ListarDaZonaAsync(string zonaId, CancellationToken cancellationToken = default);

    Task<OcorrenciaResponse> ObterAsync(long id, CancellationToken cancellationToken = default);

    Task<OcorrenciaResponse> RegistrarAsync(CriarOcorrenciaRequest requisicao, CancellationToken cancellationToken = default);

    Task RemoverAsync(long id, CancellationToken cancellationToken = default);
}

public sealed class OcorrenciaServico : IOcorrenciaServico
{
    private const string Recurso = "Ocorrência";
    private const double RaioPadraoDeBuscaMetros = 1_000;

    private readonly IOcorrenciaRepositorio _ocorrencias;
    private readonly IZonaRepositorio _zonas;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly IRelogio _relogio;
    private readonly ILogger<OcorrenciaServico> _logger;

    public OcorrenciaServico(
        IOcorrenciaRepositorio ocorrencias,
        IZonaRepositorio zonas,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio,
        ILogger<OcorrenciaServico> logger)
    {
        _ocorrencias = ocorrencias;
        _zonas = zonas;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OcorrenciaResponse>> ListarAsync(
        FiltroOcorrenciasRequest filtro,
        CancellationToken cancellationToken = default)
    {
        Guarda.Contra(
            filtro.Latitude.HasValue != filtro.Longitude.HasValue,
            "Para buscar por proximidade, informe latitude e longitude juntas.");

        var centro = filtro.Latitude is { } latitude && filtro.Longitude is { } longitude
            ? new Coordenada(latitude, longitude)
            : null;
        var raio = filtro.RaioMetros ?? RaioPadraoDeBuscaMetros;

        var criterios = new FiltroOcorrencias
        {
            Tipo = filtro.Tipo,
            ZonaId = filtro.ZonaId,
            Desde = filtro.Desde,
            Limite = filtro.Limite,
            // O banco filtra por um retângulo (rápido); o círculo exato é conferido logo abaixo.
            Area = centro?.AreaAoRedor(raio),
        };

        var encontradas = await _ocorrencias.BuscarAsync(criterios, cancellationToken);

        return encontradas
            .Where(o => centro is null || o.Local.DistanciaEmMetrosAte(centro) <= raio)
            .Select(o => o.ParaResposta())
            .ToList();
    }

    public async Task<IReadOnlyList<OcorrenciaResponse>> ListarDaZonaAsync(
        string zonaId,
        CancellationToken cancellationToken = default)
    {
        if (!await _zonas.ExisteAsync(zonaId, cancellationToken))
        {
            throw new RecursoNaoEncontradoException("Zona", zonaId);
        }

        return await ListarAsync(new FiltroOcorrenciasRequest { ZonaId = zonaId }, cancellationToken);
    }

    public async Task<OcorrenciaResponse> ObterAsync(long id, CancellationToken cancellationToken = default) =>
        (await ObterEntidadeAsync(id, cancellationToken)).ParaResposta();

    public async Task<OcorrenciaResponse> RegistrarAsync(
        CriarOcorrenciaRequest requisicao,
        CancellationToken cancellationToken = default)
    {
        var agora = _relogio.Agora;

        // A fábrica escolhe a subclasse certa (Furto, Roubo...) e o domínio valida e arredonda o local.
        var ocorrencia = OcorrenciaFactory.Criar(
            requisicao.Tipo!.Value,
            new Coordenada(requisicao.Latitude!.Value, requisicao.Longitude!.Value),
            requisicao.OcorridoEm ?? agora,
            agora);

        var zonas = await _zonas.ListarAsync(cancellationToken);
        ocorrencia.VincularZona(LocalizadorDeZonas.ZonaQueAbrange(zonas, ocorrencia.Local));

        await _ocorrencias.AdicionarAsync(ocorrencia, cancellationToken);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation(
            "Ocorrência {OcorrenciaId} ({Tipo}) registrada. Zona: {ZonaId}.",
            ocorrencia.Id,
            ocorrencia.Tipo,
            ocorrencia.ZonaId ?? "fora das zonas mapeadas");

        return ocorrencia.ParaResposta();
    }

    public async Task RemoverAsync(long id, CancellationToken cancellationToken = default)
    {
        var ocorrencia = await ObterEntidadeAsync(id, cancellationToken);
        _ocorrencias.Remover(ocorrencia);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Ocorrência {OcorrenciaId} removida (moderação).", id);
    }

    private async Task<Ocorrencia> ObterEntidadeAsync(long id, CancellationToken cancellationToken) =>
        await _ocorrencias.ObterPorIdAsync(id, cancellationToken)
        ?? throw new RecursoNaoEncontradoException(Recurso, id);
}
