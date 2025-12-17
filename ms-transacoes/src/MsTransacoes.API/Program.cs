using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HealthChecks.RabbitMQ;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MsTransacoes.API.Context;
using MsTransacoes.API.Middleware;
using MsTransacoes.Application.Interfaces;
using MsTransacoes.Application.Services;
using MsTransacoes.Infra.Audit;
using MsTransacoes.Infra.ExternalServices;
using MsTransacoes.Infra.Messaging;
using MsTransacoes.Infra.Persistence;
using MsTransacoes.Domain.Repositories;
using MsTransacoes.Infra.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MS-Transacoes",
        Version = "v1"
    });

    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Basic Auth (admin/admin123 ou user/user123)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
builder.Services.AddSingleton<IUserContextAccessor, UserContextAccessor>();

builder.Services.AddSingleton<AuditInterceptor>();

builder.Services.AddDbContext<TransacoesDbContext>((serviceProvider, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
        ?? "Host=localhost;Database=poc_auditoria;Username=poc_user;Password=poc_password;SearchPath=transacoes";

    options.UseNpgsql(connectionString);
    options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
});

builder.Services.AddScoped<ITransacaoService, TransacaoService>();
builder.Services.AddScoped<ITransacaoRepository, TransacaoRepository>();
builder.Services.AddSingleton<IAuditEventPublisher, RabbitMQPublisher>();

builder.Services.AddHttpClient<IContasApiClient, ContasApiClient>(client =>
{
    var baseUrl = builder.Configuration["Services:MsContas"] ?? "http://localhost:8080";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TransacoesDbContext>(name: "db")
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=poc_auditoria;Username=poc_user;Password=poc_password;SearchPath=transacoes",
        name: "postgres")
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMQ:User"] ?? "guest"}:{builder.Configuration["RabbitMQ:Password"] ?? "guest"}@{builder.Configuration["RabbitMQ:Host"] ?? "localhost"}/",
        name: "rabbitmq");

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var statusCode = exception switch
        {
            MsTransacoes.Application.Exceptions.NotFoundException => StatusCodes.Status404NotFound,
            MsTransacoes.Application.Exceptions.BusinessException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = exception switch
            {
                MsTransacoes.Application.Exceptions.NotFoundException => "Resource not found",
                MsTransacoes.Application.Exceptions.BusinessException => "Business rule violation",
                _ => "Unexpected error"
            },
            Detail = exception?.Message,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    });
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SimpleAuthMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
