using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using GatewayService.Models.DTOs;
using GatewayService.Services.RabbitMq;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GatewayService.Controllers;

[ApiController]
[Route("api/LeakTests")]
public class LeakTestServiceController : GatewayControllerBase
{
    private readonly IProducer _leakTestProducer;

    public LeakTestServiceController(IProducer leakTestProducer)
    {
        _leakTestProducer = leakTestProducer;
    }

    [HttpGet("test")]
    public string Test()
    {
        
        return "test"; 
    }
    
    [HttpPost]
    public async Task<IActionResult> AddSingleAsync([FromBody] LeakTestDto leakTestDto)
    {
        const string queueName = "add-single-requests";
        const string routingKey = "add-single-route";
        try
        {
            var response = await _leakTestProducer.SendMessage(leakTestDto, queueName, routingKey);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LeakTestDto>>(response);
            
            if (apiResponse.ErrorMessage != null)
            {
                return BadRequestWithDetails(apiResponse.ErrorMessage, "Single test result", "N/A");
            }
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            apiResponse.Data.Links = new Dictionary<string, string>()
            {
                { "self", $"{baseUrl}/api/LeakTests/{apiResponse.Data.LeakTestId}" }
            };

            Console.WriteLine("in controller: " + response);
            return CreatedAtAction(nameof(GetById), new { id = apiResponse.Data.LeakTestId }, apiResponse);
        }
        
        catch (TimeoutException e)
        {
            const string? item = "Single test result"; // what was being processed
            var id = leakTestDto.LeakTestId?.ToString() ?? "N/A"; // Using the ID if available, otherwise "N/A"

            return TimedOutRequestWithDetails(e.Message, item, id);
        }
        
        catch (Exception e)
        {
            // Log the exception here
    
            const string? item = "Single test result"; // what was being processed
            var id = leakTestDto.LeakTestId?.ToString() ?? "N/A"; // Using the ID if available, otherwise "N/A"
    
            return BadRequestWithDetails($"The request could not be processed due to: {e.Message}", item, id);
        }
    }

    
    [HttpPost("Batch")]
    public async Task<IActionResult> AddBatchAsync([FromBody] List<LeakTestDto> leakTestDtos)
    {
        const string queueName = "add-batch-requests";
        const string routingKey = "add-batch-route";
        try
        {
            var response = await _leakTestProducer.SendMessage(leakTestDtos, queueName, routingKey);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<LeakTestDto>>>(response);

            if (apiResponse.StatusCode == 400 && apiResponse.ErrorMessage.StartsWith("Please provide a reason"))
            {
                string pattern = @"<(.+?)>";
                Match match = Regex.Match(apiResponse.ErrorMessage, pattern);

                const string? item = "Batch of test results";
                return BadRequestWithDetails(apiResponse.ErrorMessage, id: match.Groups[1].Value);
            }
            
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            foreach (var leakTestDto in apiResponse.Data)
            {
                leakTestDto.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{leakTestDto.LeakTestId}" }
                };
            }


            Console.WriteLine("in controller: " + response);
            return CreatedAtAction(nameof(GetAll), apiResponse);
        }
        
        catch (TimeoutException e)
        {
            const string? item = "Batch of test results"; // what was being processed
            var ids = new StringBuilder();
            leakTestDtos.ForEach(x => ids.Append($"{x.LeakTestId.ToString()}, ")); // Using the ID if available, otherwise "N/A"
            
            return TimedOutRequestWithDetails(e.Message, item, ids.ToString());
        }
        
        catch (Exception e)
        {
            // Log the exception here
    
            const string? item = "Batch of test results"; // what was being processed
            var ids = new StringBuilder();
            leakTestDtos.ForEach(x => ids.Append($"{x.LeakTestId.ToString()}, ")); // Using the ID if available, otherwise "N/A"
    
            return BadRequestWithDetails($"The request could not be processed due to: {e.Message}", item, ids.ToString());
        }
    }


    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        const string queueName = "get-by-id-requests";
        const string routingKey = "get-by-id-route";
        try
        {
            var response = await _leakTestProducer.SendMessage(id, queueName, routingKey);

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<LeakTestDto>>(response);

            
            // Add HATEOAS links
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            if (apiResponse != null)
                
            {
                apiResponse.Data.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{apiResponse.Data.LeakTestId}" }
                };
            }
            
            return Ok(apiResponse);
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

            if (response == null)
            {
                return NotFound(new ApiResponse<TestObjectDto>
                {
                    StatusCode = 404,
                    ErrorMessage = "Data not found"
                });
            }

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<LeakTestDto>>>(response);
            
            // Add HATEOAS links
            apiResponse.Data?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(apiResponse);
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

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<LeakTestDto> >>(response);
            
            // Add HATEOAS links
            apiResponse.Data?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(apiResponse);
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

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<LeakTestDto> >>(response);
            
            // Add HATEOAS links
            apiResponse.Data?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(apiResponse);
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

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<LeakTestDto> >>(response);
            
            // Add HATEOAS links
            apiResponse.Data?.ForEach(t =>
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                t.Links = new Dictionary<string, string>()
                {
                    { "self", $"{baseUrl}/api/LeakTests/{t.LeakTestId}" }
                };
            });
            
            return Ok(apiResponse);
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