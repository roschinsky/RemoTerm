using TRoschinsky.RemoTerm;

namespace TRoschinsky.RemoTerm.Api;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        var configs = GetBaseConfigs();

        app.MapGet("/api/remoterm/{id}", (string id) =>
        {
            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("Id parameter is required");
            }

            var config = configs.Find(c => c.Id == id);
            if (config == null)
            {
                return Results.NotFound("Config not found");
            }

            return Results.Ok(config);
        })
        .WithName("GetRemoTermConfig");

        app.MapPost("/api/remoterm", async (HttpRequest req, Stream body) =>
        {
            try
            {
                var log = body.Length > 0 ? await System.Text.Json.JsonSerializer.DeserializeAsync<object>(body) : null;
                if (log == null)
                {
                    return Results.BadRequest("Invalid log data");
                }
                else
                {
                    // Process the log data here (e.g., save to a database, write to a file, etc.)
                    Console.WriteLine($"Received log: {System.Text.Json.JsonSerializer.Serialize(log)}");
                }

            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error processing log: {ex.Message}");
            }
            return Results.Ok("Log received");
        })
        .WithName("SetRemoTermExecutionLog");

        app.Run();
    }

    private static List<Config> GetBaseConfigs()
    {
        var result = new List<Config>();

        result.Add(new Config()
        {
            Id = "1",
            OnlyInLockedMode = true,
            DelayString = "random120",
            Actions =
            [
                new ActionConfig()
                    {
                        Type = "Terminate",
                        Title = "end all calculations",
                        Payload = "calc"
                    },
                new ActionConfig()
                    {
                        Type = "Message",
                        Title = "say hi",
                        Payload = "Caption...|Hey buddy!|Information"
                    },
                new ActionConfig()
                    {
                        Type = "Execute",
                        Title = "run terminal",
                        Payload = "cmd.exe"
                    }
            ]
        });

        result.Add(new Config()
        {
            Id = "42",
            OnlyInLockedMode = false,
            DelayString = "42",
            Actions =
            [
                new ActionConfig()
                    {
                        Type = "Terminate",
                        Title = "end all calculations",
                        Payload = "discord"
                    },
                new ActionConfig()
                    {
                        Type = "Message",
                        Title = "Show id",
                        Payload = "ID of config|#42|Information"
                    }
            ]
        });

        return result;

    }
}
