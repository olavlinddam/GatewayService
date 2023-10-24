using System.Text;
using GatewayService.Configuration;
using GatewayService.Models;
using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;
using GatewayService.Models.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GatewayService.Controllers;

[ApiController]
[Route("api/LeakTests")]
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
        return "test"; 
    }
    
    [HttpPost]
    public async Task<IActionResult> AddSingleAsync([FromBody] LeakTestDto leakTestDto)
    {
        try
        {
            var response = await _leakTestProducer.SendMessage(leakTestDto, "add-single-requests", "add-single-route");
            
            
            Console.WriteLine("in controller: " + response);
            return Ok(response);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    [HttpPost("Batch")]
    public async Task<IActionResult> AddBatchAsync()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            var response = await _leakTestProducer.SendMessage(body, "add-batch-requests", "add-batch-route");
            
            
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

            var leakTest = JsonSerializer.Deserialize<LeakTestDto>(response);

            
            // Add HATEOAS links
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            var controllerName = ControllerContext.ActionDescriptor.ControllerName;
            if (leakTest != null)
            {
                leakTest.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{leakTest.LeakTestId}" }
                };
            }
            
            return Ok(leakTest);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        const string queueName = "get-all-requests";
        const string routingKey = "get-all-route";
        
        try
        {
            var response = await _leakTestProducer.SendMessage("", queueName, routingKey);

            var leakTests = JsonSerializer.Deserialize<List<LeakTestDto>>(response);


            // Add HATEOAS links
            leakTests?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(leakTests);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    [HttpGet("Tag")]
    public async Task<IActionResult> GetByTagAsync(string key, string value)
    {
        const string queueName = "get-by-tag-requests";
        const string routingKey = "get-by-tag-route";
        
        try
        {
            var response = await _leakTestProducer.SendMessage($"{key};{value}", queueName, routingKey);

            var leakTests = JsonSerializer.Deserialize<List<LeakTestDto>>(response);


            // Add HATEOAS links
            leakTests?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(leakTests);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    [HttpGet("Field")]
    public async Task<IActionResult> GetByFieldAsync(string key, string value)
    {
        const string queueName = "get-by-field-requests";
        const string routingKey = "get-by-field-route";
        
        try
        {
            var response = await _leakTestProducer.SendMessage($"{key};{value}", queueName, routingKey);

            var leakTests = JsonSerializer.Deserialize<List<LeakTestDto>>(response);


            // Add HATEOAS links
            leakTests?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(leakTests);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    
    [HttpGet("TimeFrame")]
    public async Task<IActionResult> GetWithinTimeFrameAsync(string start, string stop)
    {
        const string queueName = "get-within-timeframe-requests";
        const string routingKey = "get-within-timeframe-route";
        
        try
        {
            var response = await _leakTestProducer.SendMessage($"{start};{stop}", queueName, routingKey);

            var leakTests = JsonSerializer.Deserialize<List<LeakTestDto>>(response);


            // Add HATEOAS links
            leakTests?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(leakTests);
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