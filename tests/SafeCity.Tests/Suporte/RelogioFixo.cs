using SafeCity.Application.Abstracoes;

namespace SafeCity.Tests.Suporte;

/// <summary>Relógio parado em um instante conhecido, com o fuso de São Paulo (UTC-3).</summary>
public sealed class RelogioFixo : IRelogio
{
    /// <summary>13/11/2026 às 18h em São Paulo (21h UTC): horário de pico no centro.</summary>
    public static readonly DateTimeOffset Padrao = new(2026, 11, 13, 21, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan FusoSaoPaulo = TimeSpan.FromHours(-3);

    public RelogioFixo(DateTimeOffset? agora = null)
    {
        Agora = agora ?? Padrao;
    }

    public DateTimeOffset Agora { get; set; }

    public DateTimeOffset ParaHorarioLocal(DateTimeOffset instante) => instante.ToOffset(FusoSaoPaulo);
}
