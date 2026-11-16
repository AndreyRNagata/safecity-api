using SafeCity.Application.Dtos;
using SafeCity.Application.Risco;
using SafeCity.Domain.Ocorrencias;
using SafeCity.Domain.Risco;
using SafeCity.Domain.Zonas;

namespace SafeCity.Application.Mapeamentos;

/// <summary>Conversões de entidades do domínio para os DTOs expostos pela API.</summary>
public static class MapeamentoExtensions
{
    public static ZonaRiscoResponse ParaResposta(this ZonaRisco zona) => new(
        zona.Id,
        zona.Nome,
        zona.Bairro,
        zona.Tipo,
        zona.Centro.Latitude,
        zona.Centro.Longitude,
        zona.RaioMetros,
        zona.Previsao.RiscosPorHora,
        zona.OcorrenciasUltimos30Dias,
        zona.Dicas,
        zona.AtualizadoEm ?? zona.CriadoEm);

    public static ZonaResumoResponse ParaResumo(this ZonaRisco zona) => new(zona.Id, zona.Nome, zona.Bairro, zona.Tipo);

    public static PrevisaoZonaResponse ParaPrevisao(this ZonaRisco zona)
    {
        var horas = zona.Previsao.RiscosPorHora
            .Select((risco, hora) => new RiscoHoraResponse(hora, risco, ClassificadorRisco.Classificar(risco)))
            .ToList();
        var (horaPico, riscoPico) = zona.Previsao.Pico();

        return new PrevisaoZonaResponse(
            zona.Id,
            zona.Nome,
            horas,
            new RiscoHoraResponse(horaPico, riscoPico, ClassificadorRisco.Classificar(riscoPico)),
            zona.Previsao.DescreverHorariosCriticos(),
            Math.Round(zona.Previsao.RiscoMedio, 4));
    }

    public static OcorrenciaResponse ParaResposta(this Ocorrencia ocorrencia) => new(
        ocorrencia.Id,
        ocorrencia.Tipo,
        ocorrencia.Local.Latitude,
        ocorrencia.Local.Longitude,
        ocorrencia.OcorridoEm,
        ocorrencia.ZonaId,
        ocorrencia.Descricao,
        ocorrencia.EnvolveViolencia,
        ocorrencia.PesoNoRisco,
        ocorrencia.CriadoEm);

    public static ParcelaRiscoResponse ParaResposta(this ParcelaRisco parcela) =>
        new(parcela.Fator, Math.Round(parcela.Valor, 4), parcela.Explicacao);
}
