using System.Text;
using System.Text.Json;

namespace SafeCity.Application.Serializacao;

/// <summary>
/// Converte nomes de enums para o formato usado pelo app mobile:
/// <c>MuitoAlto</c> vira <c>muito_alto</c> e <c>Estacao</c> vira <c>estacao</c>.
/// </summary>
/// <remarks>O .NET 7 ainda não traz essa política pronta (ela chegou no .NET 8).</remarks>
public sealed class SnakeCaseLowerNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseLowerNamingPolicy Instancia { get; } = new();

    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var resultado = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var caractere = name[i];
            if (char.IsUpper(caractere))
            {
                if (i > 0)
                {
                    resultado.Append('_');
                }

                resultado.Append(char.ToLowerInvariant(caractere));
            }
            else
            {
                resultado.Append(caractere);
            }
        }

        return resultado.ToString();
    }
}
