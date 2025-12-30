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

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseStaticFiles(); // Enable static files for photo uploads

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
