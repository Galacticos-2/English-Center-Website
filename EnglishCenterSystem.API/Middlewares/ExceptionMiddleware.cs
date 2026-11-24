using System.Net;
using System.Text.Json;

using EnglishCenterSystem.Application.Common.Models;

namespace EnglishCenterSystem.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode =
                (int)HttpStatusCode.BadRequest;

            context.Response.ContentType =
                "application/json";

            var response =
                ApiResponse<object>
                .FailResponse(ex.Message);

            var json =
                JsonSerializer.Serialize(response);

            await context.Response
                .WriteAsync(json);
        }
    }
}