using GatewayService.Models.DTOs;

namespace GatewayService.Services.Aggregation;

public interface IAggregationService
{
    public Task<ApiResponse<TestObjectWithResultsDto>> GetTestObjectWithResults(Guid id);
}