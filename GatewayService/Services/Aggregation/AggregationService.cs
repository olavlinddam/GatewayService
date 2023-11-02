using System.Text.Json;
using GatewayService.Models.DTOs;
using GatewayService.Services.RabbitMq;

namespace GatewayService.Services.Aggregation;

public class AggregationService : IAggregationService
{
    private readonly IProducer _leakTestProducer;
    private readonly IProducer _testObjectProducer;

    public AggregationService(IProducer testObjectTestProducer, IProducer leakTestProducer)
    {
        _leakTestProducer = leakTestProducer;
        _testObjectProducer = testObjectTestProducer;
    }

    public async Task<ApiResponse<TestObjectWithResultsDto>> GetTestObjectWithResults(Guid id)
    {
        const string testObjQueueName = "get-single-test-object-requests";
        const string testObjRoutingKey = "get-single-test-object-route";
        const string leakTestQueueName = "get-by-tag-requests";
        const string leakTestRoutingKey = "get-by-tag-route";

        try
        {
            Console.WriteLine("Fetching test object.");
            
            // Fetch TestObject
            var testObjectServiceResponse = await _testObjectProducer.SendMessage(id, testObjQueueName, testObjRoutingKey);
            var testObjectApiResponse = JsonSerializer.Deserialize<ApiResponse<TestObjectDto>>(testObjectServiceResponse);
            
            
            // Fetch LeakTests
            const string key = "TestObjectId"; // LeakTestService needs to know what LeakTest attribute it should retrieve data for. 
            var value = testObjectApiResponse.Data.Id; // LeakTestService needs to know what the value of the key is. 
            
            var leakTestServiceResponse = await _leakTestProducer.SendMessage($"{key};{value}", leakTestQueueName, leakTestRoutingKey);
            var leakTestApiResponse = JsonSerializer.Deserialize<ApiResponse<List<LeakTestDto> >>(leakTestServiceResponse);
            
            // Create the aggregated DTO
            var testObjectWithResultsDto = new TestObjectWithResultsDto()
            {
                TestObjectDto = testObjectApiResponse.Data,
                LeakTestDto = leakTestApiResponse?.Data
            };

            var aggregatedResponse = new ApiResponse<TestObjectWithResultsDto>()
            {
                StatusCode = 200,
                Data = testObjectWithResultsDto,
                ErrorMessage = null
            };

            return aggregatedResponse;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
}