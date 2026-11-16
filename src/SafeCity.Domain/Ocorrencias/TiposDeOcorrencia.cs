using SafeCity.Domain.ValueObjects;

namespace SafeCity.Domain.Ocorrencias;

/// <summary>O celular foi levado sem violência nem ameaça (ex.: do bolso, da mão, da mesa).</summary>
public sealed class Furto : Ocorrencia
{
    private Furto()
    {
    }

    public Furto(Coordenada local, DateTimeOffset ocorridoEm, DateTimeOffset agora)
        : base(TipoOcorrencia.Furto, local, ocorridoEm, agora)
    {
    }

    public override double PesoNoRisco => 1.0;

    public override bool EnvolveViolencia => false;

    public override string Descricao => "Furto: o celular foi levado sem violência nem ameaça.";
}

/// <summary>O celular foi levado com violência ou ameaça. É o tipo que mais pesa no risco.</summary>
public sealed class Roubo : Ocorrencia
{
    private Roubo()
    {
    }

    public Roubo(Coordenada local, DateTimeOffset ocorridoEm, DateTimeOffset agora)
        : base(TipoOcorrencia.Roubo, local, ocorridoEm, agora)
    {
    }

    public override double PesoNoRisco => 1.5;

    public override bool EnvolveViolencia => true;

    public override string Descricao => "Roubo: o celular foi levado com violência ou ameaça.";
}

/// <summary>Alguém tentou levar o celular, mas não conseguiu.</summary>
public sealed class Tentativa : Ocorrencia
{
    private Tentativa()
    {
    }

    public Tentativa(Coordenada local, DateTimeOffset ocorridoEm, DateTimeOffset agora)
        : base(TipoOcorrencia.Tentativa, local, ocorridoEm, agora)
    {
    }

    public override double PesoNoRisco => 0.75;

    public override bool EnvolveViolencia => false;

    public override string Descricao => "Tentativa: alguém tentou levar o celular, mas não conseguiu.";
}

/// <summary>Atitude suspeita observada pelo cidadão. É um sinal fraco, por isso pesa menos.</summary>
public sealed class AtitudeSuspeita : Ocorrencia
{
    private AtitudeSuspeita()
    {
    }

    public AtitudeSuspeita(Coordenada local, DateTimeOffset ocorridoEm, DateTimeOffset agora)
        : base(TipoOcorrencia.Suspeita, local, ocorridoEm, agora)
    {
    }

    public override double PesoNoRisco => 0.5;

    public override bool EnvolveViolencia => false;

    public override string Descricao => "Atitude suspeita: comportamento que indica risco de furto no local.";
}
