namespace SafeCity.Application.Risco;

/// <summary>
/// Um critério que contribui para o risco (padrão Strategy).
/// A <see cref="CalculadoraRisco"/> recebe todas as implementações por injeção de dependência:
/// para criar um critério novo (ex.: iluminação da rua), basta implementar esta interface
/// e registrá-la no contêiner, sem mexer na calculadora.
/// </summary>
public interface IFatorRisco
{
    /// <summary>Identificador do fator, exibido na explicação do risco.</summary>
    string Nome { get; }

    ParcelaRisco Calcular(ContextoAvaliacao contexto);
}

/// <summary>Calcula o risco combinando todos os fatores registrados.</summary>
public interface ICalculadoraRisco
{
    ResultadoAvaliacao Avaliar(ContextoAvaliacao contexto);
}
