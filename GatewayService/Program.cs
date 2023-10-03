

using GatewayService.Configuration;
using GatewayService.StartUp;

var builder = WebApplication.CreateBuilder(args);

// uses the extension method to read from the wanted appsettings.json file. This information is stored in the 
// builder.Configuration().
builder.Host.ConfigureAppSettings();



// Add services to the container. First we add the InfluxDbConfig to the dependency injection container. 
builder.Services.Configure<LeakTestServiceConfig>(builder.Configuration.GetSection("RabbitMqConfigurations:LeakTestServiceConfig"));


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

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();