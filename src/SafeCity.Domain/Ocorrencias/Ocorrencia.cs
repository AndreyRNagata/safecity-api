using SafeCity.Domain.Comum;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;

namespace SafeCity.Domain.Ocorrencias;

/// <summary>Tipos de relato. Na API aparecem como furto, roubo, tentativa e suspeita.</summary>
public enum TipoOcorrencia
{
    Furto = 1,
    Roubo = 2,
    Tentativa = 3,
    Suspeita = 4,
}

/// <summary>
/// Relato anônimo de um cidadão. É uma classe abstrata: cada tipo concreto
/// (<see cref="Furto"/>, <see cref="Roubo"/>, <see cref="Tentativa"/>, <see cref="AtitudeSuspeita"/>)
/// define quanto pesa no risco em tempo real (polimorfismo).
/// </summary>
/// <remarks>
/// Privacidade: não há nenhum dado pessoal aqui. A localização é sempre arredondada (~100 m).
/// </remarks>
public abstract class Ocorrencia : Entidade<long>
{
    /// <summary>Casas decimais mantidas na localização (3 ≈ 100 m).</summary>
    public const int CasasDecimaisDoLocal = 3;

    /// <summary>Margem para diferenças de relógio entre o celular e o servidor.</summary>
    public static readonly TimeSpan ToleranciaParaFuturo = TimeSpan.FromMinutes(5);

    /// <summary>Relatos mais antigos que isso não ajudam a prever o risco de agora.</summary>
    public static readonly TimeSpan IdadeMaxima = TimeSpan.FromDays(30);

    /// <summary>Construtor usado pelo Entity Framework Core.</summary>
    protected Ocorrencia()
    {
    }

    protected Ocorrencia(TipoOcorrencia tipo, Coordenada local, DateTimeOffset ocorridoEm, DateTimeOffset agora)
    {
        Guarda.Contra(local is null, "A localização da ocorrência é obrigatória.");
        Guarda.Contra(ocorridoEm > agora + ToleranciaParaFuturo, "A data da ocorrência não pode estar no futuro.");
        Guarda.Contra(
            ocorridoEm < agora - IdadeMaxima,
            $"Só aceitamos relatos dos últimos {IdadeMaxima.TotalDays:0} dias.");

        Tipo = tipo;
        Local = local!.Aproximada(CasasDecimaisDoLocal);
        OcorridoEm = ocorridoEm.ToUniversalTime();
    }

    public TipoOcorrencia Tipo { get; private set; }

    public Coordenada Local { get; private set; } = null!;

    public DateTimeOffset OcorridoEm { get; private set; }

    /// <summary>Zona de risco mais próxima, quando o relato está dentro do alcance de alguma.</summary>
    public string? ZonaId { get; private set; }

    public ZonaRisco? Zona { get; private set; }

    /// <summary>Quanto o relato pesa no risco em tempo real, em relação a um furto (peso 1).</summary>
    public abstract double PesoNoRisco { get; }

    public abstract bool EnvolveViolencia { get; }

    /// <summary>Explicação em linguagem simples do que é este tipo de ocorrência.</summary>
    public abstract string Descricao { get; }

    public void VincularZona(ZonaRisco? zona) => ZonaId = zona?.Id;

    /// <summary>Indica se o relato aconteceu dentro da janela de tempo informada.</summary>
    public bool EhRecente(DateTimeOffset agora, TimeSpan janela) =>
        OcorridoEm >= agora - janela && OcorridoEm <= agora + ToleranciaParaFuturo;
}
