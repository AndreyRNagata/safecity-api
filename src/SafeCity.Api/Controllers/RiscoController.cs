using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SafeCity.Application.Dtos;
using SafeCity.Application.Servicos;

namespace SafeCity.Api.Controllers;

/// <summary>Avaliação do risco de furto de celular em um ponto do mapa.</summary>
[ApiController]
[Route("api/risco")]
[Produces("application/json")]
public sealed class RiscoController : ControllerBase
{
    private readonly IRiscoServico _risco;

    public RiscoController(IRiscoServico risco)
    {
        _risco = risco;
    }

    /// <summary>"Qual o risco aqui, agora?"</summary>
    /// <remarks>
    /// Combina a previsão do modelo de ML da zona mais influente (ajustada pela distância) com os
    /// relatos recentes de cidadãos por perto. A resposta explica cada fator e traz a previsão
    /// das próximas horas. Exemplo (Estação República às 18h):
    /// <c>?latitude=-23.5442&amp;longitude=-46.6424&amp;hora=18</c>
    /// </remarks>
    /// <param name="latitude">Latitude do ponto.</param>
    /// <param name="longitude">Longitude do ponto.</param>
    /// <param name="hora">Hora a avaliar (0 a 23). Se não informada, usa a hora atual de São Paulo.</param>
    /// <param name="cancellationToken">Cancelado se o cliente desistir da requisição.</param>
    [HttpGet]
    [ProducesResponseType(typeof(AvaliacaoRiscoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvaliacaoRiscoResponse>> Avaliar(
        [FromQuery, Required, Range(-90, 90)] double? latitude,
        [FromQuery, Required, Range(-180, 180)] double? longitude,
        [FromQuery, Range(0, 23)] int? hora,
        CancellationToken cancellationToken) =>
        Ok(await _risco.AvaliarAsync(latitude!.Value, longitude!.Value, hora, cancellationToken));
}
