using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SafeCity.Application.Risco;
using SafeCity.Application.Servicos;

namespace SafeCity.Application;

/// <summary>Registra no contêiner de injeção de dependência tudo o que a camada de aplicação oferece.</summary>
public static class InjecaoDeDependencia
{
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OpcoesRisco>()
            .Bind(configuration.GetSection(OpcoesRisco.Secao))
            .ValidateDataAnnotations();

        // Strategy: cada fator é uma implementação de IFatorRisco.
        // A calculadora recebe IEnumerable<IFatorRisco> com todos os registrados abaixo.
        services.AddSingleton<IFatorRisco, FatorPrevisaoModelo>();
        services.AddSingleton<IFatorRisco, FatorRelatosRecentes>();
        services.AddSingleton<ICalculadoraRisco, CalculadoraRisco>();

        // Serviços com acesso ao banco: um por requisição HTTP (Scoped), igual ao DbContext.
        services.AddScoped<IZonaServico, ZonaServico>();
        services.AddScoped<IOcorrenciaServico, OcorrenciaServico>();
        services.AddScoped<IRiscoServico, RiscoServico>();
        services.AddScoped<IEstatisticaServico, EstatisticaServico>();

        return services;
    }
}
