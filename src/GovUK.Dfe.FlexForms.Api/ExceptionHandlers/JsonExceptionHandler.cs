using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GovUK.Dfe.CoreLibs.Http.Interfaces;
using GovUK.Dfe.CoreLibs.Http.Models;
using Microsoft.AspNetCore.Http;

namespace GovUK.Dfe.FlexForms.Api.ExceptionHandlers;

/// <summary>
/// Handles invalid JSON request bodies, including unrecognised enum values.
/// </summary>
[ExcludeFromCodeCoverage]
public class JsonExceptionHandler : ICustomExceptionHandler
{
    public bool CanHandle(Type exceptionType) =>
        exceptionType == typeof(JsonException) || exceptionType == typeof(BadHttpRequestException);

    public int Priority => 15;

    public ExceptionResponse Handle(Exception exception, Dictionary<string, object>? context = null)
    {
        var details = exception switch
        {
            JsonException jsonException => jsonException.Message,
            BadHttpRequestException badHttpRequestException => badHttpRequestException.Message,
            _ => exception.Message
        };

        return new ExceptionResponse
        {
            StatusCode = StatusCodes.Status400BadRequest,
            Message = "Invalid request data.",
            Details = details,
            ExceptionType = exception.GetType().Name
        };
    }
}
