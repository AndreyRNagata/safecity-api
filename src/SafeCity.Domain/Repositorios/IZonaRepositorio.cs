using SafeCity.Domain.Zonas;

namespace SafeCity.Domain.Repositorios;

public interface IZonaRepositorio : IRepositorio<ZonaRisco, string>
{
    /// <summary>Lista as zonas, com filtros opcionais por bairro e tipo de local.</summary>
    Task<IReadOnlyList<ZonaRisco>> BuscarAsync(
        string? bairro,
        TipoLocal? tipo,
        CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(string id, CancellationToken cancellationToken = default);
}
