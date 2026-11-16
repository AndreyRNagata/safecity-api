using Microsoft.AspNetCore.Mvc;
using SafeCity.Domain.Comum;

namespace SafeCity.Api.Middlewares;

/// <summary>
/// Captura as exceções e devolve respostas padronizadas (ProblemDetails, RFC 7807).
/// Os controllers ficam limpos: não precisam de try/catch.
/// </summary>
public sealed class TratamentoDeErrosMiddleware
{
    private readonly RequestDelegate _proximo;
    private readonly ILogger<TratamentoDeErrosMiddleware> _logger;

    public TratamentoDeErrosMiddleware(RequestDelegate proximo, ILogger<TratamentoDeErrosMiddleware> logger)
    {
        _proximo = proximo;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _proximo(contexto);
        }
        catch (SafeCityException erro) when (!contexto.Response.HasStarted)
        {
            var (status, titulo) = erro switch
            {
                RecursoNaoEncontradoException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
                ConflitoException => (StatusCodes.Status409Conflict, "Conflito"),
                _ => (StatusCodes.Status400BadRequest, "Requisição inválida"),
            };

            await EscreverProblemaAsync(contexto, status, titulo, erro.Message);
        }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            // O cliente desistiu da requisição (ex.: o app atingiu o tempo limite). Nada a responder.
        }
        catch (Exception erro) when (!contexto.Response.HasStarted)
        {
            _logger.LogError(erro, "Erro inesperado em {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);
            await EscreverProblemaAsync(
                contexto,
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                "Ocorreu um erro inesperado. Tente novamente em instantes.");
        }
    }

    private static Task EscreverProblemaAsync(HttpContext contexto, int status, string titulo, string detalhe)
    {
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalhe,
            Instance = contexto.Request.Path,
        };
        problema.Extensions["traceId"] = contexto.TraceIdentifier;

        contexto.Response.StatusCode = status;
        return contexto.Response.WriteAsJsonAsync(problema, options: null, contentType: "application/problem+json");
    }
}
