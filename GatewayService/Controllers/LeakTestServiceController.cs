using GatewayService.Configuration;
using GatewayService.Models;
using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GatewayService.Controllers;

[ApiController]
[Route("[controller]")]
public class LeakTestServiceController : ControllerBase
{
    private readonly IProducer _leakTestProducer;
    private readonly PendingRequestManager _pendingRequestManager;

    public LeakTestServiceController(IOptions<LeakTestServiceConfig> configOptions, PendingRequestManager pendingRequestManager)
    {
        _pendingRequestManager = pendingRequestManager;
        _leakTestProducer = new LeakTestProducer(configOptions);
    }

    [HttpGet("test")]
    public string Test()
    {
        return "hej"; 
    }
    
    [HttpPost("AddSingle")]
    public async Task<IActionResult> AddSingleAsync()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var response = await _leakTestProducer.SendMessage(body);
            
            
            Console.WriteLine("in controller: " + response);
            return Ok(response);
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
        
            var leakTest = JsonSerializer.Deserialize<string>(body);
            
            _leakTestProducer.SendMessage(leakTest);
            return Ok();
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
}