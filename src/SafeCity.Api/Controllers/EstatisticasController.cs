using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SafeCity.Application.Dtos;
using SafeCity.Application.Servicos;

namespace SafeCity.Api.Controllers;

/// <summary>Indicadores dos relatos para gestores públicos e para a comunidade.</summary>
[ApiController]
[Route("api/estatisticas")]
[Produces("application/json")]
public sealed class EstatisticasController : ControllerBase
{
    private readonly IEstatisticaServico _estatisticas;

    public EstatisticasController(IEstatisticaServico estatisticas)
    {
        _estatisticas = estatisticas;
    }

    /// <summary>Totais por tipo, por zona e por período do dia, e as zonas mais críticas agora.</summary>
    /// <param name="dias">Período analisado, em dias (1 a 365; padrão: 30).</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet]
    [ProducesResponseType(typeof(EstatisticasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EstatisticasResponse>> Obter(
        [FromQuery, Range(1, 365)] int dias = 30,
        CancellationToken cancellationToken = default) =>
        Ok(await _estatisticas.ObterResumoAsync(dias, cancellationToken));
}
