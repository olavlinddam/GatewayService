using System.Text;
using System.Text.Json;
using GatewayService.Models.DTOs;
using GatewayService.Services.RabbitMq;
using Microsoft.Extensions.Primitives;

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
        
        ApiResponse<TestObjectDto> testObjectApiResponse = null;
        ApiResponse<List<LeakTestDto>> leakTestApiResponse = null;

        try
        {
            Console.WriteLine("Fetching test object.");
            // Fetch TestObject
            var testObjectServiceResponse =
                await _testObjectProducer.SendMessage(id, testObjQueueName, testObjRoutingKey);
            testObjectApiResponse = TrySerializeTestObjectResponse(testObjectServiceResponse);
        }
        catch (TimeoutException e)
        {
            Console.WriteLine($"Error fetching test object: {e.Message}");
            testObjectApiResponse = new ApiResponse<TestObjectDto>()
            {
                StatusCode = 408,
                ErrorMessage = "Test object service is currently unavailable. Please try again later."
            };
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching test object: {e.Message}");
            throw;
        }

        try
        {
            Console.WriteLine("Fetching test results");
            // Fetch LeakTests
            const string
                key = "TestObjectId"; // LeakTestService needs to know what LeakTest attribute it should retrieve data for. 
            var value = id; // LeakTestService needs to know what the value of the key is. 
            
            var leakTestServiceResponse = await _leakTestProducer.SendMessage($"{key};{value}", leakTestQueueName, leakTestRoutingKey);
            leakTestApiResponse = TrySerializeLeakTestServiceResponse(leakTestServiceResponse);
        }
        catch (TimeoutException e)
        {
            Console.WriteLine($"Error fetching test results: haloooouu");
            leakTestApiResponse = new ApiResponse<List<LeakTestDto>>()
            {
                StatusCode = 408,
                ErrorMessage = "Test object service is currently unavailable. Please try again later."
            };
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching test results: {e.Message}");
            throw;
        }
        try
        {
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
                ErrorMessage = e.Message
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
                var combinedErrorMessage = new StringBuilder();
                combinedErrorMessage = combinedErrorMessage.Append(testObjectApiResponse.ErrorMessage)
                    .Append(leakTestApiResponse.ErrorMessage);
                Console.WriteLine($"None of the services return 200 OK. TestObjectService returned: {testObjectApiResponse.StatusCode} with message: {testObjectApiResponse.ErrorMessage}. " +
                                  $"LeakTestService returned: {leakTestApiResponse.StatusCode} with message: {leakTestApiResponse.ErrorMessage}.");
                throw new DataAggregationFailedException($"Could not fetch message due to the follow error(s): " +
                                                         $"{combinedErrorMessage}");
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
                        $"No test data matched the provided test object id {testObjectApiResponse.Data.Id}. " +
                        $"Only the requested test object could be returned. {leakTestApiResponse.ErrorMessage}"
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
                        $"No test object matched the provided test object id {leakTestApiResponse.Data.SingleOrDefault().LeakTestId}. " +
                        $"Only the requested test data could be returned. {testObjectApiResponse.ErrorMessage}"
                };
            }
            else
            {
                var testObjectWithResultsDto = new TestObjectWithResultsDto
                {
                    TestObjectDto = testObjectApiResponse.Data, 
                    LeakTestDto = leakTestApiResponse.Data
                };

                return new ApiResponse<TestObjectWithResultsDto>() { Data = testObjectWithResultsDto ,StatusCode = 200};
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