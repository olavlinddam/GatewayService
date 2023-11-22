using System.Text;
using GatewayService.Models.DTOs;
using GatewayService.Services.RabbitMq;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GatewayService.Controllers
{
  [ApiController]
  [Route("api/MongoDB")]
  public class MongoDBController : GatewayControllerBase
  {

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
      using (var client = new HttpClient())
      {
        try
        {
          var response =
            await client.GetAsync(
              "https://eu-central-1.aws.data.mongodb-api.com/app/testapplication-ischo/endpoint/GetTestResult");
          if (response.IsSuccessStatusCode)
          {
            var data = await response.Content.ReadAsStringAsync();
            return Ok(data);
          }
          else
          {
            var error = await response.Content.ReadAsStringAsync();
            return BadRequest($"Fejl ved indhentning af data: {error}");
          }
        }
        catch (Exception ex)
        {
          return StatusCode(500, $"Intern serverfejl: {ex.Message}");
        }
      }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTestResult([FromBody] TestResultatDto testResult)
    {
      if (testResult == null)
      {
        return BadRequest("Invalid test result data");
      }

      try
      {
        var client = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(testResult), Encoding.UTF8, "application/json");
        var response =
          await client.PostAsync(
            "https://eu-central-1.aws.data.mongodb-api.com/app/testapplication-ischo/endpoint/CreateTestResult",
            content);

        if (response.IsSuccessStatusCode)
        {
          return Ok(await response.Content.ReadAsStringAsync());
        }
        else
        {
          return BadRequest("Error while creating test result");
        }
      }
      catch (Exception ex)
      {
        return StatusCode(500, $"Intern serverfejl: {ex.Message}");
      }
    }
  }

  public class TestResultatDto
  {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; }

    public string ownerId { get; set; }

    public string description { get; set; }

    public string name { get; set; }

    public string objectType { get; set; }

    public string reason { get; set; }

    public string sniffingPoint { get; set; }

    public string status { get; set; }
  }
}
