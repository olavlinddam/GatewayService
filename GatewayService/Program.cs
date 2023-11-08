using GatewayService.Configuration;
using GatewayService.Models;
using GatewayService.Services;
using GatewayService.Services.Aggregation;
using GatewayService.Services.RabbitMq;
using GatewayService.Services.Retry;
using GatewayService.StartUp;

var builder = WebApplication.CreateBuilder(args);


// uses the extension method to read from the wanted appsettings.json file. This information is stored in the 
// builder.Configuration().
builder.Host.ConfigureAppSettings();



// Add services to the container. First we add the InfluxDbConfig to the dependency injection container. 
builder.Services.Configure<LeakTestServiceConfig>(builder.Configuration.GetSection("RabbitMqConfigurations:LeakTestServiceConfig"));
builder.Services.Configure<TestObjectServiceConfig>(builder.Configuration.GetSection("RabbitMqConfigurations:TestObjectServiceConfig"));
builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMqConfig"));
builder.Services.AddScoped<IProducer, LeakTestProducer>();
builder.Services.AddScoped<IProducer, TestObjectProducer>();
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddSingleton<RabbitMqConnectionService>();
builder.Services.AddSingleton<IRetryService, RetryService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("*") 
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


builder.Services.AddControllers();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

// Middleware
app.UseCors("AllowWebApp");
app.MapControllers();

app.Run();