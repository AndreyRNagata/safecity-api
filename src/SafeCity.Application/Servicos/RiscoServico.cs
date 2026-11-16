using Microsoft.Extensions.Options;
using SafeCity.Application.Abstracoes;
using SafeCity.Application.Dtos;
using SafeCity.Application.Mapeamentos;
using SafeCity.Application.Risco;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Repositorios;
using SafeCity.Domain.Risco;
using SafeCity.Domain.ValueObjects;

namespace SafeCity.Application.Servicos;

/// <summary>Responde "qual o risco aqui, agora?" para um ponto do mapa.</summary>
public interface IRiscoServico
{
    Task<AvaliacaoRiscoResponse> AvaliarAsync(
        double latitude,
        double longitude,
        int? hora,
        CancellationToken cancellationToken = default);
}

public sealed class RiscoServico : IRiscoServico
{
    private static readonly IReadOnlyList<string> DicasGerais = new[]
    {
        "Use o celular encostado em uma parede ou dentro de um comércio.",
        "Evite andar mexendo no celular perto da rua, de pontos de ônibus e de saídas de estação.",
        "Cadastre seu aparelho no Celular Seguro antes de precisar.",
    };

    private readonly IZonaRepositorio _zonas;
    private readonly IOcorrenciaRepositorio _ocorrencias;
    private readonly ICalculadoraRisco _calculadora;
    private readonly IRelogio _relogio;
    private readonly OpcoesRisco _opcoes;

    public RiscoServico(
        IZonaRepositorio zonas,
        IOcorrenciaRepositorio ocorrencias,
        ICalculadoraRisco calculadora,
        IRelogio relogio,
        IOptions<OpcoesRisco> opcoes)
    {
        _zonas = zonas;
        _ocorrencias = ocorrencias;
        _calculadora = calculadora;
        _relogio = relogio;
        _opcoes = opcoes.Value;
    }

    public async Task<AvaliacaoRiscoResponse> AvaliarAsync(
        double latitude,
        double longitude,
        int? hora,
        CancellationToken cancellationToken = default)
    {
        var posicao = new Coordenada(latitude, longitude);
        var agora = _relogio.Agora;
        var horaConsulta = PrevisaoHoraria.NormalizarHora(hora ?? _relogio.ParaHorarioLocal(agora).Hour);

        // As duas consultas são aguardadas em sequência de propósito: o DbContext do EF Core
        // não aceita duas operações ao mesmo tempo, então Task.WhenAll aqui causaria erro.
        var zonas = await _zonas.ListarAsync(cancellationToken);
        var recentes = await _ocorrencias.ListarDesdeAsync(agora - _opcoes.JanelaRelatos, cancellationToken);

        var contexto = new ContextoAvaliacao(posicao, horaConsulta, agora, zonas, recentes);
        var resultado = _calculadora.Avaliar(contexto);

        // Para as próximas horas usamos só a previsão do modelo: os relatos são um sinal do "agora".
        var semRelatos = contexto with { Ocorrencias = Array.Empty<Ocorrencia>() };
        var proximasHoras = Enumerable.Range(1, _opcoes.HorasDePrevisao)
            .Select(passo => _calculadora.Avaliar(semRelatos with { Hora = PrevisaoHoraria.NormalizarHora(horaConsulta + passo) }))
            .Select(r => new RiscoHoraResponse(r.Hora, r.Score, r.Nivel))
            .ToList();

        var dicas = resultado.ZonaDeReferencia is { Dicas.Count: > 0 } zona && resultado.Nivel >= NivelRisco.Moderado
            ? zona.Dicas
            : DicasGerais;

        return new AvaliacaoRiscoResponse(
            resultado.Score,
            (int)Math.Round(resultado.Score * 100),
            resultado.Nivel,
            resultado.Hora,
            resultado.ZonaDeReferencia?.ParaResumo(),
            resultado.DistanciaMetros is { } distancia ? (int)Math.Round(distancia) : null,
            resultado.RelatosProximos,
            resultado.DentroDaArea,
            resultado.Parcelas.Select(p => p.ParaResposta()).ToList(),
            dicas,
            proximasHoras);
    }
}
