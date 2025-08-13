using HybridCacheDemo.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Default UI at /swagger
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();