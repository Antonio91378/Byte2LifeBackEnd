using Byte2Life.API.Converters;
using Byte2Life.API.Models;
using Byte2Life.API.Services;
using LiteDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetValue<string>("LiteDbSettings:ConnectionString") ?? "Byte2Life.db";
builder.Services.AddSingleton<LiteDatabase>(sp => new LiteDatabase(connectionString));

builder.Services.AddSingleton<IFilamentService, FilamentService>();
builder.Services.AddSingleton<IClientService, ClientService>();
builder.Services.AddSingleton<ISaleService, SaleService>();
builder.Services.AddSingleton<IBudgetService, BudgetService>();
builder.Services.AddSingleton<IInvestmentService, InvestmentService>();
builder.Services.AddSingleton<IPaintService, PaintService>();
builder.Services.AddSingleton<IReminderService, ReminderService>();
builder.Services.AddSingleton<IServiceProviderService, ServiceProviderService>();
builder.Services.AddSingleton<IDesignTaskService, DesignTaskService>();
builder.Services.AddSingleton<PaintingTaskService>();
builder.Services.AddSingleton<IPaintingTaskService>(sp => sp.GetRequiredService<PaintingTaskService>());

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

var app = builder.Build();

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

app.MapControllers();

app.Run();

public partial class Program { }
