using System.ComponentModel.DataAnnotations;
using SafeCity.Domain.Risco;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Dtos;

/// <summary>
/// Zona de risco no formato que o app SafeCity consome (mesmo contrato do mock em json-server).
/// </summary>
/// <param name="Id">Identificador legível, ex.: "se" ou "paulista-masp".</param>
/// <param name="Nome">Nome do ponto, ex.: "Estação República".</param>
/// <param name="Bairro">Bairro onde fica o ponto.</param>
/// <param name="Tipo">estacao, terminal, avenida, rua ou praca.</param>
/// <param name="Latitude">Latitude do centro da zona.</param>
/// <param name="Longitude">Longitude do centro da zona.</param>
/// <param name="RaioMetros">Raio da zona em metros.</param>
/// <param name="RiscoPorHora">24 valores de 0 a 1: risco previsto pelo modelo de ML de 0h a 23h.</param>
/// <param name="OcorrenciasUltimos30Dias">Ocorrências oficiais (SSP-SP) no local nos últimos 30 dias.</param>
/// <param name="Dicas">Dicas de prevenção para o local.</param>
/// <param name="AtualizadoEm">Última alteração da zona ou da previsão.</param>
public sealed record ZonaRiscoResponse(
    string Id,
    string Nome,
    string Bairro,
    TipoLocal Tipo,
    double Latitude,
    double Longitude,
    int RaioMetros,
    IReadOnlyList<double> RiscoPorHora,
    int OcorrenciasUltimos30Dias,
    IReadOnlyList<string> Dicas,
    DateTimeOffset? AtualizadoEm);

/// <summary>Dados editáveis de uma zona de risco.</summary>
public record DadosZonaRequest
{
    /// <example>Estação República</example>
    [Required]
    [StringLength(ZonaRisco.TamanhoMaximoNome)]
    public string Nome { get; init; } = string.Empty;

    /// <example>República</example>
    [Required]
    [StringLength(ZonaRisco.TamanhoMaximoBairro)]
    public string Bairro { get; init; } = string.Empty;

    /// <summary>estacao, terminal, avenida, rua ou praca.</summary>
    /// <example>estacao</example>
    [Required]
    public TipoLocal? Tipo { get; init; }

    /// <example>-23.5442</example>
    [Required]
    [Range(-90, 90)]
    public double? Latitude { get; init; }

    /// <example>-46.6424</example>
    [Required]
    [Range(-180, 180)]
    public double? Longitude { get; init; }

    /// <example>300</example>
    [Range(ZonaRisco.RaioMinimoMetros, ZonaRisco.RaioMaximoMetros)]
    public int RaioMetros { get; init; } = 300;

    /// <summary>24 valores de 0 a 1 (0h a 23h), gerados pelo modelo de Machine Learning.</summary>
    [Required]
    [MinLength(PrevisaoHoraria.TotalDeHoras)]
    [MaxLength(PrevisaoHoraria.TotalDeHoras)]
    public double[] RiscoPorHora { get; init; } = Array.Empty<double>();

    /// <example>298</example>
    [Range(0, int.MaxValue)]
    public int OcorrenciasUltimos30Dias { get; init; }

    [MaxLength(ZonaRisco.MaximoDeDicas)]
    public string[] Dicas { get; init; } = Array.Empty<string>();
}

/// <summary>Cadastro de uma nova zona de risco.</summary>
public sealed record CriarZonaRequest : DadosZonaRequest
{
    /// <summary>Identificador legível: letras minúsculas sem acento, números e hífens.</summary>
    /// <example>republica</example>
    [Required]
    [StringLength(ZonaRisco.TamanhoMaximoId, MinimumLength = 2)]
    public string Id { get; init; } = string.Empty;
}

/// <summary>Nova previsão do modelo de Machine Learning para uma zona.</summary>
public sealed record AtualizarPrevisaoRequest
{
    /// <summary>24 valores de 0 a 1 (0h a 23h).</summary>
    [Required]
    [MinLength(PrevisaoHoraria.TotalDeHoras)]
    [MaxLength(PrevisaoHoraria.TotalDeHoras)]
    public double[] RiscoPorHora { get; init; } = Array.Empty<double>();

    /// <summary>Opcional: total atualizado de ocorrências oficiais nos últimos 30 dias.</summary>
    [Range(0, int.MaxValue)]
    public int? OcorrenciasUltimos30Dias { get; init; }
}

/// <summary>Risco previsto em uma hora.</summary>
public sealed record RiscoHoraResponse(int Hora, double Risco, NivelRisco Nivel);

/// <summary>Previsão das 24 horas de uma zona, com os horários críticos em texto.</summary>
/// <param name="ZonaId">Id da zona.</param>
/// <param name="Nome">Nome da zona.</param>
/// <param name="Horas">Risco e nível de 0h a 23h.</param>
/// <param name="HorarioDePico">Hora com o maior risco previsto.</param>
/// <param name="HorariosCriticos">Faixas com risco alto ou muito alto, ex.: "7h–8h e 17h–19h".</param>
/// <param name="RiscoMedio">Média do risco no dia.</param>
public sealed record PrevisaoZonaResponse(
    string ZonaId,
    string Nome,
    IReadOnlyList<RiscoHoraResponse> Horas,
    RiscoHoraResponse HorarioDePico,
    string HorariosCriticos,
    double RiscoMedio);

/// <summary>Posição de uma zona no ranking de risco de uma hora.</summary>
public sealed record RankingZonaResponse(
    int Posicao,
    string ZonaId,
    string Nome,
    string Bairro,
    TipoLocal Tipo,
    double Risco,
    NivelRisco Nivel);
