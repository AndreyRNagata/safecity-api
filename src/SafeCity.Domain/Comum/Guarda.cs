namespace SafeCity.Domain.Comum;

/// <summary>Validações reutilizadas pelas entidades para manter seus dados sempre consistentes.</summary>
public static class Guarda
{
    /// <summary>Lança <see cref="RegraDeNegocioException"/> quando a condição é verdadeira.</summary>
    public static void Contra(bool condicao, string mensagem)
    {
        if (condicao)
        {
            throw new RegraDeNegocioException(mensagem);
        }
    }

    /// <summary>Exige um texto preenchido e com até <paramref name="tamanhoMaximo"/> caracteres.</summary>
    /// <returns>O texto sem espaços nas pontas.</returns>
    public static string TextoObrigatorio(string? valor, string campo, int tamanhoMaximo)
    {
        Contra(string.IsNullOrWhiteSpace(valor), $"O campo '{campo}' é obrigatório.");
        var texto = valor!.Trim();
        Contra(texto.Length > tamanhoMaximo, $"O campo '{campo}' deve ter no máximo {tamanhoMaximo} caracteres.");
        return texto;
    }
}
