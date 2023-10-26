using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GatewayService.Controllers;


[ApiController]
[Route("api/TestObjects")]
public class TestObjectServiceController : ControllerBase
{
    private readonly IProducer _testObjectProducer;

    public TestObjectServiceController(IOptions<TestObjectServiceConfig> configOptions)
    {
        _testObjectProducer = new TestObjectProducer(configOptions);
    }

    [HttpPost]
    public async Task<IActionResult> AddSingleAsync([FromBody] TestObjectDto leakTestDto)
    {
        const string queueName = "add-single-requests";
        const string routingKey = "add-single-route";
        
        try
        {
            var response = await _testObjectProducer.SendMessage(leakTestDto, queueName, routingKey);
            
            Console.WriteLine("in controller: " + response);
            return Ok(response);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
}