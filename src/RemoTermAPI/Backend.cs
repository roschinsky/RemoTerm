using System.Text.Json;
using TRoschinsky.Common;

namespace TRoschinsky.RemoTerm.Api;

public partial class Backend
{
    private static string? apiTokenReader;
    private static string? apiTokenWriter;
    private static List<Config> configurations = [];
    private static Dictionary<string, Dictionary<DateTime, List<LogEntry>>> logs = [];

    private static void Main(string[] args)
    {
        configurations = GetBaseConfigs();

        try
        {
            apiTokenReader = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APITokenRead")) ? string.Empty : Environment.GetEnvironmentVariable("APITokenRead")?.Trim();
            apiTokenWriter = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APITokenWrite")) ? string.Empty : Environment.GetEnvironmentVariable("APITokenWrite")?.Trim();
        }
        catch (Exception) { }

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

        #region Configs

        app.MapGet("/api/configs/{id}", (string id) =>
        {
            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("Id parameter is required");
            }

            var config = configurations.Find(c => c.Id == id);
            if (config == null)
            {
                return Results.NotFound("Config not found");
            }

            return Results.Ok(config);
        })
        .WithName("GetRemoTermConfig");

        app.MapGet("/api/configs", (HttpRequest req) =>
        {
            if (!req.Headers.TryGetValue("Authorization", out var authHeader) || (authHeader != $"Bearer {apiTokenReader}" && authHeader != $"Bearer {apiTokenWriter}"))
            {
                return Results.Ok(configurations.Where(c => c.IsPublic));
            }
            else
            {
                return Results.Ok(configurations);
            }
        })
        .WithName("GetRemoTermConfigs");

        app.MapPost("/api/configs/{id}", async (string id, Config config, HttpRequest req) =>
        {
            if (!req.Headers.TryGetValue("Authorization", out var authHeader) || authHeader != $"Bearer {apiTokenWriter}")
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("Id parameter is required");
            }

            try
            {
                if (configurations.Any(c => c.Id == id))
                {
                    return Results.Conflict($"Config #{id} already existing - delete it first or use a different id");
                }

                if (config == null)
                {
                    return Results.Conflict("Invalid config data");
                }
                else if (config.Id != id)
                {
                    return Results.Conflict($"Config #'{config.Id}' does not match the id in the URL '{id}'");
                }
                else
                {
                    configurations.Add(config);
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error processing config: {ex.Message}");
            }
            return Results.Created();
        })
        .WithName("CreateRemoTermConfig");

        app.MapDelete("/api/configs/{id}", (string id, HttpRequest req) =>
        {
            if (!req.Headers.TryGetValue("Authorization", out var authHeader) || authHeader != $"Bearer {apiTokenWriter}")
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("Id parameter is required");
            }

            var config = configurations.Find(c => c.Id == id);
            if (config == null)
            {
                return Results.NotFound("Config not found");
            }

            configurations.Remove(config);
            return Results.Accepted($"Config #{id} deleted.");
        })
        .WithName("DeleteRemoTermConfig");

        #endregion


        #region Logs

        app.MapGet("/api/logs/{id}", (string id) =>
        {
            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("Id parameter is required");
            }

            try
            {
                if (!logs.ContainsKey(id))
                {
                    return Results.NotFound("Logs not found");
                }

                var logsForId = logs[id];
                return Results.Ok(logsForId);

            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error obtaining logs for {id}: {ex.Message}");
            }
        })
        .WithName("GetRemoTermExecutionLog");


        app.MapPost("/api/logs/{id}", async (string id, LogEntry[] log) =>
        {
            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("Id parameter is required");
            }

            try
            {
                if (!logs.ContainsKey(id))
                {
                    logs[id] = [];
                }

                //var log = body.Length > 0 ? await JsonSerializer.DeserializeAsync<List<JournalEntry>>(body) : null;
                if (log == null)
                {
                    return Results.BadRequest("Invalid log data");
                }
                else
                {
                    logs[id].Add(DateTime.Now, log.ToList());
                }
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Error processing log: {ex.Message}");
            }
            return Results.Ok("Log saved successfully");
        })
        .WithName("SetRemoTermExecutionLog");


        #endregion

        app.Run();
    }

    #region Helper Methods

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
                        ActionType = ActionType.Terminate,
                        Title = "end all calculations",
                        Payload = "calc"
                    }
            ]
        });

        result.Add(new Config()
        {
            Id = "23",
            OnlyInLockedMode = false,
            IsPublic = false,
            DelayString = "random10",
            Actions =
            [
                new ActionConfig()
                    {
                        ActionType = ActionType.Terminate,
                        Title = "end all shells",
                        Payload = "cmd.exe"
                    },
                new ActionConfig()
                    {
                        ActionType = ActionType.Message,
                        Title = "say hi",
                        Payload = "Caption...|Hey buddy, how are you?|Question"
                    },
                new ActionConfig()
                    {
                        ActionType = ActionType.Execute,
                        Title = "run a calculator",
                        Payload = "calc.exe"
                    }
            ]
        });

        result.Add(new Config()
        {
            Id = "42",
            OnlyInLockedMode = false,
            DelayString = "2",
            Actions =
            [
                new ActionConfig()
                    {
                        ActionType = ActionType.Terminate,
                        Title = "end all drawing",
                        Payload = "mspaint"
                    },
                new ActionConfig()
                    {
                        ActionType = ActionType.Message,
                        Title = "Show id",
                        Payload = "ID of config|#42|Information"
                    }
            ]
        });

        return result;
    }

    #endregion
}
