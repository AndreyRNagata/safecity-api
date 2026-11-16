using System.Globalization;

namespace SafeCity.Application.Risco;

/// <summary>
/// Fator principal: a previsão do modelo de Machine Learning para a zona que mais
/// influencia o ponto naquela hora, reduzida conforme a distância até ela.
/// </summary>
public sealed class FatorPrevisaoModelo : IFatorRisco
{
    public string Nome => "previsao_modelo";

    public ParcelaRisco Calcular(ContextoAvaliacao contexto)
    {
        var maisInfluente = contexto.Zonas
            .Select(zona => new { Zona = zona, Valor = zona.RiscoPara(contexto.Posicao, contexto.Hora) })
            .OrderByDescending(x => x.Valor)
            .FirstOrDefault();

        if (maisInfluente is null || maisInfluente.Valor <= 0)
        {
            return new ParcelaRisco(Nome, 0, "Nenhum ponto mapeado influencia este local neste horário.");
        }

        var zona = maisInfluente.Zona;
        var previsto = zona.RiscoNaHora(contexto.Hora).ToString("P0", CultureInfo.GetCultureInfo("pt-BR"));
        return new ParcelaRisco(
            Nome,
            maisInfluente.Valor,
            $"O modelo prevê {previsto} de risco em {zona.Nome} às {contexto.Hora}h (ajustado pela distância).");
    }
}
