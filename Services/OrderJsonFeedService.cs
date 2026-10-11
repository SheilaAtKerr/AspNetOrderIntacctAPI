using System.Text.Json;
using AspNetOrderAPI.Models;

namespace AspNetOrderAPI.Services;

public class OrderJsonFeedService : IOrderJsonFeedService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderJsonFeedService> _logger;

    public OrderJsonFeedService(
        HttpClient httpClient,
        ILogger<OrderJsonFeedService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Parse a JSON string into a list of orders.
    /// Supports both single order and array of orders.
    /// </summary>
    public async Task<List<Order>> ParseJsonFeedAsync(string jsonContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Empty JSON content provided.");
                return new List<Order>();
            }

            var orders = new List<Order>();

            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Handle array of orders
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    var order = ParseOrderFromJson(element);
                    if (order != null)
                    {
                        orders.Add(order);
                    }
                }
            }
            // Handle single order object
            else if (root.ValueKind == JsonValueKind.Object)
            {
                var order = ParseOrderFromJson(root);
                if (order != null)
                {
                    orders.Add(order);
                }
            }
            // Handle wrapped array (e.g., { "orders": [...] })
            else if (root.TryGetProperty("orders", out var ordersArray))
            {
                foreach (var element in ordersArray.EnumerateArray())
                {
                    var order = ParseOrderFromJson(element);
                    if (order != null)
                    {
                        orders.Add(order);
                    }
                }
            }
            else if (root.TryGetProperty("data", out var dataArray))
            {
                foreach (var element in dataArray.EnumerateArray())
                {
                    var order = ParseOrderFromJson(element);
                    if (order != null)
                    {
                        orders.Add(order);
                    }
                }
            }

            _logger.LogInformation("Parsed {Count} orders from JSON feed.", orders.Count);
            return orders;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON content.");
            throw new InvalidOperationException($"Invalid JSON format: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON feed.");
            throw;
        }
    }

    /// <summary>
    /// Load orders from a JSON file.
    /// </summary>
    public async Task<List<Order>> LoadFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            _logger.LogInformation("Loaded JSON file from: {FilePath}", filePath);

            return await ParseJsonFeedAsync(jsonContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON from file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Load orders from a URL/HTTP endpoint.
    /// </summary>
    public async Task<List<Order>> LoadFromUrlAsync(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL cannot be empty.", nameof(url));
            }

            _logger.LogInformation("Loading JSON feed from URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successfully fetched JSON feed from: {Url}", url);

            return await ParseJsonFeedAsync(jsonContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error loading JSON from URL: {Url}", url);
            throw new InvalidOperationException($"Failed to fetch JSON feed from {url}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON from URL: {Url}", url);
            throw;
        }
    }

    /// <summary>
    /// Validate JSON structure before parsing.
    /// </summary>
    public async Task<bool> ValidateJsonStructureAsync(string jsonContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return false;
            }

            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            // Check if it's a valid order structure
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                return HasOrderFields(root[0]);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Check for orders array
                if (root.TryGetProperty("orders", out var ordersArray) && ordersArray.ValueKind == JsonValueKind.Array)
                {
                    return ordersArray.GetArrayLength() > 0 && HasOrderFields(ordersArray[0]);
                }
                // Check for data array
                if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    return dataArray.GetArrayLength() > 0 && HasOrderFields(dataArray[0]);
                }
                // Check if root is an order
                return HasOrderFields(root);
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private Order? ParseOrderFromJson(JsonElement element)
    {
        try
        {
            var json = element.GetRawText();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var feed = JsonSerializer.Deserialize<OrderJsonFeed>(json, options);
            return feed?.ToOrder();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse order element from JSON.");
            return null;
        }
    }

    private bool HasOrderFields(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        // Check for at least one of the key order fields
        var hasOrderNumber = element.TryGetProperty("orderNumber", out _) || element.TryGetProperty("order_number", out _);
        var hasCustomerId = element.TryGetProperty("customerId", out _) || element.TryGetProperty("customer_id", out _);
        var hasTotalAmount = element.TryGetProperty("totalAmount", out _) || element.TryGetProperty("total_amount", out _);

        return hasOrderNumber || (hasCustomerId && hasTotalAmount);
    }
}
