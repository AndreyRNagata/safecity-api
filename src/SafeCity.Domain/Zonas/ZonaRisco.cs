using System.Text.RegularExpressions;
using SafeCity.Domain.Comum;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.ValueObjects;

namespace SafeCity.Domain.Zonas;

/// <summary>
/// Ponto da cidade com histórico de furtos e roubos de celular (estação, terminal, avenida...).
/// Guarda a previsão de risco por hora gerada pelo modelo de Machine Learning da equipe.
/// </summary>
/// <remarks>
/// Todos os setters são privados: a zona só muda pelos métodos abaixo, que validam os dados.
/// Assim é impossível existir uma zona com raio negativo ou previsão incompleta.
/// </remarks>
public sealed class ZonaRisco : Entidade<string>
{
    public const int TamanhoMaximoId = 60;
    public const int TamanhoMaximoNome = 120;
    public const int TamanhoMaximoBairro = 80;
    public const int TamanhoMaximoDica = 300;
    public const int MaximoDeDicas = 10;
    public const int RaioMinimoMetros = 50;
    public const int RaioMaximoMetros = 5_000;

    /// <summary>A zona influencia o risco até 3 vezes o seu raio.</summary>
    public const int MultiplicadorDeAlcance = 3;

    private static readonly Regex FormatoDoId = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private readonly List<Ocorrencia> _ocorrencias = new();

    /// <summary>Construtor usado pelo Entity Framework Core.</summary>
    private ZonaRisco()
    {
    }

    public ZonaRisco(
        string id,
        string nome,
        string bairro,
        TipoLocal tipo,
        Coordenada centro,
        int raioMetros,
        PrevisaoHoraria previsao,
        int ocorrenciasUltimos30Dias = 0,
        IEnumerable<string>? dicas = null)
    {
        Id = ValidarId(id);
        AtualizarDados(nome, bairro, tipo, centro, raioMetros, dicas);
        AtualizarPrevisao(previsao, ocorrenciasUltimos30Dias);
    }

    public string Nome { get; private set; } = null!;

    public string Bairro { get; private set; } = null!;

    public TipoLocal Tipo { get; private set; }

    public Coordenada Centro { get; private set; } = null!;

    public int RaioMetros { get; private set; }

    /// <summary>Saída do modelo de ML: risco previsto para cada hora do dia.</summary>
    public PrevisaoHoraria Previsao { get; private set; } = null!;

    /// <summary>Total de ocorrências oficiais (SSP-SP) no local nos últimos 30 dias.</summary>
    public int OcorrenciasUltimos30Dias { get; private set; }

    /// <summary>Dicas de prevenção específicas do local.</summary>
    public IReadOnlyList<string> Dicas { get; private set; } = Array.Empty<string>();

    /// <summary>Relatos de cidadãos vinculados a esta zona.</summary>
    public IReadOnlyCollection<Ocorrencia> Ocorrencias => _ocorrencias.AsReadOnly();

    /// <summary>Distância máxima em que a zona ainda influencia o risco.</summary>
    public double AlcanceMetros => RaioMetros * MultiplicadorDeAlcance;

    public void AtualizarDados(
        string nome,
        string bairro,
        TipoLocal tipo,
        Coordenada centro,
        int raioMetros,
        IEnumerable<string>? dicas)
    {
        Guarda.Contra(!Enum.IsDefined(tipo), "Tipo de local inválido.");
        Guarda.Contra(centro is null, "A localização da zona é obrigatória.");
        Guarda.Contra(
            raioMetros is < RaioMinimoMetros or > RaioMaximoMetros,
            $"O raio deve ficar entre {RaioMinimoMetros} e {RaioMaximoMetros} metros.");

        Nome = Guarda.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Bairro = Guarda.TextoObrigatorio(bairro, nameof(Bairro), TamanhoMaximoBairro);
        Tipo = tipo;
        Centro = centro!;
        RaioMetros = raioMetros;
        Dicas = ValidarDicas(dicas);
    }

    /// <summary>Recebe uma nova previsão do modelo de Machine Learning.</summary>
    public void AtualizarPrevisao(PrevisaoHoraria previsao, int? ocorrenciasUltimos30Dias = null)
    {
        Guarda.Contra(previsao is null, "A previsão por hora é obrigatória.");
        Guarda.Contra(ocorrenciasUltimos30Dias < 0, "O total de ocorrências não pode ser negativo.");

        Previsao = previsao!;
        if (ocorrenciasUltimos30Dias is { } total)
        {
            OcorrenciasUltimos30Dias = total;
        }
    }

    public double RiscoNaHora(int hora) => Previsao.RiscoNaHora(hora);

    public double DistanciaEmMetrosAte(Coordenada posicao) => Centro.DistanciaEmMetrosAte(posicao);

    /// <summary>1 dentro do raio; cai em linha reta até 0 quando a distância chega a 3x o raio.</summary>
    public double FatorProximidade(double distanciaMetros)
    {
        if (distanciaMetros <= RaioMetros)
        {
            return 1;
        }

        return Math.Max(0, 1 - (distanciaMetros - RaioMetros) / (AlcanceMetros - RaioMetros));
    }

    /// <summary>Risco que esta zona gera para quem está em <paramref name="posicao"/> na <paramref name="hora"/>.</summary>
    public double RiscoPara(Coordenada posicao, int hora) =>
        RiscoNaHora(hora) * FatorProximidade(DistanciaEmMetrosAte(posicao));

    public bool Influencia(Coordenada posicao) => DistanciaEmMetrosAte(posicao) <= AlcanceMetros;

    private static string ValidarId(string id)
    {
        var valor = Guarda.TextoObrigatorio(id, nameof(Id), TamanhoMaximoId);
        Guarda.Contra(
            !FormatoDoId.IsMatch(valor),
            "O id deve usar só letras minúsculas sem acento, números e hífens (ex.: \"paulista-masp\").");
        return valor;
    }

    private static IReadOnlyList<string> ValidarDicas(IEnumerable<string>? dicas)
    {
        var lista = (dicas ?? Enumerable.Empty<string>())
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .Distinct()
            .ToArray();

        Guarda.Contra(lista.Length > MaximoDeDicas, $"Cada zona pode ter no máximo {MaximoDeDicas} dicas.");
        Guarda.Contra(
            lista.Any(d => d.Length > TamanhoMaximoDica),
            $"Cada dica deve ter no máximo {TamanhoMaximoDica} caracteres.");
        return lista;
    }
}
