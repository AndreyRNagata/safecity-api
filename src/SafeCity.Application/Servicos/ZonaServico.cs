using Microsoft.Extensions.Logging;
using SafeCity.Application.Abstracoes;
using SafeCity.Application.Dtos;
using SafeCity.Application.Mapeamentos;
using SafeCity.Domain.Comum;
using SafeCity.Domain.Repositorios;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Servicos;

/// <summary>Casos de uso das zonas de risco.</summary>
public interface IZonaServico
{
    Task<IReadOnlyList<ZonaRiscoResponse>> ListarAsync(string? bairro, TipoLocal? tipo, CancellationToken cancellationToken = default);

    Task<ZonaRiscoResponse> ObterAsync(string id, CancellationToken cancellationToken = default);

    Task<PrevisaoZonaResponse> ObterPrevisaoAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingZonaResponse>> RankingAsync(int? hora, int limite, CancellationToken cancellationToken = default);

    Task<ZonaRiscoResponse> CriarAsync(CriarZonaRequest requisicao, CancellationToken cancellationToken = default);

    Task<ZonaRiscoResponse> AtualizarAsync(string id, DadosZonaRequest requisicao, CancellationToken cancellationToken = default);

    Task<ZonaRiscoResponse> AtualizarPrevisaoAsync(string id, AtualizarPrevisaoRequest requisicao, CancellationToken cancellationToken = default);

    Task RemoverAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class ZonaServico : IZonaServico
{
    private const string Recurso = "Zona";

    private readonly IZonaRepositorio _zonas;
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho;
    private readonly IRelogio _relogio;
    private readonly ILogger<ZonaServico> _logger;

    public ZonaServico(
        IZonaRepositorio zonas,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IRelogio relogio,
        ILogger<ZonaServico> logger)
    {
        _zonas = zonas;
        _unidadeDeTrabalho = unidadeDeTrabalho;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ZonaRiscoResponse>> ListarAsync(
        string? bairro,
        TipoLocal? tipo,
        CancellationToken cancellationToken = default)
    {
        var zonas = await _zonas.BuscarAsync(bairro, tipo, cancellationToken);
        return zonas.Select(z => z.ParaResposta()).ToList();
    }

    public async Task<ZonaRiscoResponse> ObterAsync(string id, CancellationToken cancellationToken = default) =>
        (await ObterEntidadeAsync(id, cancellationToken)).ParaResposta();

    public async Task<PrevisaoZonaResponse> ObterPrevisaoAsync(string id, CancellationToken cancellationToken = default) =>
        (await ObterEntidadeAsync(id, cancellationToken)).ParaPrevisao();

    public async Task<IReadOnlyList<RankingZonaResponse>> RankingAsync(
        int? hora,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var horaConsulta = PrevisaoHoraria.NormalizarHora(hora ?? _relogio.HoraLocalAtual);
        var zonas = await _zonas.ListarAsync(cancellationToken);
        return RankingDeZonas.Montar(zonas, horaConsulta, limite);
    }

    public async Task<ZonaRiscoResponse> CriarAsync(CriarZonaRequest requisicao, CancellationToken cancellationToken = default)
    {
        var id = requisicao.Id.Trim();
        if (await _zonas.ExisteAsync(id, cancellationToken))
        {
            throw new ConflitoException($"Já existe uma zona com o id '{id}'.");
        }

        var zona = new ZonaRisco(
            id,
            requisicao.Nome,
            requisicao.Bairro,
            requisicao.Tipo!.Value,
            new Coordenada(requisicao.Latitude!.Value, requisicao.Longitude!.Value),
            requisicao.RaioMetros,
            new PrevisaoHoraria(requisicao.RiscoPorHora),
            requisicao.OcorrenciasUltimos30Dias,
            requisicao.Dicas);

        await _zonas.AdicionarAsync(zona, cancellationToken);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Zona {ZonaId} criada.", zona.Id);
        return zona.ParaResposta();
    }

    public async Task<ZonaRiscoResponse> AtualizarAsync(
        string id,
        DadosZonaRequest requisicao,
        CancellationToken cancellationToken = default)
    {
        var zona = await ObterEntidadeAsync(id, cancellationToken);

        zona.AtualizarDados(
            requisicao.Nome,
            requisicao.Bairro,
            requisicao.Tipo!.Value,
            new Coordenada(requisicao.Latitude!.Value, requisicao.Longitude!.Value),
            requisicao.RaioMetros,
            requisicao.Dicas);
        zona.AtualizarPrevisao(new PrevisaoHoraria(requisicao.RiscoPorHora), requisicao.OcorrenciasUltimos30Dias);

        await _unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return zona.ParaResposta();
    }

    public async Task<ZonaRiscoResponse> AtualizarPrevisaoAsync(
        string id,
        AtualizarPrevisaoRequest requisicao,
        CancellationToken cancellationToken = default)
    {
        var zona = await ObterEntidadeAsync(id, cancellationToken);
        zona.AtualizarPrevisao(new PrevisaoHoraria(requisicao.RiscoPorHora), requisicao.OcorrenciasUltimos30Dias);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Previsão da zona {ZonaId} atualizada pelo modelo de ML.", zona.Id);
        return zona.ParaResposta();
    }

    public async Task RemoverAsync(string id, CancellationToken cancellationToken = default)
    {
        var zona = await ObterEntidadeAsync(id, cancellationToken);
        _zonas.Remover(zona);
        await _unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        _logger.LogInformation("Zona {ZonaId} removida.", id);
    }

    private async Task<ZonaRisco> ObterEntidadeAsync(string id, CancellationToken cancellationToken) =>
        await _zonas.ObterPorIdAsync(id, cancellationToken)
        ?? throw new RecursoNaoEncontradoException(Recurso, id);
}
