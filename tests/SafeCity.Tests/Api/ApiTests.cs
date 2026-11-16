using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SafeCity.Tests.Suporte;

namespace SafeCity.Tests.Api;

/// <summary>
/// Testes de integração: sobem a API inteira (controllers, DI, EF Core, middleware) e
/// conferem o contrato JSON que o app mobile espera.
/// </summary>
public sealed class ApiTests : IClassFixture<SafeCityApiFactory>
{
    private static readonly string[] TiposDeLocalDoApp = { "estacao", "terminal", "avenida", "rua", "praca" };

    private readonly HttpClient _cliente;

    public ApiTests(SafeCityApiFactory factory)
    {
        _cliente = factory.CreateClient();
    }

    [Fact]
    public async Task GetZonas_SegueOContratoDoAppMobile()
    {
        var resposta = await _cliente.GetAsync("/api/zonas");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var zonas = json.RootElement.EnumerateArray().ToList();

        Assert.True(zonas.Count >= 14);
        Assert.All(zonas, zona =>
        {
            Assert.Equal(JsonValueKind.String, zona.GetProperty("id").ValueKind);
            Assert.Contains(zona.GetProperty("tipo").GetString(), TiposDeLocalDoApp);
            Assert.Equal(24, zona.GetProperty("riscoPorHora").GetArrayLength());
            Assert.True(zona.GetProperty("raioMetros").GetInt32() > 0);
            Assert.Equal(JsonValueKind.Array, zona.GetProperty("dicas").ValueKind);
        });
    }

    [Fact]
    public async Task PostOcorrencia_ComOCorpoEnviadoPeloApp_Devolve201ComLocation()
    {
        var corpoDoApp = new { tipo = "furto", latitude = -23.5442, longitude = -46.6424, ocorridoEm = "2026-11-13T20:30:00.000Z" };

        var resposta = await _cliente.PostAsJsonAsync("/api/ocorrencias", corpoDoApp);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("furto", json.RootElement.GetProperty("tipo").GetString());
        Assert.Equal("republica", json.RootElement.GetProperty("zonaId").GetString());

        var consulta = await _cliente.GetAsync(resposta.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
    }

    [Fact]
    public async Task PostOcorrencia_ComTipoInvalido_Devolve400()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/ocorrencias", new { tipo = "assalto", latitude = -23.5, longitude = -46.6 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostOcorrencia_NoFuturo_Devolve400ComProblemDetails()
    {
        var resposta = await _cliente.PostAsJsonAsync(
            "/api/ocorrencias",
            new { tipo = "roubo", latitude = -23.5, longitude = -46.6, ocorridoEm = "2030-01-01T00:00:00Z" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetZona_Inexistente_Devolve404()
    {
        var resposta = await _cliente.GetAsync("/api/zonas/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetRisco_NaRepublicaAs18h_DevolveMuitoAltoComExplicacao()
    {
        var resposta = await _cliente.GetAsync("/api/risco?latitude=-23.5442&longitude=-46.6424&hora=18");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var raiz = json.RootElement;
        Assert.Equal("muito_alto", raiz.GetProperty("nivel").GetString());
        Assert.Equal("republica", raiz.GetProperty("zona").GetProperty("id").GetString());
        Assert.Equal(2, raiz.GetProperty("fatores").GetArrayLength());
        Assert.Equal(6, raiz.GetProperty("proximasHoras").GetArrayLength());
    }

    [Fact]
    public async Task GetRisco_SemLongitude_Devolve400()
    {
        var resposta = await _cliente.GetAsync("/api/risco?latitude=-23.5442");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CicloDeVidaDaZona_CriaAtualizaPrevisaoERemove()
    {
        var previsao = Enumerable.Repeat(0.2, 24).ToArray();
        var nova = new
        {
            id = "teste-ciclo",
            nome = "Ponto de teste",
            bairro = "Centro",
            tipo = "rua",
            latitude = -23.55,
            longitude = -46.64,
            raioMetros = 200,
            riscoPorHora = previsao,
        };

        var criada = await _cliente.PostAsJsonAsync("/api/zonas", nova);
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);

        previsao[19] = 0.95;
        var atualizada = await _cliente.PutAsJsonAsync("/api/zonas/teste-ciclo/previsao", new { riscoPorHora = previsao });
        Assert.Equal(HttpStatusCode.OK, atualizada.StatusCode);

        var detalhe = await _cliente.GetFromJsonAsync<JsonElement>("/api/zonas/teste-ciclo/previsao");
        Assert.Equal(19, detalhe.GetProperty("horarioDePico").GetProperty("hora").GetInt32());
        Assert.Equal("19h", detalhe.GetProperty("horariosCriticos").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync("/api/zonas/teste-ciclo")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _cliente.GetAsync("/api/zonas/teste-ciclo")).StatusCode);
    }

    [Fact]
    public async Task GetEstatisticas_Devolve200ComTotais()
    {
        var resumo = await _cliente.GetFromJsonAsync<JsonElement>("/api/estatisticas?dias=30");

        Assert.True(resumo.GetProperty("totalOcorrencias").GetInt32() >= 18);
        Assert.Equal(4, resumo.GetProperty("porFaixaHoraria").GetArrayLength());
        Assert.Equal(3, resumo.GetProperty("zonasMaisCriticasAgora").GetArrayLength());
    }

    [Fact]
    public async Task Swagger_PublicaADocumentacao()
    {
        var resposta = await _cliente.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("SafeCity API", await resposta.Content.ReadAsStringAsync());
    }
}
