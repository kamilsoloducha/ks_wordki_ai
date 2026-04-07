using Serilog;
using Wordki.Modules.Cards.Api;
using Wordki.Modules.Lessons.Api;
using Wordki.Modules.Users.Api;
using Wordki.Bff.Api.BackgroundServices;
using Wordki.Bff.SharedKernel.Abstractions;
using Wordki.Bff.SharedKernel.Services;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration);

var seqServerUrl = builder.Configuration["Seq:ServerUrl"];
if (!string.IsNullOrWhiteSpace(seqServerUrl))
{
    var seqApiKey = builder.Configuration["Seq:ApiKey"];
    loggerConfiguration = loggerConfiguration.WriteTo.Seq(
        seqServerUrl,
        apiKey: string.IsNullOrWhiteSpace(seqApiKey) ? null : seqApiKey);
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

try
{
    builder.Services.AddOpenApi();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
    builder.Services.AddUsersModule();
    builder.Services.AddCardsModule();
    builder.Services.AddLessonsModule();
    builder.Services.AddHostedService<OutboxMessageProcessorHostedService>();

    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });
    }

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseCors();
        app.MapOpenApi();
    }

    app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));
    app.MapUsersEndpoints();
    app.MapCardsEndpoints();
    app.MapLessonsEndpoints();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
