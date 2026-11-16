using Microsoft.Extensions.Options;
using SafeCity.Application.Risco;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Risco;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;
using SafeCity.Tests.Suporte;

namespace SafeCity.Tests.Aplicacao;

public class CalculadoraRiscoTests
{
    private const int Hora = 18;
    private static readonly DateTimeOffset Agora = RelogioFixo.Padrao;
    private static readonly IOptions<OpcoesRisco> Opcoes = Options.Create(new OpcoesRisco());

    private static CalculadoraRisco CriarCalculadora(params IFatorRisco[] fatoresExtras) =>
        new(new IFatorRisco[] { new FatorPrevisaoModelo(), new FatorRelatosRecentes(Opcoes) }.Concat(fatoresExtras), Opcoes);

    private static ContextoAvaliacao Contexto(
        Coordenada posicao,
        IEnumerable<ZonaRisco> zonas,
        IEnumerable<Ocorrencia>? ocorrencias = null) =>
        new(posicao, Hora, Agora, zonas.ToList(), (ocorrencias ?? Enumerable.Empty<Ocorrencia>()).ToList());

    [Fact]
    public void Avaliar_SemZonasMapeadas_DevolveRiscoBaixoForaDaArea()
    {
        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica, Array.Empty<ZonaRisco>()));

        Assert.Equal(0, resultado.Score);
        Assert.Equal(NivelRisco.Baixo, resultado.Nivel);
        Assert.Null(resultado.ZonaDeReferencia);
        Assert.False(resultado.DentroDaArea);
    }

    [Fact]
    public void Avaliar_DentroDaZonaNoHorarioDePico_UsaAPrevisaoDoModelo()
    {
        var zona = Fabricas.Zona(previsao: Fabricas.Previsao(0.2, (Hora, 0.9)));

        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica, new[] { zona }));

        Assert.Equal(0.9, resultado.Score, precision: 4);
        Assert.Equal(NivelRisco.MuitoAlto, resultado.Nivel);
        Assert.Same(zona, resultado.ZonaDeReferencia);
        Assert.True(resultado.DentroDaArea);
    }

    [Fact]
    public void Avaliar_AoDobroDoRaio_ReduzORiscoPelaMetade()
    {
        var zona = Fabricas.Zona(raioMetros: 300, previsao: Fabricas.Previsao(0.8));

        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica.AoNorte(600), new[] { zona }));

        Assert.Equal(0.4, resultado.Score, precision: 3);
        Assert.Equal(NivelRisco.Moderado, resultado.Nivel);
    }

    [Theory]
    [InlineData(TipoOcorrencia.Roubo, 0.575)]     // 0,5 + 0,05 × 1,5
    [InlineData(TipoOcorrencia.Furto, 0.55)]      // 0,5 + 0,05 × 1,0
    [InlineData(TipoOcorrencia.Suspeita, 0.525)]  // 0,5 + 0,05 × 0,5
    public void Avaliar_RelatoRecentePorPerto_SomaBonusPeloPesoDoTipo(TipoOcorrencia tipo, double esperado)
    {
        var zona = Fabricas.Zona(previsao: Fabricas.Previsao(0.5));
        var relato = Fabricas.Ocorrencia(tipo, Fabricas.Republica.AoNorte(100));

        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica, new[] { zona }, new[] { relato }));

        Assert.Equal(esperado, resultado.Score, precision: 4);
        Assert.Equal(1, resultado.RelatosProximos);
    }

    [Fact]
    public void Avaliar_MuitosRelatos_LimitaOBonusEm015()
    {
        var zona = Fabricas.Zona(previsao: Fabricas.Previsao(0.3));
        var relatos = Enumerable.Range(0, 5).Select(_ => Fabricas.Ocorrencia(TipoOcorrencia.Roubo, Fabricas.Republica));

        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica, new[] { zona }, relatos));

        Assert.Equal(0.45, resultado.Score, precision: 4);
        Assert.Equal(5, resultado.RelatosProximos);
    }

    [Fact]
    public void Avaliar_IgnoraRelatosAntigosOuDistantes()
    {
        var zona = Fabricas.Zona(previsao: Fabricas.Previsao(0.3));
        var antigo = Fabricas.Ocorrencia(TipoOcorrencia.Roubo, Fabricas.Republica, ocorridoEm: Agora.AddDays(-8));
        var distante = Fabricas.Ocorrencia(TipoOcorrencia.Roubo, Fabricas.Republica.AoNorte(1_000));

        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica, new[] { zona }, new[] { antigo, distante }));

        Assert.Equal(0.3, resultado.Score, precision: 4);
        Assert.Equal(0, resultado.RelatosProximos);
    }

    [Fact]
    public void Avaliar_NuncaPassaDe1()
    {
        var zona = Fabricas.Zona(previsao: Fabricas.Previsao(0.95));
        var relatos = Enumerable.Range(0, 3).Select(_ => Fabricas.Ocorrencia(TipoOcorrencia.Roubo, Fabricas.Republica));

        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica, new[] { zona }, relatos));

        Assert.Equal(1, resultado.Score);
    }

    [Fact]
    public void Avaliar_ExplicaCadaFator()
    {
        var zona = Fabricas.Zona(previsao: Fabricas.Previsao(0.5));

        var resultado = CriarCalculadora().Avaliar(Contexto(Fabricas.Republica, new[] { zona }));

        Assert.Equal(new[] { "previsao_modelo", "relatos_recentes" }, resultado.Parcelas.Select(p => p.Fator));
        Assert.All(resultado.Parcelas, p => Assert.False(string.IsNullOrWhiteSpace(p.Explicacao)));
    }

    [Fact]
    public void Avaliar_AceitaNovosFatoresSemMudarACalculadora()
    {
        // Strategy + DI: um critério novo entra só implementando IFatorRisco.
        var zona = Fabricas.Zona(previsao: Fabricas.Previsao(0.3));

        var resultado = CriarCalculadora(new FatorFixo(0.2)).Avaliar(Contexto(Fabricas.Republica, new[] { zona }));

        Assert.Equal(0.5, resultado.Score, precision: 4);
        Assert.Contains(resultado.Parcelas, p => p.Fator == "iluminacao");
    }

    private sealed class FatorFixo : IFatorRisco
    {
        private readonly double _valor;

        public FatorFixo(double valor) => _valor = valor;

        public string Nome => "iluminacao";

        public ParcelaRisco Calcular(ContextoAvaliacao contexto) => new(Nome, _valor, "Rua com pouca iluminação.");
    }
}
