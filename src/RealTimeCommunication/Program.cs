using Akka.Hosting;
using Observability;
using RealTimeCommunication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.AddObservability();

builder.Services.AddAkka(
    "AsteroidsSystem",
    configurationBuilder =>
    {
        configurationBuilder.WithActors(
            (system, registry) => {
                //register actors here
            }
        );
    }
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<ActorHub>("/actorHub");
app.Run();
