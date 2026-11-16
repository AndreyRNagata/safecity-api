using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SafeCity.Application.Dtos;
using SafeCity.Application.Servicos;
using SafeCity.Domain.Zonas;

namespace SafeCity.Api.Controllers;

/// <summary>Zonas de risco de furto de celular e a previsão por hora do modelo de ML.</summary>
[ApiController]
[Route("api/zonas")]
[Produces("application/json")]
public sealed class ZonasController : ControllerBase
{
    private readonly IZonaServico _zonas;
    private readonly IOcorrenciaServico _ocorrencias;

    public ZonasController(IZonaServico zonas, IOcorrenciaServico ocorrencias)
    {
        _zonas = zonas;
        _ocorrencias = ocorrencias;
    }

    /// <summary>Lista as zonas de risco (contrato consumido pelo app SafeCity).</summary>
    /// <param name="bairro">Filtra pelo nome do bairro (busca parcial). Ex.: "Sé".</param>
    /// <param name="tipo">Filtra pelo tipo de local: estacao, terminal, avenida, rua ou praca.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ZonaRiscoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ZonaRiscoResponse>>> Listar(
        [FromQuery] string? bairro,
        [FromQuery] TipoLocal? tipo,
        CancellationToken cancellationToken) =>
        Ok(await _zonas.ListarAsync(bairro, tipo, cancellationToken));

    /// <summary>Ranking das zonas mais perigosas em uma hora.</summary>
    /// <param name="hora">Hora do dia (0 a 23). Se não informada, usa a hora atual de São Paulo.</param>
    /// <param name="limite">Quantas zonas devolver (padrão: 5).</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet("ranking")]
    [ProducesResponseType(typeof(IReadOnlyList<RankingZonaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<RankingZonaResponse>>> Ranking(
        [FromQuery, Range(0, 23)] int? hora,
        [FromQuery, Range(1, 50)] int limite = 5,
        CancellationToken cancellationToken = default) =>
        Ok(await _zonas.RankingAsync(hora, limite, cancellationToken));

    /// <summary>Detalhe de uma zona.</summary>
    /// <param name="id">Id da zona, ex.: "republica".</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ZonaRiscoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZonaRiscoResponse>> Obter(string id, CancellationToken cancellationToken) =>
        Ok(await _zonas.ObterAsync(id, cancellationToken));

    /// <summary>Previsão das 24 horas da zona, com nível de risco e horários críticos.</summary>
    /// <param name="id">Id da zona.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet("{id}/previsao")]
    [ProducesResponseType(typeof(PrevisaoZonaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrevisaoZonaResponse>> Previsao(string id, CancellationToken cancellationToken) =>
        Ok(await _zonas.ObterPrevisaoAsync(id, cancellationToken));

    /// <summary>Relatos de cidadãos vinculados à zona, do mais recente para o mais antigo.</summary>
    /// <param name="id">Id da zona.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet("{id}/ocorrencias")]
    [ProducesResponseType(typeof(IReadOnlyList<OcorrenciaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OcorrenciaResponse>>> Ocorrencias(string id, CancellationToken cancellationToken) =>
        Ok(await _ocorrencias.ListarDaZonaAsync(id, cancellationToken));

    /// <summary>Cadastra uma nova zona de risco.</summary>
    /// <param name="requisicao">Dados da zona, incluindo as 24 previsões do modelo.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ZonaRiscoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ZonaRiscoResponse>> Criar(
        [FromBody] CriarZonaRequest requisicao,
        CancellationToken cancellationToken)
    {
        var criada = await _zonas.CriarAsync(requisicao, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    /// <summary>Atualiza todos os dados de uma zona.</summary>
    /// <param name="id">Id da zona.</param>
    /// <param name="requisicao">Novos dados da zona.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ZonaRiscoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZonaRiscoResponse>> Atualizar(
        string id,
        [FromBody] DadosZonaRequest requisicao,
        CancellationToken cancellationToken) =>
        Ok(await _zonas.AtualizarAsync(id, requisicao, cancellationToken));

    /// <summary>Recebe uma nova previsão do modelo de Machine Learning para a zona.</summary>
    /// <remarks>Usado pelo pipeline de IA para publicar o <c>riscoPorHora</c> treinado com os dados da SSP-SP.</remarks>
    /// <param name="id">Id da zona.</param>
    /// <param name="requisicao">24 valores de 0 a 1.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpPut("{id}/previsao")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ZonaRiscoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ZonaRiscoResponse>> AtualizarPrevisao(
        string id,
        [FromBody] AtualizarPrevisaoRequest requisicao,
        CancellationToken cancellationToken) =>
        Ok(await _zonas.AtualizarPrevisaoAsync(id, requisicao, cancellationToken));

    /// <summary>Remove uma zona. Os relatos vinculados continuam salvos, sem zona.</summary>
    /// <param name="id">Id da zona.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(string id, CancellationToken cancellationToken)
    {
        await _zonas.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
