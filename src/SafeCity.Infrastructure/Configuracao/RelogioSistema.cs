using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafeCity.Application.Abstracoes;

namespace SafeCity.Infrastructure.Configuracao;

/// <summary>Relógio real do servidor, convertendo para o fuso horário da cidade.</summary>
public sealed class RelogioSistema : IRelogio
{
    private readonly TimeZoneInfo _fuso;

    public RelogioSistema(IOptions<OpcoesSafeCity> opcoes, ILogger<RelogioSistema> logger)
    {
        _fuso = ResolverFuso(opcoes.Value.FusoHorario, logger);
    }

    public DateTimeOffset Agora => DateTimeOffset.UtcNow;

    public DateTimeOffset ParaHorarioLocal(DateTimeOffset instante) => TimeZoneInfo.ConvertTime(instante, _fuso);

    private static TimeZoneInfo ResolverFuso(string id, ILogger logger)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception erro) when (erro is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Sem a base de fusos (ex.: contêiner mínimo), usamos o horário de Brasília fixo (sem horário de verão desde 2019).
            logger.LogWarning("Fuso horário '{Fuso}' não encontrado. Usando UTC-3.", id);
            return TimeZoneInfo.CreateCustomTimeZone("UTC-03", TimeSpan.FromHours(-3), "Horário de Brasília", "Horário de Brasília");
        }
    }
}
