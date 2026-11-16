using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SafeCity.Infrastructure.Dados;

namespace SafeCity.Tests.Suporte;

/// <summary>
/// Banco SQLite em memória com as migrações aplicadas. Usa o mesmo provedor da API,
/// então as consultas LINQ são traduzidas para SQL de verdade.
/// </summary>
public sealed class BancoEmMemoria : IDisposable
{
    private readonly SqliteConnection _conexao;

    public BancoEmMemoria()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();

        using var contexto = CriarContexto();
        contexto.Database.Migrate();
    }

    public RelogioFixo Relogio { get; } = new();

    /// <summary>Cada chamada devolve um contexto novo, como acontece em cada requisição HTTP.</summary>
    public SafeCityDbContext CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<SafeCityDbContext>().UseSqlite(_conexao).Options;
        return new SafeCityDbContext(opcoes, Relogio);
    }

    public void Dispose() => _conexao.Dispose();
}
