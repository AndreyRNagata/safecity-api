using SafeCity.Domain.Comum;
using SafeCity.Domain.Risco;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;
using SafeCity.Tests.Suporte;

namespace SafeCity.Tests.Dominio;

public class CoordenadaTests
{
    [Fact]
    public void DistanciaEmMetrosAte_SeERepublica_FicaPertoDe1Km()
    {
        var se = new Coordenada(-23.5503, -46.6339);

        var distancia = se.DistanciaEmMetrosAte(Fabricas.Republica);

        Assert.InRange(distancia, 1_000, 1_200);
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, 181)]
    [InlineData(double.NaN, 0)]
    public void Construtor_ForaDosLimites_LancaRegraDeNegocio(double latitude, double longitude)
    {
        Assert.Throws<RegraDeNegocioException>(() => new Coordenada(latitude, longitude));
    }

    [Fact]
    public void Aproximada_ArredondaPara3CasasDecimais()
    {
        var exata = new Coordenada(-23.544234, -46.642411);

        Assert.Equal(new Coordenada(-23.544, -46.642), exata.Aproximada());
    }

    [Fact]
    public void AreaAoRedor_ContemOPontoAoNorteDentroDoRaio()
    {
        var area = Fabricas.Republica.AreaAoRedor(500);
        var ponto = Fabricas.Republica.AoNorte(400);

        Assert.InRange(ponto.Latitude, area.LatitudeMinima, area.LatitudeMaxima);
        Assert.InRange(ponto.Longitude, area.LongitudeMinima, area.LongitudeMaxima);
    }
}

public class ZonaRiscoTests
{
    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(300, 1.0)]
    [InlineData(600, 0.5)]
    [InlineData(900, 0.0)]
    [InlineData(2_000, 0.0)]
    public void FatorProximidade_CaiDe1Ate0Entre1xE3xORaio(double distancia, double esperado)
    {
        var zona = Fabricas.Zona(raioMetros: 300);

        Assert.Equal(esperado, zona.FatorProximidade(distancia), precision: 6);
    }

    [Fact]
    public void RiscoPara_MultiplicaPrevisaoDaHoraPelaProximidade()
    {
        var zona = Fabricas.Zona(raioMetros: 300, previsao: Fabricas.Previsao(0.2, (18, 0.8)));
        var aSeiscentosMetros = Fabricas.Republica.AoNorte(600);

        Assert.Equal(0.4, zona.RiscoPara(aSeiscentosMetros, hora: 18), precision: 3);
    }

    [Theory]
    [InlineData("Estação Sé")]
    [InlineData("se_centro")]
    [InlineData("-se")]
    public void Construtor_ComIdForaDoPadrao_LancaRegraDeNegocio(string id)
    {
        Assert.Throws<RegraDeNegocioException>(() => Fabricas.Zona(id: id));
    }

    [Theory]
    [InlineData(49)]
    [InlineData(5_001)]
    public void Construtor_ComRaioForaDoLimite_LancaRegraDeNegocio(int raio)
    {
        Assert.Throws<RegraDeNegocioException>(() => Fabricas.Zona(raioMetros: raio));
    }

    [Fact]
    public void AtualizarPrevisao_ComTotalNegativo_LancaRegraDeNegocio()
    {
        var zona = Fabricas.Zona();

        Assert.Throws<RegraDeNegocioException>(() => zona.AtualizarPrevisao(Fabricas.Previsao(0.1), -1));
    }

    [Fact]
    public void LocalizadorDeZonas_EscolheAZonaMaisProximaEmProporcaoAoRaio()
    {
        var grande = Fabricas.Zona(id: "grande", centro: Fabricas.Republica, raioMetros: 1_000);
        var pequena = Fabricas.Zona(id: "pequena", centro: Fabricas.Republica.AoNorte(500), raioMetros: 100);
        var ponto = Fabricas.Republica.AoNorte(400);

        // 400 m da grande (0,4 raio) e 100 m da pequena (1,0 raio): a grande "abraça" melhor o ponto.
        Assert.Same(grande, LocalizadorDeZonas.ZonaQueAbrange(new[] { pequena, grande }, ponto));
    }

    [Fact]
    public void LocalizadorDeZonas_ForaDoAlcanceDeTodas_DevolveNulo()
    {
        var zona = Fabricas.Zona(raioMetros: 300);

        Assert.Null(LocalizadorDeZonas.ZonaQueAbrange(new[] { zona }, Fabricas.Republica.AoNorte(5_000)));
    }
}

public class ClassificadorRiscoTests
{
    [Theory]
    [InlineData(0.00, NivelRisco.Baixo)]
    [InlineData(0.29, NivelRisco.Baixo)]
    [InlineData(0.30, NivelRisco.Moderado)]
    [InlineData(0.55, NivelRisco.Alto)]
    [InlineData(0.75, NivelRisco.MuitoAlto)]
    [InlineData(1.00, NivelRisco.MuitoAlto)]
    public void Classificar_UsaOsMesmosLimitesDoApp(double score, NivelRisco esperado)
    {
        Assert.Equal(esperado, ClassificadorRisco.Classificar(score));
    }
}
