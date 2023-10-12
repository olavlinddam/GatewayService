using GatewayService.Configuration;
using GatewayService.Services;
using GatewayService.Services.LeakTestService.Producers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GatewayService.Controllers;

[ApiController]
[Route("[controller]")]
public class LeakTestServiceController : ControllerBase
{
    private readonly IProducer _leakTestProducer;
    private readonly IConsumer _leakTestConsumer;

    public LeakTestServiceController(IOptions<LeakTestServiceConfig> configOptions)
    {
        _leakTestProducer = new LeakTestProducer(configOptions);
        _leakTestConsumer = new LeakTestConsumer(configOptions);
    }

    [HttpGet("test")]
    public string Test()
    {
        return "hej"; 
    }
    
    [HttpPost("AddSingleConsumer")]
    public async Task<IActionResult> AddSingleAsync()
    {
        string routingKey = "leaktest.add-single";
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
        
            var leakTest = JObject.Parse(body);
            _leakTestProducer.SendMessage(leakTest, routingKey);
            return Ok();
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    [HttpPost("RabbitMqConsumer")]
    public async Task<IActionResult> AddSingleNoConsumerAsync()
    {
        string routingKey = "leaktest.RabbitMqConsumer";
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
        
            var leakTest = JObject.Parse(body);
            _leakTestProducer.SendMessage(leakTest, routingKey);
            return Ok();
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
}