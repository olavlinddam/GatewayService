using System.Text.Json;
using GatewayService.Configuration;
using GatewayService.Models;
using GatewayService.Models.DTOs;
using GatewayService.Models.ErrorModels;
using GatewayService.Services;
using GatewayService.Services.Aggregation;
using GatewayService.Services.RabbitMq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GatewayService.Controllers;


[ApiController]
[Route("api/TestObjects")]
public class TestObjectServiceController : GatewayControllerBase
{
    private readonly IProducer _testObjectProducer;
    private readonly IAggregationService _aggregationService;

    public TestObjectServiceController(IProducer testObjectProducer, IAggregationService aggregationService)
    {
        _testObjectProducer = testObjectProducer;
        _aggregationService = aggregationService;
    }

    [HttpGet("TestObjectWithTestResults/{id:guid}")]
    public async Task<IActionResult> GetTestObjectWithTestResults(Guid id)
    {
        var apiResponse = await _aggregationService.GetTestObjectWithResults(id);

        return Ok(apiResponse);
    }
    
    [HttpPut]
    public async Task<IActionResult> UpdateSingleAsync([FromBody] TestObjectDto testObjectDto)
    {
        const string queueName = "update-single-test-object-requests";
        const string routingKey = "update-single-test-object-route";

        try
        {
            var response = await _testObjectProducer.SendMessage(testObjectDto, queueName, routingKey);
            if (response == null)
            {
                return NotFound(new ApiResponse<TestObjectDto>
                {
                    StatusCode = 404,
                    ErrorMessage = "Data not found"
                });
            }

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestObjectDto>>(response);

            // Check if the Data in ApiResponse is not null
            if(apiResponse.Data == null)
            {
                return BadRequest("Data in the response was null.");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            apiResponse.Data.Links = new Dictionary<string, string>()
            {
                { "self", $"{baseUrl}/api/TestObjects/{apiResponse.Data.Id}" }
            };
        
            Console.WriteLine("in controller: " + response);
            return CreatedAtAction(nameof(GetById), new { id = apiResponse.Data.Id }, apiResponse); 
        }
        catch (TimeoutException e)
        {
            const string? item = "Single test result"; 
            var id = testObjectDto.Id.ToString() ?? "N/A"; 
    
            Console.WriteLine(e.Message);
            return TimedOutRequestWithDetails(e.Message, item, id);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }  
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSingleAsync(Guid id)
    {
        const string queueName = "delete-single-test-object-requests";
        const string routingKey = "delete-single-test-object-route";

        try
        {
            var response = await _testObjectProducer.SendMessage(id, queueName, routingKey);
            if (response == null)
            {
                return NotFound(new ApiResponse<TestObjectDto>
                {
                    StatusCode = 404,
                    ErrorMessage = "Data not found"
                });
            }

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestObjectDto>>(response);
            
            Console.WriteLine("in controller: " + response);
            return Ok(apiResponse);
        }
        catch (TimeoutException e)
        {
            const string? item = "Single test result"; 
            Console.WriteLine(e.Message);
            return TimedOutRequestWithDetails(e.Message, item, id.ToString());
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }


    #region Post
    
    [HttpPost]
    public async Task<IActionResult> AddSingleAsync([FromBody] TestObjectDto testObjectDto)
    {
        const string queueName = "add-single-test-object-requests";
        const string routingKey = "add-single-test-object-route";
        
        try
        {
            var response = await _testObjectProducer.SendMessage(testObjectDto, queueName, routingKey);
            
            if (response == null)
            {
                return NotFound(new ApiResponse<TestObjectDto>
                {
                    StatusCode = 404,
                    ErrorMessage = "Data not found"
                });
            }

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestObjectDto>>(response);

            // Check if the Data in ApiResponse is not null
            if(apiResponse.Data == null)
            {
                return BadRequest("Data in the response was null.");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            apiResponse.Data.Links = new Dictionary<string, string>()
            {
                { "self", $"{baseUrl}/api/TestObjects/{apiResponse.Data.Id}" }
            };
        
            Console.WriteLine("in controller: " + response);
            return CreatedAtAction(nameof(GetById), new { id = apiResponse.Data.Id }, apiResponse); 
        }
        catch (TimeoutException e)
        {
            const string? item = "Single test result"; 
            var id = testObjectDto.Id.ToString() ?? "N/A"; 
    
            Console.WriteLine(e.Message);
            return TimedOutRequestWithDetails(e.Message, item, id);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequest($"The request could not be processed due to: {e.Message}");
        }
    }
    #endregion

    #region Get

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        const string queueName = "get-single-test-object-requests";
        const string routingKey = "get-single-test-object-route";
        try
        {
            Console.WriteLine("Endpoint hit");
            var response = await _testObjectProducer.SendMessage(id, queueName, routingKey);
            
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<TestObjectDto>>(response);
            if (apiResponse == null)
            {
                return BadRequest("Invalid response from the service.");
            }

            switch (apiResponse.StatusCode)
            {
                case 200:
                {
                    // Add HATEOAS links
                    var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                    apiResponse.Data.Links = new Dictionary<string, string>
                    {
                        { "self", $"{baseUrl}/api/TestObjects/{apiResponse.Data?.Id}" }
                    };
                
                    // return the test object
                    return Ok(apiResponse);
                }
                case 404:
                    return NotFoundWithDetails(apiResponse.ErrorMessage, null, id.ToString());
                default:
                    var statusCode = apiResponse.StatusCode;
                    // Something went wrong, handle accordingly
                    return StatusCode(statusCode, apiResponse.ErrorMessage);
            }
        }
        catch (TimeoutException e)
        {
            const string? item = "Single test result"; // what was being processed
            return TimedOutRequestWithDetails(e.Message, item, id.ToString());
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequestWithDetails($"The request could not be processed due to: {e.Message}");
        }
    }
    
    
    #endregion
    
}