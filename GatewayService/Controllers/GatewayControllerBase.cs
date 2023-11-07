using GatewayService.Models.ErrorModels;
using Microsoft.AspNetCore.Mvc;

namespace GatewayService.Controllers;

public class GatewayControllerBase : ControllerBase
{
    protected IActionResult BadRequestWithDetails(string message, string? item = null, string? id = null)
    {
        var statusCode = StatusCodes.Status400BadRequest;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Name = "BadRequest",
                Message = message,
                StatusCode = statusCode,
                Item = item,
                Id = id
            }
        };
        return StatusCode(statusCode, errorResponse);
    }
    
    protected IActionResult NotFoundWithDetails(string? message, string? item = null, string? id = null)
    {
        var statusCode = StatusCodes.Status404NotFound;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Name = "NotFound",
                Message = message,
                StatusCode = statusCode,
                Item = item,
                Id = id
            }
        };
        return StatusCode(statusCode, errorResponse);
    }
    
    protected IActionResult TimedOutRequestWithDetails(string message, string? item = null, string? id = null)
    {
        var statusCode = StatusCodes.Status503ServiceUnavailable;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Name = "TimedOutRequest",
                Message = message,
                StatusCode = statusCode,
                Item = item,
                Id = id
            }
        };
        return StatusCode(statusCode, errorResponse);
    }
    
    protected IActionResult RabbitMqErrorWithDetails(string message, string? item = null, string? id = null)
    {
        var statusCode = StatusCodes.Status503ServiceUnavailable;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Name = "Service could not be reached.",
                Message = message,
                StatusCode = statusCode,
                Item = item,
                Id = id
            }
        };
        return StatusCode(statusCode, errorResponse);
    }
    protected IActionResult BrokenCircuitErrorWithDetails(string message, string? item = null, string? id = null)
    {
        var statusCode = StatusCodes.Status503ServiceUnavailable;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetails
            {
                Name = "Service unavailable.",
                Message = message,
                StatusCode = statusCode,
                Item = item,
                Id = id
            }
        };
        return StatusCode(statusCode, errorResponse);
    }
}
