using FastEndpoints;
using FastEndpoints.Swagger;
using TdpGis.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Tdp Gis API";
            s.Version = "v1";
        };
    });

var app = builder.Build();

app.UseHttpsRedirection();

app.UseDefaultExceptionHandler()
    .UseFastEndpoints()
    .UseSwaggerGen();

app.Run();