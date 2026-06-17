using InfoTrack.Application.SearchRuns;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InfoTrack.Api.ErrorHandling;

public sealed class ApiExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(exception, "Unhandled API exception after the response started.");
                throw;
            }

            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var problem = exception switch
        {
            SearchValidationException validationException => new ProblemDetails
            {
                Title = "Invalid search request",
                Detail = validationException.Message,
                Status = StatusCodes.Status400BadRequest
            },
            _ => CreateUnexpectedProblem(exception)
        };

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problem,
            options: null,
            cancellationToken: context.RequestAborted);
    }

    private ProblemDetails CreateUnexpectedProblem(Exception exception)
    {
        logger.LogError(exception, "Unhandled API exception.");

        return new ProblemDetails
        {
            Title = "Unexpected error",
            Detail = "An unexpected error occurred while processing the request.",
            Status = StatusCodes.Status500InternalServerError
        };
    }
}
