using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SafeCity.Application.Abstracoes;
using SafeCity.Infrastructure.Dados;

namespace SafeCity.Tests.Suporte;

/// <summary>
/// Sobe a API inteira em memória para os testes de integração, trocando
/// o banco por um SQLite em memória e o relógio por um relógio fixo.
/// </summary>
public sealed class SafeCityApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conexao = new("Data Source=:memory:");

    public RelogioFixo Relogio { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _conexao.Open();
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SafeCityDbContext>>();
            services.AddDbContext<SafeCityDbContext>(opcoes => opcoes.UseSqlite(_conexao));

            services.RemoveAll<IRelogio>();
            services.AddSingleton<IRelogio>(Relogio);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _conexao.Dispose();
        }
    }
}
