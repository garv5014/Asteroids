using System.Text.Json;
using Asteroids;
using Asteroids.Components;
using Blazored.LocalStorage;
using Blazored.Toast;
using Observability;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.AddObservability();
builder.Services.AddBlazoredToast();
var signalREnv = new SignalREnv();
builder.Configuration.GetRequiredSection(nameof(SignalREnv)).Bind(signalREnv);
builder.Services.AddSingleton(signalREnv);

Console.WriteLine($"Actor Options: {JsonSerializer.Serialize(signalREnv)}");

builder.Services.AddBlazoredLocalStorage();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
