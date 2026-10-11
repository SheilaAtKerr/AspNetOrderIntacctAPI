namespace AspNetOrderAPI.Services;

public interface IOrderJsonFeedService
{
    /// <summary>
    /// Parse a JSON string into a list of orders.
    /// </summary>
    Task<List<Models.Order>> ParseJsonFeedAsync(string jsonContent);

    /// <summary>
    /// Load orders from a JSON file.
    /// </summary>
    Task<List<Models.Order>> LoadFromFileAsync(string filePath);

    /// <summary>
    /// Load orders from a URL/HTTP endpoint.
    /// </summary>
    Task<List<Models.Order>> LoadFromUrlAsync(string url);

    /// <summary>
    /// Validate JSON structure before parsing.
    /// </summary>
    Task<bool> ValidateJsonStructureAsync(string jsonContent);
}
