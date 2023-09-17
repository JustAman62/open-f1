using OpenF1.Data;
using OpenF1.Data.Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddLiveTimingProvider();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapSimulatorApis();

// Start Processors
var processingService = app.Services.GetRequiredService<ProcessingService>();
await processingService.StartAsync();

app.Run();
