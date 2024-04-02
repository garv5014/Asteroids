using Observability;
using Raft_Node;
using Raft_Node.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.AddApiOptions();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

builder.AddObservability();

builder.Services.AddSingleton<IRaftNodeClient, RaftNodeClient>();

builder.Services.AddSingleton<RaftNodeService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RaftNodeService>());
var app = builder.Build();

try
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapControllers();
    Console.WriteLine(
        "Node Identifier: " + app.Services.GetRequiredService<ApiOptions>().NodeIdentifier
            ?? "no service"
    );
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Error in the startup peaches {ex.Message}");
}
