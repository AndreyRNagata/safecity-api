using SafeCity.Domain.Comum;
using SafeCity.Domain.ValueObjects;

namespace SafeCity.Domain.Ocorrencias;

/// <summary>
/// Fábrica (padrão Factory): cria a subclasse certa de <see cref="Ocorrencia"/> a partir do tipo
/// informado pelo app. Quem usa não precisa conhecer as classes concretas.
/// </summary>
public static class OcorrenciaFactory
{
    public static Ocorrencia Criar(TipoOcorrencia tipo, Coordenada local, DateTimeOffset ocorridoEm, DateTimeOffset agora) =>
        tipo switch
        {
            TipoOcorrencia.Furto => new Furto(local, ocorridoEm, agora),
            TipoOcorrencia.Roubo => new Roubo(local, ocorridoEm, agora),
            TipoOcorrencia.Tentativa => new Tentativa(local, ocorridoEm, agora),
            TipoOcorrencia.Suspeita => new AtitudeSuspeita(local, ocorridoEm, agora),
            _ => throw new RegraDeNegocioException($"Tipo de ocorrência inválido: {tipo}."),
        };
}
