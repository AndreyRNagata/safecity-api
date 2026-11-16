using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SafeCity.Application.Abstracoes;
using SafeCity.Domain.Repositorios;
using SafeCity.Infrastructure.Configuracao;
using SafeCity.Infrastructure.Dados;
using SafeCity.Infrastructure.Dados.Seed;
using SafeCity.Infrastructure.Repositorios;

namespace SafeCity.Infrastructure;

/// <summary>Registra o banco de dados, os repositórios e o relógio no contêiner de DI.</summary>
public static class InjecaoDeDependencia
{
    public const string NomeDaConexao = "SafeCity";

    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OpcoesSafeCity>().Bind(configuration.GetSection(OpcoesSafeCity.Secao));
        services.AddSingleton<IRelogio, RelogioSistema>();

        var conexao = configuration.GetConnectionString(NomeDaConexao) ?? "Data Source=safecity.db";
        services.AddDbContext<SafeCityDbContext>(opcoes => opcoes.UseSqlite(conexao));
        services.AddScoped<IUnidadeDeTrabalho>(provider => provider.GetRequiredService<SafeCityDbContext>());

        services.AddScoped<IZonaRepositorio, ZonaRepositorio>();
        services.AddScoped<IOcorrenciaRepositorio, OcorrenciaRepositorio>();
        services.AddScoped<SemeadorDeDados>();

        return services;
    }

    /// <summary>Aplica as migrações pendentes e, se configurado, carrega os dados de demonstração.</summary>
    public static async Task InicializarBancoDeDadosAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        await using var escopo = provider.CreateAsyncScope();
        var servicos = escopo.ServiceProvider;

        await servicos.GetRequiredService<SafeCityDbContext>().Database.MigrateAsync(cancellationToken);

        if (servicos.GetRequiredService<IOptions<OpcoesSafeCity>>().Value.PopularDadosIniciais)
        {
            await servicos.GetRequiredService<SemeadorDeDados>().PopularAsync(cancellationToken);
        }
    }
}
