using System.ComponentModel.DataAnnotations;

namespace SafeCity.Application.Risco;

/// <summary>
/// Parâmetros do cálculo de risco, lidos da seção "Risco" do appsettings.json (Options pattern).
/// Os valores padrão são os mesmos do app mobile.
/// </summary>
public sealed class OpcoesRisco
{
    public const string Secao = "Risco";

    /// <summary>Raio em que relatos recentes aumentam o risco.</summary>
    [Range(50, 5_000)]
    public double RaioRelatosMetros { get; set; } = 400;

    /// <summary>Por quantos dias um relato ainda conta como recente.</summary>
    [Range(1, 30)]
    public int JanelaRelatosDias { get; set; } = 7;

    /// <summary>Acréscimo no risco por relato de furto (os outros tipos multiplicam pelo seu peso).</summary>
    [Range(0, 1)]
    public double BonusPorRelato { get; set; } = 0.05;

    /// <summary>Limite do acréscimo somado de todos os relatos.</summary>
    [Range(0, 1)]
    public double BonusMaximo { get; set; } = 0.15;

    /// <summary>Distância até a zona mais próxima para considerar que o ponto está na área coberta.</summary>
    [Range(100, 100_000)]
    public double DistanciaCoberturaMetros { get; set; } = 15_000;

    /// <summary>Quantas horas à frente entram na previsão da resposta de risco.</summary>
    [Range(1, 24)]
    public int HorasDePrevisao { get; set; } = 6;

    public TimeSpan JanelaRelatos => TimeSpan.FromDays(JanelaRelatosDias);
}
