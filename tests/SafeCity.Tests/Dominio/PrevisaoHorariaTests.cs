using SafeCity.Domain.Comum;
using SafeCity.Domain.ValueObjects;
using SafeCity.Tests.Suporte;

namespace SafeCity.Tests.Dominio;

public class PrevisaoHorariaTests
{
    [Theory]
    [InlineData(23)]
    [InlineData(25)]
    public void Construtor_SemExatamente24Valores_LancaRegraDeNegocio(int quantidade)
    {
        var erro = Assert.Throws<RegraDeNegocioException>(() => new PrevisaoHoraria(new double[quantidade]));
        Assert.Contains("24 valores", erro.Message);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void Construtor_ComRiscoForaDe0a1_InformaAHoraInvalida(double valorInvalido)
    {
        var valores = new double[24];
        valores[18] = valorInvalido;

        var erro = Assert.Throws<RegraDeNegocioException>(() => new PrevisaoHoraria(valores));
        Assert.Contains("18h", erro.Message);
    }

    [Theory]
    [InlineData(24, 0)]
    [InlineData(-1, 23)]
    [InlineData(49, 1)]
    public void RiscoNaHora_NormalizaHorasForaDoIntervalo(int horaInformada, int horaEsperada)
    {
        var previsao = Fabricas.Previsao(0.1, (horaEsperada, 0.9));

        Assert.Equal(0.9, previsao.RiscoNaHora(horaInformada));
    }

    [Fact]
    public void DescreverHorariosCriticos_ComDuasFaixas_JuntaComE()
    {
        var previsao = Fabricas.Previsao(0.2, (7, 0.6), (8, 0.7), (17, 0.8), (18, 0.9), (19, 0.6));

        Assert.Equal("7h–8h e 17h–19h", previsao.DescreverHorariosCriticos());
    }

    [Fact]
    public void DescreverHorariosCriticos_FaixaQueAtravessaAMeiaNoite_ViraUmaSoFaixa()
    {
        var previsao = Fabricas.Previsao(0.1, (22, 0.6), (23, 0.7), (0, 0.6), (1, 0.6));

        Assert.Equal("22h–1h", previsao.DescreverHorariosCriticos());
    }

    [Fact]
    public void DescreverHorariosCriticos_SemRiscoAlto_InformaQueNaoHaHorarioCritico()
    {
        Assert.Equal("nenhum horário crítico previsto", Fabricas.Previsao(0.2).DescreverHorariosCriticos());
    }

    [Fact]
    public void Pico_DevolveAHoraDeMaiorRisco()
    {
        var previsao = Fabricas.Previsao(0.2, (18, 0.9));

        Assert.Equal((18, 0.9), previsao.Pico());
    }

    [Fact]
    public void Igualdade_ComparaPelosValores()
    {
        Assert.Equal(Fabricas.Previsao(0.3), Fabricas.Previsao(0.3));
        Assert.NotEqual(Fabricas.Previsao(0.3), Fabricas.Previsao(0.3, (0, 0.4)));
    }
}
