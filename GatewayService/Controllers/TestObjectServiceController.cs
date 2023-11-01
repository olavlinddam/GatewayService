using System.Text.Json;
using GatewayService.Configuration;
using GatewayService.Models.DTOs;
using GatewayService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GatewayService.Controllers;


[ApiController]
[Route("api/TestObjects")]
public class TestObjectServiceController : GatewayControllerBase
{
    private readonly IProducer _testObjectProducer;

    public TestObjectServiceController(IOptions<TestObjectServiceConfig> configOptions)
    {
        _testObjectProducer = new TestObjectProducer(configOptions);
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

            testObjectDto.Id = Guid.Parse(response);
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            testObjectDto.Links = new Dictionary<string, string>()
            {
                { "self", $"{baseUrl}/api/TestObjects/{testObjectDto.Id}" }
            };
            
            Console.WriteLine("in controller: " + response);
            return CreatedAtAction(nameof(GetById), new { id = testObjectDto.Id }, testObjectDto); 
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
            var response = await _testObjectProducer.SendMessage(id, queueName, routingKey);

            TestObjectDto? testObject;
            try
            {
                testObject = JsonSerializer.Deserialize<TestObjectDto>(response);
            }
            catch (JsonException)
            {
                // If deserialization fails, it might be a plain string error message
                return BadRequestWithDetails($"The request could not be processed due to: {response}");
            }

            if (testObject == null)
            {
                return NotFound("Test object not found");
            }
        
            // Add HATEOAS links
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            testObject.Links = new Dictionary<string, string>
            {
                { "self", $"{baseUrl}/api/TestObjects/{testObject.Id}" }
            };
        
            return Ok(testObject);
        }
        catch (Exception e)
        {
            // Log the exception here
            return BadRequestWithDetails($"The request could not be processed due to: {e.Message}");
        }
    }
    
    
    #endregion
    
}