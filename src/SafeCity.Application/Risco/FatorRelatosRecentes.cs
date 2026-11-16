using Microsoft.Extensions.Options;

namespace SafeCity.Application.Risco;

/// <summary>
/// Sinal em tempo real que o modelo ainda não viu: relatos recentes de cidadãos por perto.
/// Cada relato soma <c>BonusPorRelato × PesoNoRisco</c>, então um roubo pesa mais que uma
/// atitude suspeita (o peso vem de cada subclasse de Ocorrencia, por polimorfismo).
/// </summary>
public sealed class FatorRelatosRecentes : IFatorRisco
{
    private readonly OpcoesRisco _opcoes;

    public FatorRelatosRecentes(IOptions<OpcoesRisco> opcoes)
    {
        _opcoes = opcoes.Value;
    }

    public string Nome => "relatos_recentes";

    public ParcelaRisco Calcular(ContextoAvaliacao contexto)
    {
        var proximos = contexto.Ocorrencias
            .RecentesPerto(contexto.Posicao, _opcoes.RaioRelatosMetros, contexto.Agora, _opcoes.JanelaRelatos)
            .ToList();

        if (proximos.Count == 0)
        {
            return new ParcelaRisco(
                Nome,
                0,
                $"Nenhum relato de cidadãos a até {_opcoes.RaioRelatosMetros:0} m nos últimos {_opcoes.JanelaRelatosDias} dias.");
        }

        var bonus = Math.Min(_opcoes.BonusMaximo, proximos.Sum(o => o.PesoNoRisco * _opcoes.BonusPorRelato));

        var resumoPorTipo = proximos
            .GroupBy(o => o.Tipo)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Count()} {g.Key.ToString().ToLowerInvariant()}")
            .ToList();

        return new ParcelaRisco(
            Nome,
            bonus,
            $"{proximos.Count} relato(s) a até {_opcoes.RaioRelatosMetros:0} m nos últimos {_opcoes.JanelaRelatosDias} dias ({string.Join(", ", resumoPorTipo)}).");
    }
}
