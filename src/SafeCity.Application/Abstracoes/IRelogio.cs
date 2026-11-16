namespace SafeCity.Application.Abstracoes;

/// <summary>
/// Fonte da data e hora atual. Usar uma interface (em vez de DateTime.Now direto)
/// permite testar o cálculo de risco com um horário fixo.
/// </summary>
public interface IRelogio
{
    /// <summary>Instante atual em UTC.</summary>
    DateTimeOffset Agora { get; }

    /// <summary>Converte um instante para o horário local da cidade (São Paulo).</summary>
    DateTimeOffset ParaHorarioLocal(DateTimeOffset instante);

    /// <summary>Hora local atual, de 0 a 23 (implementação padrão na própria interface).</summary>
    int HoraLocalAtual => ParaHorarioLocal(Agora).Hour;
}
