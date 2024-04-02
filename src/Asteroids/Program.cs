using Asteroids;
using Asteroids.Components;
using Observability;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.AddObservability();

var signalREnv = new SignalREnv();
builder.Configuration.GetRequiredSection(nameof(SignalREnv)).Bind(signalREnv);
builder.Services.AddSingleton(signalREnv);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
