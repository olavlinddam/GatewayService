using System.Text;
using GatewayService.Configuration;
using GatewayService.Models;
using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

            var response = await _leakTestProducer.SendMessage(body, "add-single-requests", "add-single-route");
            
            
            Console.WriteLine("in controller: " + response);
            return Ok(response);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }


    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        const string routingKey = "GetById";
        try
        {
            var response = await _leakTestProducer.SendMessage(id, "get-by-id-requests", "get-by-id-route");

            // Add HATEOAS links
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var links = $"{baseUrl}/api/LeakTestService/{id}";
            var updatedResponse = AddValueToExistingProperty(response, "Links", links);
            
            return Ok(updatedResponse.ToString());
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    public JObject AddValueToExistingProperty(string inputJson, string propertyName, string value)
    {
        var json = JObject.Parse(inputJson);
        if (json[propertyName] != null) // Check if property exists
        {
            json[propertyName] = value;
        }
        return json;
    }


}