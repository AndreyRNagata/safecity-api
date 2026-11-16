using System.ComponentModel.DataAnnotations;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Repositorios;

namespace SafeCity.Application.Dtos;

/// <summary>
/// Novo relato enviado pelo app. Só o necessário: tipo, local aproximado e horário.
/// Nenhum dado pessoal é aceito.
/// </summary>
public sealed record CriarOcorrenciaRequest
{
    /// <summary>furto, roubo, tentativa ou suspeita.</summary>
    /// <example>furto</example>
    [Required]
    public TipoOcorrencia? Tipo { get; init; }

    /// <example>-23.544</example>
    [Required]
    [Range(-90, 90)]
    public double? Latitude { get; init; }

    /// <example>-46.642</example>
    [Required]
    [Range(-180, 180)]
    public double? Longitude { get; init; }

    /// <summary>Quando aconteceu (ISO 8601). Se não for enviado, usa o horário atual.</summary>
    public DateTimeOffset? OcorridoEm { get; init; }
}

/// <summary>Filtros opcionais da listagem de ocorrências (query string).</summary>
public sealed record FiltroOcorrenciasRequest
{
    /// <summary>furto, roubo, tentativa ou suspeita.</summary>
    public TipoOcorrencia? Tipo { get; init; }

    /// <summary>Só os relatos vinculados a esta zona.</summary>
    public string? ZonaId { get; init; }

    /// <summary>Só os relatos a partir desta data (ISO 8601).</summary>
    public DateTimeOffset? Desde { get; init; }

    /// <summary>Latitude do centro da busca por proximidade (use com longitude).</summary>
    [Range(-90, 90)]
    public double? Latitude { get; init; }

    /// <summary>Longitude do centro da busca por proximidade (use com latitude).</summary>
    [Range(-180, 180)]
    public double? Longitude { get; init; }

    /// <summary>Raio da busca por proximidade, em metros (padrão: 1000).</summary>
    [Range(50, 20_000)]
    public double? RaioMetros { get; init; }

    /// <summary>Quantidade máxima de resultados (padrão: 500).</summary>
    [Range(1, 1_000)]
    public int Limite { get; init; } = FiltroOcorrencias.LimitePadrao;
}

/// <summary>Relato anônimo. Os campos tipo, latitude, longitude e ocorridoEm seguem o contrato do app.</summary>
/// <param name="Id">Identificador do relato.</param>
/// <param name="Tipo">furto, roubo, tentativa ou suspeita.</param>
/// <param name="Latitude">Latitude arredondada (~100 m).</param>
/// <param name="Longitude">Longitude arredondada (~100 m).</param>
/// <param name="OcorridoEm">Quando aconteceu (UTC).</param>
/// <param name="ZonaId">Zona de risco à qual o relato foi vinculado, se houver.</param>
/// <param name="Descricao">Explicação do tipo de ocorrência.</param>
/// <param name="EnvolveViolencia">Se o tipo envolve violência ou ameaça.</param>
/// <param name="PesoNoRisco">Quanto o relato pesa no risco em tempo real (furto = 1).</param>
/// <param name="RegistradoEm">Quando o relato chegou à API.</param>
public sealed record OcorrenciaResponse(
    long Id,
    TipoOcorrencia Tipo,
    double Latitude,
    double Longitude,
    DateTimeOffset OcorridoEm,
    string? ZonaId,
    string Descricao,
    bool EnvolveViolencia,
    double PesoNoRisco,
    DateTimeOffset RegistradoEm);
