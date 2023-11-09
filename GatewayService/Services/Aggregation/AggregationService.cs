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
            var testObjectServiceResponse =
                await _testObjectProducer.SendMessage(id, testObjQueueName, testObjRoutingKey);
            var testObjectApiResponse = TrySerializeTestObjectResponse(testObjectServiceResponse);


            Console.WriteLine("Fetching test results");
            // Fetch LeakTests
            const string
                key = "TestObjectId"; // LeakTestService needs to know what LeakTest attribute it should retrieve data for. 
            var value = id; // LeakTestService needs to know what the value of the key is. 
            

            var leakTestServiceResponse = await _leakTestProducer.SendMessage($"{key};{value}", leakTestQueueName, leakTestRoutingKey);
            var leakTestApiResponse = TrySerializeLeakTestServiceResponse(leakTestServiceResponse);

            // Create the aggregated DTO
            var aggregatedResponse = CreateTestObjectWithResultsDto(leakTestApiResponse, testObjectApiResponse);

            return aggregatedResponse;

        }
        catch (DataAggregationFailedException e)
        {
            return new ApiResponse<TestObjectWithResultsDto>()
            {
                StatusCode = 400,
                Data = null,
                ErrorMessage = "No data matched the provided id"
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private ApiResponse<TestObjectDto> TrySerializeTestObjectResponse(string testObjectServiceResponse)
    {
        try
        {
            var testObjectApiResponse = JsonSerializer.Deserialize<ApiResponse<TestObjectDto>>(testObjectServiceResponse);

            return testObjectApiResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Serializing test object failed in AggregationService, method 'TrySerializerTestObject', {e.Message}");
            return null;
        }
    }
    
    private ApiResponse<List<LeakTestDto>> TrySerializeLeakTestServiceResponse(string leakTestServiceResponse)
    {
        try
        {
            var leakTestApiResponse = JsonSerializer.Deserialize<ApiResponse<List<LeakTestDto>>>(leakTestServiceResponse);

            return leakTestApiResponse;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Serializing test object failed in AggregationService, method 'TrySerializerTestObject', {e.Message}");
            return null;
        }
    }

    private ApiResponse<TestObjectWithResultsDto> CreateTestObjectWithResultsDto(
        ApiResponse<List<LeakTestDto>>? leakTestApiResponse, ApiResponse<TestObjectDto>? testObjectApiResponse)
    {
        try
        {
            Console.WriteLine("Trying to create api response.");

            if (leakTestApiResponse.StatusCode != 200 && testObjectApiResponse.StatusCode != 200)
            {
                Console.WriteLine("No data matched the provided id.");
                throw new DataAggregationFailedException("No data matched the provide id");
            }

            if (leakTestApiResponse.StatusCode != 200 && testObjectApiResponse.StatusCode == 200)
            {
                Console.WriteLine("No test results matched the provided id. Creating partial response.");

                var testObjectWithResultsDto = new TestObjectWithResultsDto()
                    { TestObjectDto = testObjectApiResponse.Data };
                return new ApiResponse<TestObjectWithResultsDto>()
                {
                    StatusCode = 203,
                    Data = testObjectWithResultsDto,
                    ErrorMessage =
                        $"No test data matched the provided test object id {testObjectApiResponse.Data.Id}. Only the requested test object could be returned."
                };
            }

            if (leakTestApiResponse.StatusCode == 200 && testObjectApiResponse.StatusCode != 200)
            {
                Console.WriteLine("No test object matched the provided id. Creating partial response.");

                // Create partial response message. 
                var testObjectWithResultsDto = new TestObjectWithResultsDto
                    { LeakTestDto = leakTestApiResponse.Data };
                return new ApiResponse<TestObjectWithResultsDto>
                {
                    StatusCode = 203,
                    Data = testObjectWithResultsDto,
                    ErrorMessage =
                        $"No test object matched the provided test object id {leakTestApiResponse.Data.SingleOrDefault().LeakTestId}. Only the requested test data could be returned."
                };
            }
            else
            {
                var testObjectWithResultsDto = new TestObjectWithResultsDto
                {
                    TestObjectDto = testObjectApiResponse.Data, LeakTestDto = leakTestApiResponse.Data 
                };

                return new ApiResponse<TestObjectWithResultsDto>() { Data = testObjectWithResultsDto };
            }
        }
        catch (DataAggregationFailedException e)
        {
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public class DataAggregationFailedException : Exception
    {
        public DataAggregationFailedException()
        {
        }
        public DataAggregationFailedException(string message) : base(message)
        {
        }
        public DataAggregationFailedException(string message, Exception inner) : base(message, inner)
        {
        }
    } 
    
}