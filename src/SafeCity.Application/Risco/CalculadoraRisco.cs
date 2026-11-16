using Microsoft.Extensions.Options;
using SafeCity.Domain.Risco;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Risco;

/// <summary>
/// Soma as parcelas de todos os <see cref="IFatorRisco"/> registrados e limita o resultado a 1.
/// <code>risco = previsão do modelo × proximidade + bônus por relatos recentes</code>
/// </summary>
public sealed class CalculadoraRisco : ICalculadoraRisco
{
    private readonly IReadOnlyList<IFatorRisco> _fatores;
    private readonly OpcoesRisco _opcoes;

    public CalculadoraRisco(IEnumerable<IFatorRisco> fatores, IOptions<OpcoesRisco> opcoes)
    {
        _fatores = fatores.ToList();
        _opcoes = opcoes.Value;
    }

    public ResultadoAvaliacao Avaliar(ContextoAvaliacao contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var parcelas = _fatores.Select(fator => fator.Calcular(contexto)).ToList();
        var score = Math.Round(Math.Clamp(parcelas.Sum(p => p.Valor), 0, 1), 4);

        var referencia = ZonaDeReferencia(contexto);
        var maisProxima = contexto.Zonas.MinBy(z => z.DistanciaEmMetrosAte(contexto.Posicao));
        var dentroDaArea = maisProxima is not null
            && maisProxima.DistanciaEmMetrosAte(contexto.Posicao) <= _opcoes.DistanciaCoberturaMetros;

        var relatosProximos = contexto.Ocorrencias
            .RecentesPerto(contexto.Posicao, _opcoes.RaioRelatosMetros, contexto.Agora, _opcoes.JanelaRelatos)
            .Count();

        return new ResultadoAvaliacao(
            score,
            ClassificadorRisco.Classificar(score),
            contexto.Hora,
            referencia,
            referencia?.DistanciaEmMetrosAte(contexto.Posicao),
            relatosProximos,
            dentroDaArea,
            parcelas);
    }

    /// <summary>A zona que mais contribui para o risco; se nenhuma contribui, a mais próxima.</summary>
    private static ZonaRisco? ZonaDeReferencia(ContextoAvaliacao contexto)
    {
        var maisInfluente = contexto.Zonas
            .Where(z => z.RiscoPara(contexto.Posicao, contexto.Hora) > 0)
            .MaxBy(z => z.RiscoPara(contexto.Posicao, contexto.Hora));

        return maisInfluente ?? contexto.Zonas.MinBy(z => z.DistanciaEmMetrosAte(contexto.Posicao));
    }
}
