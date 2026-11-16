using Microsoft.OpenApi.Models;

namespace SafeCity.Api.Configuracao;

public static class PoliticasCors
{
    /// <summary>Libera o app (Expo no celular ou na web) para chamar a API.</summary>
    public const string AppMobile = "app-mobile";
}

public static class SwaggerConfiguracao
{
    public static IServiceCollection AdicionarDocumentacaoSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opcoes =>
        {
            opcoes.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SafeCity API",
                Version = "v1",
                Description =
                    "Backend da CitizenTech **SafeCity — Alerta Preventivo & Resposta Rápida** (GS FIAP 2026.2).\n\n" +
                    "Entrega ao app as zonas de risco de furto de celular com a previsão por hora do modelo de " +
                    "Machine Learning, recebe relatos anônimos de cidadãos e calcula o risco de um ponto em tempo real.\n\n" +
                    "Todos os dados de demonstração são **simulados**. Nenhum dado pessoal é armazenado.",
            });

            // Comentários /// dos controllers e dos DTOs aparecem na documentação.
            foreach (var arquivoXml in Directory.GetFiles(AppContext.BaseDirectory, "SafeCity.*.xml"))
            {
                opcoes.IncludeXmlComments(arquivoXml, includeControllerXmlComments: true);
            }

            opcoes.SupportNonNullableReferenceTypes();
        });

        return services;
    }
}
