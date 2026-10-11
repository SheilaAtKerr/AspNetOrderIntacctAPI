using AspNetOrderAPI.Models;
using AspNetOrderAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetOrderAPI.Controllers;

/// <summary>
/// Controller for importing orders from JSON feeds.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrderFeedController : ControllerBase
{
    private readonly IOrderJsonFeedService _jsonFeedService;
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderFeedController> _logger;

    public OrderFeedController(
        IOrderJsonFeedService jsonFeedService,
        IOrderService orderService,
        ILogger<OrderFeedController> logger)
    {
        _jsonFeedService = jsonFeedService;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Import orders from a JSON string (posted in request body).
    /// </summary>
    /// <param name="request">Request containing JSON content and options</param>
    /// <returns>List of imported orders</returns>
    [HttpPost("import-from-json")]
    public async Task<ActionResult<OrderFeedImportResponse>> ImportFromJson([FromBody] OrderFeedImportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.JsonContent))
            {
                return BadRequest(new { message = "JSON content is required." });
            }

            // Validate JSON structure
            var isValid = await _jsonFeedService.ValidateJsonStructureAsync(request.JsonContent);
            if (!isValid)
            {
                return BadRequest(new { message = "Invalid JSON structure. Expected orders with required fields." });
            }

            // Parse JSON
            var orders = await _jsonFeedService.ParseJsonFeedAsync(request.JsonContent);

            if (orders.Count == 0)
            {
                return BadRequest(new { message = "No valid orders found in JSON." });
            }

            // Save orders to database if requested
            var savedOrders = new List<Order>();
            if (request.SaveToDatabase ?? true)
            {
                foreach (var order in orders)
                {
                    try
                    {
                        var saved = await _orderService.CreateOrderAsync(order);
                        savedOrders.Add(saved);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save order: {OrderNumber}", order.OrderNumber);
                    }
                }
            }
            else
            {
                savedOrders = orders;
            }

            var response = new OrderFeedImportResponse
            {
                TotalParsed = orders.Count,
                TotalSaved = savedOrders.Count,
                Orders = savedOrders,
                SavedToDatabase = request.SaveToDatabase ?? true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing orders from JSON.");
            return BadRequest(new { message = "Failed to import orders.", error = ex.Message });
        }
    }

    /// <summary>
    /// Import orders from a JSON file on the server.
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    /// <param name="saveToDatabase">Whether to save orders to database</param>
    /// <returns>List of imported orders</returns>
    [HttpPost("import-from-file")]
    public async Task<ActionResult<OrderFeedImportResponse>> ImportFromFile([FromQuery] string filePath, [FromQuery] bool saveToDatabase = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest(new { message = "File path is required." });
            }

            // Load from file
            var orders = await _jsonFeedService.LoadFromFileAsync(filePath);

            if (orders.Count == 0)
            {
                return BadRequest(new { message = "No valid orders found in file." });
            }

            // Save orders to database if requested
            var savedOrders = new List<Order>();
            if (saveToDatabase)
            {
                foreach (var order in orders)
                {
                    try
                    {
                        var saved = await _orderService.CreateOrderAsync(order);
                        savedOrders.Add(saved);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save order: {OrderNumber}", order.OrderNumber);
                    }
                }
            }
            else
            {
                savedOrders = orders;
            }

            var response = new OrderFeedImportResponse
            {
                TotalParsed = orders.Count,
                TotalSaved = savedOrders.Count,
                Orders = savedOrders,
                SavedToDatabase = saveToDatabase
            };

            return Ok(response);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "File not found: {FilePath}", filePath);
            return NotFound(new { message = $"File not found: {filePath}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing orders from file: {FilePath}", filePath);
            return BadRequest(new { message = "Failed to import orders from file.", error = ex.Message });
        }
    }

    /// <summary>
    /// Import orders from a URL/HTTP endpoint.
    /// </summary>
    /// <param name="url">URL to the JSON feed</param>
    /// <param name="saveToDatabase">Whether to save orders to database</param>
    /// <returns>List of imported orders</returns>
    [HttpPost("import-from-url")]
    public async Task<ActionResult<OrderFeedImportResponse>> ImportFromUrl([FromQuery] string url, [FromQuery] bool saveToDatabase = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new { message = "URL is required." });
            }

            // Load from URL
            var orders = await _jsonFeedService.LoadFromUrlAsync(url);

            if (orders.Count == 0)
            {
                return BadRequest(new { message = "No valid orders found at URL." });
            }

            // Save orders to database if requested
            var savedOrders = new List<Order>();
            if (saveToDatabase)
            {
                foreach (var order in orders)
                {
                    try
                    {
                        var saved = await _orderService.CreateOrderAsync(order);
                        savedOrders.Add(saved);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save order: {OrderNumber}", order.OrderNumber);
                    }
                }
            }
            else
            {
                savedOrders = orders;
            }

            var response = new OrderFeedImportResponse
            {
                TotalParsed = orders.Count,
                TotalSaved = savedOrders.Count,
                Orders = savedOrders,
                SavedToDatabase = saveToDatabase
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing orders from URL: {Url}", url);
            return BadRequest(new { message = "Failed to import orders from URL.", error = ex.Message });
        }
    }

    /// <summary>
    /// Validate JSON structure without importing.
    /// </summary>
    /// <param name="request">Request containing JSON content</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate-json")]
    public async Task<ActionResult<ValidateJsonResponse>> ValidateJson([FromBody] OrderFeedImportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request?.JsonContent))
            {
                return Ok(new ValidateJsonResponse { IsValid = false, Message = "JSON content is empty." });
            }

            var isValid = await _jsonFeedService.ValidateJsonStructureAsync(request.JsonContent);

            if (isValid)
            {
                var orders = await _jsonFeedService.ParseJsonFeedAsync(request.JsonContent);
                return Ok(new ValidateJsonResponse
                {
                    IsValid = true,
                    Message = "JSON is valid and contains order data.",
                    OrderCount = orders.Count
                });
            }
            else
            {
                return Ok(new ValidateJsonResponse
                {
                    IsValid = false,
                    Message = "JSON structure is invalid or missing required order fields."
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JSON.");
            return Ok(new ValidateJsonResponse
            {
                IsValid = false,
                Message = $"JSON validation failed: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Request model for importing orders from JSON.
/// </summary>
public class OrderFeedImportRequest
{
    public string? JsonContent { get; set; }
    public bool? SaveToDatabase { get; set; } = true;
}

/// <summary>
/// Response model for order import operations.
/// </summary>
public class OrderFeedImportResponse
{
    public int TotalParsed { get; set; }
    public int TotalSaved { get; set; }
    public List<Order> Orders { get; set; } = new();
    public bool SavedToDatabase { get; set; }
}

/// <summary>
/// Response model for JSON validation.
/// </summary>
public class ValidateJsonResponse
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public int? OrderCount { get; set; }
}
