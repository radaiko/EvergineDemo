using EvergineDemo.Backend.Hubs;
using EvergineDemo.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add SignalR
builder.Services.AddSignalR();

// Add simulation service as hosted service
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<SimulationService>());

// Add CORS for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<SimulationHub>("/simulationHub");

app.Run();
