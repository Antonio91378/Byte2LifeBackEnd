using Byte2Life.API.Converters;
using Byte2Life.API.Models;
using Byte2Life.API.Persistence;
using Byte2Life.API.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

builder.Services.AddSingleton<IFilamentService, FilamentService>();
builder.Services.AddSingleton<IClientService, ClientService>();
builder.Services.AddSingleton<ISaleService, SaleService>();
builder.Services.AddSingleton<ISaleAttachmentStorageService, SaleAttachmentStorageService>();
builder.Services.AddSingleton<IBudgetService, BudgetService>();
builder.Services.AddSingleton<IInvestmentService, InvestmentService>();
builder.Services.AddSingleton<IPaintService, PaintService>();
builder.Services.AddSingleton<IReminderService, ReminderService>();
builder.Services.AddSingleton<IServiceProviderService, ServiceProviderService>();
builder.Services.AddSingleton<IDesignTaskService, DesignTaskService>();
builder.Services.AddSingleton<IPrinterMonitorService, PrinterMonitorService>();
builder.Services.AddSingleton<PaintingTaskService>();
builder.Services.AddSingleton<IPaintingTaskService>(sp => sp.GetRequiredService<PaintingTaskService>());
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<IEmailService, EmailService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ObjectIdConverter());
    });

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await MongoConnectionVerifier.VerifyAsync(database, app.Logger);
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = context.Response.StatusCode,
            message = feature?.Error.Message,
            stackTrace = app.Environment.IsDevelopment() ? feature?.Error.StackTrace : null
        };
        await context.Response.WriteAsJsonAsync(payload);
    });
});

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.HasStarted || response.ContentLength.HasValue || response.ContentType != null)
    {
        return;
    }

    response.ContentType = "application/json";
    var payload = new
    {
        status = response.StatusCode,
        path = context.HttpContext.Request.Path.Value
    };
    await response.WriteAsJsonAsync(payload);
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseStaticFiles(); // Enable static files for photo uploads

app.UseAuthorization();

app.MapGet("/health/mongo", async (IMongoDatabase database) =>
{
    try
    {
        await MongoConnectionVerifier.PingAsync(database);

        return Results.Ok(new
        {
            status = "Healthy",
            database = database.DatabaseNamespace.DatabaseName,
            checkedAt = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "MongoDB health check failed");
    }
});

app.MapControllers();

app.Run();

public partial class Program { }
