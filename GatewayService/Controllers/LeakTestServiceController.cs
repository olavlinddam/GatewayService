using GatewayService.Configuration;
using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace GatewayService.Controllers;

[ApiController]
[Route("[controller]")]
public class LeakTestServiceController : ControllerBase
{
    private readonly IRabbitMqProducer _leakTestProducer;

    public LeakTestServiceController(IOptions<LeakTestServiceConfig> configOptions)
    {
        _leakTestProducer = new RabbitMqProducer(configOptions.Value);
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        _leakTestProducer.SendMessage("hej");

        return Ok();
    }
    
    [HttpPost]
    public async Task<IActionResult> AddSingleAsync()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
        
            var leakTest = JObject.Parse(body);
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