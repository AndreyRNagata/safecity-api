namespace SafeCity.Infrastructure.Configuracao;

/// <summary>Configurações gerais, lidas da seção "SafeCity" do appsettings.json.</summary>
public sealed class OpcoesSafeCity
{
    public const string Secao = "SafeCity";

    /// <summary>Fuso horário da cidade (IANA). As previsões por hora usam o horário local.</summary>
    public string FusoHorario { get; set; } = "America/Sao_Paulo";

    /// <summary>Quando o banco está vazio, carrega as zonas e os relatos simulados.</summary>
    public bool PopularDadosIniciais { get; set; } = true;
}
