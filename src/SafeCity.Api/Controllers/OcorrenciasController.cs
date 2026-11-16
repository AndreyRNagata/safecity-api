using Microsoft.AspNetCore.Mvc;
using SafeCity.Application.Dtos;
using SafeCity.Application.Servicos;

namespace SafeCity.Api.Controllers;

/// <summary>Relatos anônimos de furto, roubo, tentativa ou atitude suspeita enviados pelos cidadãos.</summary>
[ApiController]
[Route("api/ocorrencias")]
[Produces("application/json")]
public sealed class OcorrenciasController : ControllerBase
{
    private readonly IOcorrenciaServico _ocorrencias;

    public OcorrenciasController(IOcorrenciaServico ocorrencias)
    {
        _ocorrencias = ocorrencias;
    }

    /// <summary>Lista os relatos (contrato consumido pelo app SafeCity), com filtros opcionais.</summary>
    /// <remarks>
    /// Exemplos: <c>?tipo=roubo</c>, <c>?zonaId=se</c>,
    /// <c>?latitude=-23.5442&amp;longitude=-46.6424&amp;raioMetros=500</c>.
    /// </remarks>
    /// <param name="filtro">Filtros por tipo, zona, data e proximidade.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OcorrenciaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<OcorrenciaResponse>>> Listar(
        [FromQuery] FiltroOcorrenciasRequest filtro,
        CancellationToken cancellationToken) =>
        Ok(await _ocorrencias.ListarAsync(filtro, cancellationToken));

    /// <summary>Detalhe de um relato.</summary>
    /// <param name="id">Id do relato.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(OcorrenciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OcorrenciaResponse>> Obter(long id, CancellationToken cancellationToken) =>
        Ok(await _ocorrencias.ObterAsync(id, cancellationToken));

    /// <summary>Registra um novo relato enviado pelo app.</summary>
    /// <remarks>
    /// A localização é arredondada para ~100 m antes de ser salva e o relato é vinculado
    /// automaticamente à zona de risco mais próxima, se houver.
    /// </remarks>
    /// <param name="requisicao">Tipo, local aproximado e horário. Nenhum dado pessoal.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OcorrenciaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OcorrenciaResponse>> Registrar(
        [FromBody] CriarOcorrenciaRequest requisicao,
        CancellationToken cancellationToken)
    {
        var criada = await _ocorrencias.RegistrarAsync(requisicao, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    /// <summary>Remove um relato (moderação de conteúdo falso ou duplicado).</summary>
    /// <param name="id">Id do relato.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remover(long id, CancellationToken cancellationToken)
    {
        await _ocorrencias.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
