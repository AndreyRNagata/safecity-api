using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;

namespace SafeCity.Tests.Suporte;

/// <summary>Atalhos para montar objetos de teste com valores conhecidos.</summary>
public static class Fabricas
{
    /// <summary>Centro da Estação República (dados simulados).</summary>
    public static readonly Coordenada Republica = new(-23.5442, -46.6424);

    /// <summary>Metros por grau de latitude na fórmula de Haversine usada pelo domínio.</summary>
    private const double MetrosPorGrau = 6_371_000 * Math.PI / 180;

    /// <summary>Ponto a <paramref name="metros"/> ao norte da coordenada.</summary>
    public static Coordenada AoNorte(this Coordenada origem, double metros) =>
        new(origem.Latitude + metros / MetrosPorGrau, origem.Longitude);

    /// <summary>Previsão com o mesmo risco em todas as horas, exceto as informadas.</summary>
    public static PrevisaoHoraria Previsao(double valorPadrao, params (int Hora, double Risco)[] excecoes)
    {
        var valores = Enumerable.Repeat(valorPadrao, PrevisaoHoraria.TotalDeHoras).ToArray();
        foreach (var (hora, risco) in excecoes)
        {
            valores[hora] = risco;
        }

        return new PrevisaoHoraria(valores);
    }

    public static ZonaRisco Zona(
        string id = "republica",
        Coordenada? centro = null,
        int raioMetros = 300,
        PrevisaoHoraria? previsao = null,
        string nome = "Estação República") =>
        new(
            id,
            nome,
            "República",
            TipoLocal.Estacao,
            centro ?? Republica,
            raioMetros,
            previsao ?? Previsao(0.5),
            ocorrenciasUltimos30Dias: 100,
            dicas: new[] { "Guarde o celular antes da catraca." });

    public static Ocorrencia Ocorrencia(
        TipoOcorrencia tipo,
        Coordenada local,
        DateTimeOffset? ocorridoEm = null,
        DateTimeOffset? agora = null)
    {
        var referencia = agora ?? RelogioFixo.Padrao;
        return OcorrenciaFactory.Criar(tipo, local, ocorridoEm ?? referencia.AddHours(-1), referencia);
    }
}
