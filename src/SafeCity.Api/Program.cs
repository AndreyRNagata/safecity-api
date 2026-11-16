using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using SafeCity.Api.Configuracao;
using SafeCity.Api.Middlewares;
using SafeCity.Application;
using SafeCity.Application.Serializacao;
using SafeCity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serviços (injeção de dependência) ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(opcoes =>
    {
        // Enums como texto no mesmo formato do app: "estacao", "muito_alto", "furto".
        opcoes.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(SnakeCaseLowerNamingPolicy.Instancia, allowIntegerValues: false));
        // Mantém acentos legíveis nas respostas ("Sé" em vez de "Sé").
        opcoes.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    });

builder.Services.AdicionarAplicacao(builder.Configuration);
builder.Services.AdicionarInfraestrutura(builder.Configuration);
builder.Services.AdicionarDocumentacaoSwagger();
builder.Services.AddHealthChecks();
builder.Services.AddCors(opcoes => opcoes.AddPolicy(
    PoliticasCors.AppMobile,
    politica => politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ---------- Banco: migrações + dados de demonstração ----------
await app.Services.InicializarBancoDeDadosAsync();

// ---------- Pipeline HTTP ----------
app.UseMiddleware<TratamentoDeErrosMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(opcoes =>
{
    opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "SafeCity API v1");
    opcoes.DocumentTitle = "SafeCity API";
    opcoes.EnableDeepLinking();
    opcoes.DisplayRequestDuration();
});
app.UseCors(PoliticasCors.AppMobile);

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

/// <summary>Exposto para os testes de integração (WebApplicationFactory).</summary>
public partial class Program
{
}
