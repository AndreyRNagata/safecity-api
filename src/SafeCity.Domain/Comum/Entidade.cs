namespace SafeCity.Domain.Comum;

/// <summary>
/// Base de todas as entidades do domínio. Guarda os dados de auditoria
/// (quando foi criada e quando foi alterada), preenchidos pela camada de dados ao salvar.
/// </summary>
public abstract class Entidade
{
    public DateTimeOffset CriadoEm { get; private set; }

    public DateTimeOffset? AtualizadoEm { get; private set; }

    public void MarcarCriacao(DateTimeOffset quando) => CriadoEm = quando;

    public void MarcarAtualizacao(DateTimeOffset quando) => AtualizadoEm = quando;
}

/// <summary>Entidade identificada por uma chave do tipo <typeparamref name="TId"/>.</summary>
public abstract class Entidade<TId> : Entidade where TId : notnull
{
    public TId Id { get; protected set; } = default!;
}
