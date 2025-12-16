using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MsTransacoes.Application.Interfaces;

namespace MsTransacoes.API.Middleware;

public sealed class SimpleAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SimpleAuthMiddleware> _logger;

    private static readonly HashSet<PathString> PublicPrefixes = new()
    {
        new PathString("/swagger"),
        new PathString("/health")
    };

    public SimpleAuthMiddleware(RequestDelegate next, ILogger<SimpleAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContextAccessor userContext)
    {
        if (PublicPrefixes.Any(prefix => context.Request.Path.StartsWithSegments(prefix)))
        {
            await _next(context);
            return;
        }

        var authorization = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorizedAsync(context, "Missing Authorization header");
            return;
        }

        var encoded = authorization["Basic ".Length..].Trim();
        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }
        catch
        {
            await WriteUnauthorizedAsync(context, "Invalid Authorization header");
            return;
        }

        var parts = decoded.Split(':', 2);
        if (parts.Length != 2)
        {
            await WriteUnauthorizedAsync(context, "Invalid credentials format");
            return;
        }

        var username = parts[0];
        var password = parts[1];

        var isValid = (username == "admin" && password == "admin123")
            || (username == "user" && password == "user123");

        if (!isValid)
        {
            _logger.LogWarning("Invalid credentials for user {Username}", username);
            await WriteUnauthorizedAsync(context, "Invalid credentials");
            return;
        }

        userContext.SetCurrentUserId(username);

        await _next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Basic";
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = (int)HttpStatusCode.Unauthorized,
            Title = "Unauthorized",
            Detail = detail,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
