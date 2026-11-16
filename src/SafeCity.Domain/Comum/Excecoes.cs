namespace SafeCity.Domain.Comum;

/// <summary>
/// Base das exceções conhecidas do SafeCity. A API transforma cada tipo
/// em uma resposta HTTP adequada (400, 404 ou 409).
/// </summary>
public abstract class SafeCityException : Exception
{
    protected SafeCityException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>Uma regra de negócio foi violada (ex.: previsão sem 24 horas, data no futuro).</summary>
public sealed class RegraDeNegocioException : SafeCityException
{
    public RegraDeNegocioException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>O recurso pedido não existe.</summary>
public sealed class RecursoNaoEncontradoException : SafeCityException
{
    public RecursoNaoEncontradoException(string recurso, object id)
        : base($"{recurso} '{id}' não foi encontrado(a).")
    {
    }
}

/// <summary>O recurso já existe ou está em um estado que impede a operação.</summary>
public sealed class ConflitoException : SafeCityException
{
    public ConflitoException(string mensagem) : base(mensagem)
    {
    }
}
