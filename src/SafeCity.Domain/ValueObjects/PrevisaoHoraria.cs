using System.Collections.ObjectModel;
using SafeCity.Domain.Comum;
using SafeCity.Domain.Risco;

namespace SafeCity.Domain.ValueObjects;

/// <summary>
/// Previsão do modelo de Machine Learning para uma zona: a probabilidade (0 a 1)
/// de furto ou roubo de celular em cada hora do dia, de 0h a 23h.
/// </summary>
public sealed class PrevisaoHoraria : IEquatable<PrevisaoHoraria>
{
    public const int TotalDeHoras = 24;

    private readonly double[] _riscos;

    public PrevisaoHoraria(IEnumerable<double> riscosPorHora)
    {
        Guarda.Contra(riscosPorHora is null, "A previsão por hora é obrigatória.");
        var riscos = riscosPorHora!.ToArray();

        Guarda.Contra(
            riscos.Length != TotalDeHoras,
            $"A previsão precisa ter exatamente {TotalDeHoras} valores (um por hora, de 0h a 23h). Recebidos: {riscos.Length}.");

        var horaInvalida = Array.FindIndex(riscos, r => !double.IsFinite(r) || r is < 0 or > 1);
        Guarda.Contra(horaInvalida >= 0, $"O risco das {horaInvalida}h deve estar entre 0 e 1.");

        _riscos = riscos.Select(r => Math.Round(r, 4)).ToArray();
        RiscosPorHora = new ReadOnlyCollection<double>(_riscos);
    }

    /// <summary>Os 24 valores, somente leitura.</summary>
    public IReadOnlyList<double> RiscosPorHora { get; }

    public double RiscoMedio => _riscos.Average();

    /// <summary>Converte qualquer inteiro para uma hora válida (ex.: 24 vira 0 e -1 vira 23).</summary>
    public static int NormalizarHora(int hora) => ((hora % TotalDeHoras) + TotalDeHoras) % TotalDeHoras;

    public double RiscoNaHora(int hora) => _riscos[NormalizarHora(hora)];

    /// <summary>Hora com o maior risco previsto.</summary>
    public (int Hora, double Risco) Pico()
    {
        var maior = _riscos.Max();
        return (Array.IndexOf(_riscos, maior), maior);
    }

    /// <summary>
    /// Faixas contínuas de horas com risco igual ou acima do limite.
    /// Uma faixa que atravessa a meia-noite (ex.: 22h–1h) é devolvida como uma só.
    /// </summary>
    public IReadOnlyList<FaixaHoraria> FaixasCriticas(double limite = ClassificadorRisco.LimiteAlto)
    {
        var faixas = new List<FaixaHoraria>();
        for (var hora = 0; hora < TotalDeHoras; hora++)
        {
            if (_riscos[hora] < limite)
            {
                continue;
            }

            if (faixas.Count > 0 && faixas[^1].Fim == hora - 1)
            {
                faixas[^1] = faixas[^1] with { Fim = hora };
            }
            else
            {
                faixas.Add(new FaixaHoraria(hora, hora));
            }
        }

        var atravessaMeiaNoite = faixas.Count > 1 && faixas[0].Inicio == 0 && faixas[^1].Fim == TotalDeHoras - 1;
        if (atravessaMeiaNoite)
        {
            faixas[^1] = faixas[^1] with { Fim = faixas[0].Fim };
            faixas.RemoveAt(0);
        }

        return faixas;
    }

    /// <summary>Texto para o cidadão, ex.: "7h–8h e 17h–19h".</summary>
    public string DescreverHorariosCriticos(double limite = ClassificadorRisco.LimiteAlto)
    {
        var textos = FaixasCriticas(limite).Select(f => f.ToString()).ToList();
        return textos.Count switch
        {
            0 => "nenhum horário crítico previsto",
            1 => textos[0],
            _ => $"{string.Join(", ", textos.Take(textos.Count - 1))} e {textos[^1]}",
        };
    }

    public bool Equals(PrevisaoHoraria? outra) => outra is not null && _riscos.SequenceEqual(outra._riscos);

    public override bool Equals(object? obj) => Equals(obj as PrevisaoHoraria);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var risco in _riscos)
        {
            hash.Add(risco);
        }

        return hash.ToHashCode();
    }
}

/// <summary>Intervalo de horas (inclusive). Ex.: 17h–19h.</summary>
public readonly record struct FaixaHoraria(int Inicio, int Fim)
{
    public override string ToString() => Inicio == Fim ? $"{Inicio}h" : $"{Inicio}h–{Fim}h";
}
