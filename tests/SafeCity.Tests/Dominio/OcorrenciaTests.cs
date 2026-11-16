using SafeCity.Domain.Comum;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.ValueObjects;
using SafeCity.Tests.Suporte;

namespace SafeCity.Tests.Dominio;

public class OcorrenciaTests
{
    private static readonly DateTimeOffset Agora = RelogioFixo.Padrao;

    [Theory]
    [InlineData(TipoOcorrencia.Furto, typeof(Furto))]
    [InlineData(TipoOcorrencia.Roubo, typeof(Roubo))]
    [InlineData(TipoOcorrencia.Tentativa, typeof(Tentativa))]
    [InlineData(TipoOcorrencia.Suspeita, typeof(AtitudeSuspeita))]
    public void Factory_CriaASubclasseDoTipoInformado(TipoOcorrencia tipo, Type classeEsperada)
    {
        var ocorrencia = OcorrenciaFactory.Criar(tipo, Fabricas.Republica, Agora, Agora);

        Assert.IsType(classeEsperada, ocorrencia);
        Assert.Equal(tipo, ocorrencia.Tipo);
    }

    [Fact]
    public void Factory_ComTipoDesconhecido_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(
            () => OcorrenciaFactory.Criar((TipoOcorrencia)99, Fabricas.Republica, Agora, Agora));
    }

    [Fact]
    public void PesoNoRisco_RouboPesaMaisQueFurtoQuePesaMaisQueTentativaESuspeita()
    {
        // Polimorfismo: a mesma propriedade, um valor diferente para cada subclasse.
        var pesos = Enum.GetValues<TipoOcorrencia>()
            .ToDictionary(tipo => tipo, tipo => Fabricas.Ocorrencia(tipo, Fabricas.Republica).PesoNoRisco);

        Assert.True(pesos[TipoOcorrencia.Roubo] > pesos[TipoOcorrencia.Furto]);
        Assert.True(pesos[TipoOcorrencia.Furto] > pesos[TipoOcorrencia.Tentativa]);
        Assert.True(pesos[TipoOcorrencia.Tentativa] > pesos[TipoOcorrencia.Suspeita]);
    }

    [Fact]
    public void Construtor_ArredondaALocalizacaoParaProtegerQuemRelata()
    {
        var ocorrencia = Fabricas.Ocorrencia(TipoOcorrencia.Furto, new Coordenada(-23.544234, -46.642411));

        Assert.Equal(new Coordenada(-23.544, -46.642), ocorrencia.Local);
    }

    [Fact]
    public void Construtor_ComDataNoFuturo_LancaRegraDeNegocio()
    {
        var erro = Assert.Throws<RegraDeNegocioException>(
            () => Fabricas.Ocorrencia(TipoOcorrencia.Furto, Fabricas.Republica, ocorridoEm: Agora.AddHours(1)));

        Assert.Contains("futuro", erro.Message);
    }

    [Fact]
    public void Construtor_AceitaPequenaDiferencaDeRelogioDoCelular()
    {
        var ocorrencia = Fabricas.Ocorrencia(TipoOcorrencia.Furto, Fabricas.Republica, ocorridoEm: Agora.AddMinutes(2));

        Assert.Equal(Agora.AddMinutes(2), ocorrencia.OcorridoEm);
    }

    [Fact]
    public void Construtor_ComRelatoMaisAntigoQue30Dias_LancaRegraDeNegocio()
    {
        Assert.Throws<RegraDeNegocioException>(
            () => Fabricas.Ocorrencia(TipoOcorrencia.Roubo, Fabricas.Republica, ocorridoEm: Agora.AddDays(-31)));
    }

    [Fact]
    public void Construtor_GuardaADataEmUtc()
    {
        var horarioDeSaoPaulo = new DateTimeOffset(2026, 11, 13, 17, 0, 0, TimeSpan.FromHours(-3));

        var ocorrencia = Fabricas.Ocorrencia(TipoOcorrencia.Furto, Fabricas.Republica, ocorridoEm: horarioDeSaoPaulo);

        Assert.Equal(TimeSpan.Zero, ocorrencia.OcorridoEm.Offset);
        Assert.Equal(20, ocorrencia.OcorridoEm.Hour);
    }
}
