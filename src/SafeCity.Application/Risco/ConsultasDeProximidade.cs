using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.ValueObjects;

namespace SafeCity.Application.Risco;

/// <summary>Métodos de extensão LINQ para filtrar ocorrências por tempo e distância.</summary>
public static class ConsultasDeProximidade
{
    /// <summary>Ocorrências recentes a até <paramref name="raioMetros"/> do ponto.</summary>
    public static IEnumerable<Ocorrencia> RecentesPerto(
        this IEnumerable<Ocorrencia> ocorrencias,
        Coordenada ponto,
        double raioMetros,
        DateTimeOffset agora,
        TimeSpan janela) =>
        ocorrencias.Where(o => o.EhRecente(agora, janela) && o.Local.DistanciaEmMetrosAte(ponto) <= raioMetros);
}
