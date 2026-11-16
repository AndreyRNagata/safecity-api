namespace SafeCity.Domain.Risco;

/// <summary>Nível de risco mostrado ao cidadão. Na API aparece como baixo, moderado, alto ou muito_alto.</summary>
public enum NivelRisco
{
    Baixo = 1,
    Moderado = 2,
    Alto = 3,
    MuitoAlto = 4,
}

/// <summary>
/// Converte o índice de risco (0 a 1) em um nível. Os limites são os mesmos usados no app mobile.
/// </summary>
public static class ClassificadorRisco
{
    public const double LimiteModerado = 0.30;
    public const double LimiteAlto = 0.55;
    public const double LimiteMuitoAlto = 0.75;

    public static NivelRisco Classificar(double score) => score switch
    {
        >= LimiteMuitoAlto => NivelRisco.MuitoAlto,
        >= LimiteAlto => NivelRisco.Alto,
        >= LimiteModerado => NivelRisco.Moderado,
        _ => NivelRisco.Baixo,
    };
}
