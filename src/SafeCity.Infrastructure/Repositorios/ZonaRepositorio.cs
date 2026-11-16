using Microsoft.EntityFrameworkCore;
using SafeCity.Domain.Repositorios;
using SafeCity.Domain.Zonas;
using SafeCity.Infrastructure.Dados;

namespace SafeCity.Infrastructure.Repositorios;

public sealed class ZonaRepositorio : RepositorioBase<ZonaRisco, string>, IZonaRepositorio
{
    public ZonaRepositorio(SafeCityDbContext contexto) : base(contexto)
    {
    }

    public override async Task<IReadOnlyList<ZonaRisco>> ListarAsync(CancellationToken cancellationToken = default) =>
        await Conjunto.AsNoTracking().OrderBy(z => z.Nome).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ZonaRisco>> BuscarAsync(
        string? bairro,
        TipoLocal? tipo,
        CancellationToken cancellationToken = default)
    {
        // A consulta é montada aos poucos (IQueryable) e só vai ao banco no ToListAsync.
        IQueryable<ZonaRisco> consulta = Conjunto.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(bairro))
        {
            var termo = $"%{bairro.Trim()}%";
            consulta = consulta.Where(z => EF.Functions.Like(z.Bairro, termo));
        }

        if (tipo is { } tipoLocal)
        {
            consulta = consulta.Where(z => z.Tipo == tipoLocal);
        }

        return await consulta.OrderBy(z => z.Nome).ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteAsync(string id, CancellationToken cancellationToken = default) =>
        Conjunto.AnyAsync(z => z.Id == id, cancellationToken);
}
