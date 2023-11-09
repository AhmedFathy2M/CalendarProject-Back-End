using System.Text.Json;
using Core.Exceptions;

namespace API.Middlewares;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(context, e);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = exception switch
        {
            BadRequestException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedBusinessException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        var errorMessage = exception.Message;
        var stackTrace = exception.StackTrace;
        var statusCode = httpContext.Response.StatusCode;

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            message = errorMessage,
            trace = statusCode == StatusCodes.Status500InternalServerError ? stackTrace : null
        }));
    }
}