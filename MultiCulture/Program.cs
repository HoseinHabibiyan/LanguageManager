using Microsoft.AspNetCore.Mvc;
using MultiCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

builder.Services.AddMultiLanguage((option) =>
{
    option.ResourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
    option.Cultures = new[] { "fa", "en" };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/", ([FromServices]ILocalization localization) =>
    {
        localization.GetAllString();
    })
    .WithOpenApi();

app.Run();
