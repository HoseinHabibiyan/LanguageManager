using System.Globalization;
using LanguageManager;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddLanguageManager((option) =>
{
    option.ResourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
    option.Cultures = ["fa", "en"];
});

builder.Services.AddRequestLocalization((options) =>
{
    options.SupportedCultures =
    [
        new CultureInfo("en"),
        new CultureInfo("fa")
    ];
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMultiLanguage();

app.UseHttpsRedirection();

app.MapGet("/", ([FromServices]ILocalization localization , CancellationToken ct) => Results.Ok((object?)localization.Get("Hi",ct)))
    .WithOpenApi();

app.Run();
