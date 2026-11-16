using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SafeCity.Domain.ValueObjects;

namespace SafeCity.Infrastructure.Dados.Conversores;

/// <summary>DateTimeOffset ⇄ milissegundos desde 1970 (UTC).</summary>
public sealed class DateTimeOffsetParaUnixMsConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetParaUnixMsConverter()
        : base(
            valor => valor.ToUnixTimeMilliseconds(),
            valor => DateTimeOffset.FromUnixTimeMilliseconds(valor))
    {
    }
}

/// <summary>Previsão de 24 horas ⇄ texto "0.09;0.08;...;0.19" (cultura invariável).</summary>
public sealed class PrevisaoHorariaConverter : ValueConverter<PrevisaoHoraria, string>
{
    private const char Separador = ';';

    public PrevisaoHorariaConverter()
        : base(previsao => Serializar(previsao), texto => Desserializar(texto))
    {
    }

    private static string Serializar(PrevisaoHoraria previsao) =>
        string.Join(Separador, previsao.RiscosPorHora.Select(r => r.ToString("R", CultureInfo.InvariantCulture)));

    private static PrevisaoHoraria Desserializar(string texto) =>
        new(texto.Split(Separador).Select(parte => double.Parse(parte, CultureInfo.InvariantCulture)));
}

/// <summary>Lista de textos ⇄ JSON (ex.: as dicas de uma zona).</summary>
public sealed class ListaDeTextosConverter : ValueConverter<IReadOnlyList<string>, string>
{
    private static readonly JsonSerializerOptions Opcoes = new()
    {
        // Mantém os acentos legíveis dentro do banco.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public ListaDeTextosConverter()
        : base(lista => Serializar(lista), json => Desserializar(json))
    {
    }

    private static string Serializar(IReadOnlyList<string> lista) => JsonSerializer.Serialize(lista, Opcoes);

    private static IReadOnlyList<string> Desserializar(string json) =>
        JsonSerializer.Deserialize<string[]>(json, Opcoes) ?? Array.Empty<string>();
}

/// <summary>Ensina o EF Core a perceber quando uma lista de textos mudou (compara item a item).</summary>
public sealed class ListaDeTextosComparer : ValueComparer<IReadOnlyList<string>>
{
    public ListaDeTextosComparer()
        : base(
            (a, b) => ReferenceEquals(a, b) || (a != null && b != null && a.SequenceEqual(b)),
            lista => lista.Aggregate(0, (hash, texto) => HashCode.Combine(hash, texto.GetHashCode())),
            lista => lista.ToArray())
    {
    }
}
