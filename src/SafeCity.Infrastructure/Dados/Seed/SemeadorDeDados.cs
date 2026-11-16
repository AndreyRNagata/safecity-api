using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SafeCity.Application.Abstracoes;
using SafeCity.Application.Serializacao;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.ValueObjects;
using SafeCity.Domain.Zonas;

namespace SafeCity.Infrastructure.Dados.Seed;

/// <summary>
/// Carrega as zonas e os relatos de demonstração quando o banco está vazio.
/// São os mesmos dados SIMULADOS do mock do app mobile (mock-api/db.json), para que
/// app e API mostrem a mesma coisa. Nenhum dado é real ou pessoal.
/// </summary>
public sealed class SemeadorDeDados
{
    private const string NomeDoRecurso = "SafeCity.DadosIniciais.json";

    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(SnakeCaseLowerNamingPolicy.Instancia) },
    };

    private readonly SafeCityDbContext _contexto;
    private readonly IRelogio _relogio;
    private readonly ILogger<SemeadorDeDados> _logger;

    public SemeadorDeDados(SafeCityDbContext contexto, IRelogio relogio, ILogger<SemeadorDeDados> logger)
    {
        _contexto = contexto;
        _relogio = relogio;
        _logger = logger;
    }

    public async Task PopularAsync(CancellationToken cancellationToken = default)
    {
        if (await _contexto.Zonas.AnyAsync(cancellationToken))
        {
            return;
        }

        var dados = await LerArquivoAsync(cancellationToken);
        var agora = _relogio.Agora;

        var zonas = dados.Zonas
            .Select(z => new ZonaRisco(
                z.Id,
                z.Nome,
                z.Bairro,
                z.Tipo,
                new Coordenada(z.Latitude, z.Longitude),
                z.RaioMetros,
                new PrevisaoHoraria(z.RiscoPorHora),
                z.OcorrenciasUltimos30Dias,
                z.Dicas))
            .ToList();

        // Os relatos simulados "envelhecem": deslocamos as datas para que o mais recente
        // tenha acontecido 1 hora atrás, mantendo a demonstração coerente em qualquer dia.
        var maisRecente = dados.Ocorrencias.Max(o => o.OcorridoEm);
        var deslocamento = agora.AddHours(-1) - maisRecente;

        var ocorrencias = dados.Ocorrencias
            .Select(o =>
            {
                var ocorrencia = OcorrenciaFactory.Criar(
                    o.Tipo,
                    new Coordenada(o.Latitude, o.Longitude),
                    o.OcorridoEm + deslocamento,
                    agora);
                ocorrencia.VincularZona(LocalizadorDeZonas.ZonaQueAbrange(zonas, ocorrencia.Local));
                return ocorrencia;
            })
            .ToList();

        await _contexto.Zonas.AddRangeAsync(zonas, cancellationToken);
        await _contexto.Ocorrencias.AddRangeAsync(ocorrencias, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Banco populado com {Zonas} zonas e {Ocorrencias} relatos simulados.",
            zonas.Count,
            ocorrencias.Count);
    }

    private static async Task<ArquivoSeed> LerArquivoAsync(CancellationToken cancellationToken)
    {
        await using var arquivo = typeof(SemeadorDeDados).Assembly.GetManifestResourceStream(NomeDoRecurso)
            ?? throw new InvalidOperationException($"Recurso embutido '{NomeDoRecurso}' não encontrado.");

        return await JsonSerializer.DeserializeAsync<ArquivoSeed>(arquivo, OpcoesJson, cancellationToken)
            ?? throw new InvalidOperationException("Arquivo de dados iniciais vazio.");
    }

    private sealed record ArquivoSeed(IReadOnlyList<ZonaSeed> Zonas, IReadOnlyList<OcorrenciaSeed> Ocorrencias);

    private sealed record ZonaSeed(
        string Id,
        string Nome,
        string Bairro,
        TipoLocal Tipo,
        double Latitude,
        double Longitude,
        int RaioMetros,
        double[] RiscoPorHora,
        int OcorrenciasUltimos30Dias,
        string[] Dicas);

    private sealed record OcorrenciaSeed(TipoOcorrencia Tipo, double Latitude, double Longitude, DateTimeOffset OcorridoEm);
}
