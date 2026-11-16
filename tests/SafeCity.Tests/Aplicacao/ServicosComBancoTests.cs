using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SafeCity.Application.Dtos;
using SafeCity.Application.Servicos;
using SafeCity.Domain.Comum;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Zonas;
using SafeCity.Infrastructure.Dados;
using SafeCity.Infrastructure.Repositorios;
using SafeCity.Tests.Suporte;

namespace SafeCity.Tests.Aplicacao;

/// <summary>Serviços rodando contra um SQLite em memória (EF Core + LINQ de verdade).</summary>
public sealed class ServicosComBancoTests : IDisposable
{
    private readonly BancoEmMemoria _banco = new();

    public ServicosComBancoTests()
    {
        using var contexto = _banco.CriarContexto();
        contexto.Zonas.AddRange(
            Fabricas.Zona(id: "republica", previsao: Fabricas.Previsao(0.3, (18, 0.9))),
            Fabricas.Zona(id: "se", nome: "Praça da Sé", centro: new(-23.5503, -46.6339), previsao: Fabricas.Previsao(0.4, (18, 0.7))));
        contexto.SaveChanges();
    }

    public void Dispose() => _banco.Dispose();

    private OcorrenciaServico CriarServicoDeOcorrencias(SafeCityDbContext contexto) => new(
        new OcorrenciaRepositorio(contexto),
        new ZonaRepositorio(contexto),
        contexto,
        _banco.Relogio,
        NullLogger<OcorrenciaServico>.Instance);

    private ZonaServico CriarServicoDeZonas(SafeCityDbContext contexto) => new(
        new ZonaRepositorio(contexto),
        contexto,
        _banco.Relogio,
        NullLogger<ZonaServico>.Instance);

    [Fact]
    public async Task RegistrarAsync_VinculaAZonaMaisProximaEArredondaOLocal()
    {
        await using var contexto = _banco.CriarContexto();

        var criada = await CriarServicoDeOcorrencias(contexto).RegistrarAsync(new CriarOcorrenciaRequest
        {
            Tipo = TipoOcorrencia.Roubo,
            Latitude = -23.544234,
            Longitude = -46.642411,
        });

        Assert.Equal("republica", criada.ZonaId);
        Assert.Equal(-23.544, criada.Latitude);
        Assert.Equal(-46.642, criada.Longitude);
        Assert.Equal(_banco.Relogio.Agora, criada.RegistradoEm);
        Assert.Equal(_banco.Relogio.Agora, criada.OcorridoEm);
    }

    [Fact]
    public async Task RegistrarAsync_ForaDasZonas_FicaSemZona()
    {
        await using var contexto = _banco.CriarContexto();

        var criada = await CriarServicoDeOcorrencias(contexto).RegistrarAsync(new CriarOcorrenciaRequest
        {
            Tipo = TipoOcorrencia.Furto,
            Latitude = -23.6,
            Longitude = -46.8,
        });

        Assert.Null(criada.ZonaId);
    }

    [Fact]
    public async Task Tph_LeCadaRelatoComASubclasseCerta()
    {
        await using (var contexto = _banco.CriarContexto())
        {
            contexto.Ocorrencias.AddRange(Enum.GetValues<TipoOcorrencia>()
                .Select(tipo => Fabricas.Ocorrencia(tipo, Fabricas.Republica)));
            await contexto.SaveChangesAsync();
        }

        await using var novoContexto = _banco.CriarContexto();
        var lidas = await novoContexto.Ocorrencias.ToDictionaryAsync(o => o.Tipo);

        Assert.Equal(4, lidas.Count);
        Assert.IsType<Furto>(lidas[TipoOcorrencia.Furto]);
        Assert.IsType<Roubo>(lidas[TipoOcorrencia.Roubo]);
        Assert.IsType<Tentativa>(lidas[TipoOcorrencia.Tentativa]);
        Assert.IsType<AtitudeSuspeita>(lidas[TipoOcorrencia.Suspeita]);

        // A consulta por subclasse vira um filtro na coluna discriminadora (WHERE Tipo = 'Roubo').
        Assert.Single(await novoContexto.Ocorrencias.OfType<Roubo>().ToListAsync());
    }

    [Fact]
    public async Task ListarAsync_PorProximidade_DevolveSoOsRelatosDentroDoRaio()
    {
        await using (var contexto = _banco.CriarContexto())
        {
            contexto.Ocorrencias.AddRange(
                Fabricas.Ocorrencia(TipoOcorrencia.Furto, Fabricas.Republica.AoNorte(200)),
                Fabricas.Ocorrencia(TipoOcorrencia.Furto, Fabricas.Republica.AoNorte(2_000)));
            await contexto.SaveChangesAsync();
        }

        await using var leitura = _banco.CriarContexto();
        var encontradas = await CriarServicoDeOcorrencias(leitura).ListarAsync(new FiltroOcorrenciasRequest
        {
            Latitude = Fabricas.Republica.Latitude,
            Longitude = Fabricas.Republica.Longitude,
            RaioMetros = 500,
        });

        Assert.Single(encontradas);
    }

    [Fact]
    public async Task ListarAsync_ComSoLatitude_LancaRegraDeNegocio()
    {
        await using var contexto = _banco.CriarContexto();

        await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            CriarServicoDeOcorrencias(contexto).ListarAsync(new FiltroOcorrenciasRequest { Latitude = -23.5 }));
    }

    [Fact]
    public async Task ObterAsync_Inexistente_LancaRecursoNaoEncontrado()
    {
        await using var contexto = _banco.CriarContexto();

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => CriarServicoDeOcorrencias(contexto).ObterAsync(999));
    }

    [Fact]
    public async Task CriarZona_ComIdRepetido_LancaConflito()
    {
        await using var contexto = _banco.CriarContexto();
        var requisicao = new CriarZonaRequest
        {
            Id = "se",
            Nome = "Outra Sé",
            Bairro = "Sé",
            Tipo = TipoLocal.Praca,
            Latitude = -23.55,
            Longitude = -46.63,
            RiscoPorHora = new double[24],
        };

        await Assert.ThrowsAsync<ConflitoException>(() => CriarServicoDeZonas(contexto).CriarAsync(requisicao));
    }

    [Fact]
    public async Task AtualizarPrevisaoAsync_GravaANovaPrevisaoEADataDeAtualizacao()
    {
        var novaPrevisao = Fabricas.Previsao(0.1, (7, 0.8)).RiscosPorHora.ToArray();
        _banco.Relogio.Agora = RelogioFixo.Padrao.AddDays(1);

        await using (var contexto = _banco.CriarContexto())
        {
            await CriarServicoDeZonas(contexto).AtualizarPrevisaoAsync(
                "se",
                new AtualizarPrevisaoRequest { RiscoPorHora = novaPrevisao, OcorrenciasUltimos30Dias = 50 });
        }

        await using var leitura = _banco.CriarContexto();
        var zona = await leitura.Zonas.SingleAsync(z => z.Id == "se");
        Assert.Equal(0.8, zona.RiscoNaHora(7));
        Assert.Equal(50, zona.OcorrenciasUltimos30Dias);
        Assert.Equal(RelogioFixo.Padrao.AddDays(1), zona.AtualizadoEm);
    }

    [Fact]
    public async Task RankingAsync_OrdenaPeloRiscoDaHora()
    {
        await using var contexto = _banco.CriarContexto();

        var ranking = await CriarServicoDeZonas(contexto).RankingAsync(hora: 18, limite: 5);

        Assert.Equal(new[] { "republica", "se" }, ranking.Select(r => r.ZonaId));
        Assert.Equal(1, ranking[0].Posicao);
    }

    [Fact]
    public async Task Estatisticas_AgrupaPorTipoZonaEPeriodoDoDia()
    {
        var agora = _banco.Relogio.Agora;
        await using (var contexto = _banco.CriarContexto())
        {
            var tarde = Fabricas.Ocorrencia(TipoOcorrencia.Roubo, Fabricas.Republica, ocorridoEm: agora.AddHours(-1)); // 17h SP
            var madrugada = Fabricas.Ocorrencia(TipoOcorrencia.Furto, Fabricas.Republica, ocorridoEm: agora.AddHours(-17)); // 1h SP
            tarde.VincularZona(await contexto.Zonas.FindAsync("republica"));
            contexto.Ocorrencias.AddRange(tarde, madrugada);
            await contexto.SaveChangesAsync();
        }

        await using var leitura = _banco.CriarContexto();
        var servico = new EstatisticaServico(new OcorrenciaRepositorio(leitura), new ZonaRepositorio(leitura), _banco.Relogio);
        var resumo = await servico.ObterResumoAsync(dias: 30);

        Assert.Equal(2, resumo.TotalOcorrencias);
        Assert.Equal(4, resumo.PorTipo.Count);
        Assert.Equal(50, resumo.PorTipo.Single(t => t.Tipo == TipoOcorrencia.Roubo).Percentual);
        Assert.Contains(resumo.PorZona, z => z.ZonaId == "republica" && z.Total == 1);
        Assert.Contains(resumo.PorZona, z => z.ZonaId == null && z.Total == 1);
        Assert.Equal(1, resumo.PorFaixaHoraria.Single(f => f.Faixa.StartsWith("Madrugada")).Total);
        Assert.Equal(1, resumo.PorFaixaHoraria.Single(f => f.Faixa.StartsWith("Tarde")).Total);
    }
}
