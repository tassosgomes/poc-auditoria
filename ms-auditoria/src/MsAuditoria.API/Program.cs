using Microsoft.OpenApi.Models;
using MsAuditoria.API.Middleware;
using MsAuditoria.Application.Interfaces;
using MsAuditoria.Application.Services;
using MsAuditoria.Infra.Elasticsearch;
using MsAuditoria.Infra.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MS-Auditoria API",
        Version = "v1",
        Description = "API para consulta de eventos de auditoria"
    });

    // Configurar Basic Auth no Swagger
    c.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Autenticação Basic Auth. Use admin:admin123 ou user:user123"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Elasticsearch
builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();

// Serviços de aplicação
builder.Services.AddScoped<IAuditService, AuditService>();

// RabbitMQ Consumer (BackgroundService)
builder.Services.AddHostedService<AuditEventConsumer>();

// Inicializador de índices Elasticsearch
builder.Services.AddHostedService<IndexInitializer>();

var app = builder.Build();

// Swagger sempre habilitado (POC)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS-Auditoria API v1");
    c.RoutePrefix = "swagger";
});

// Middleware de autenticação
app.UseMiddleware<SimpleAuthMiddleware>();

app.MapControllers();

// Health check simples
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ms-auditoria" }));

app.Run();
