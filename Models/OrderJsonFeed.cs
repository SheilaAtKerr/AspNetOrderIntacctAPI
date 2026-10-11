using System.Text.Json.Serialization;

namespace AspNetOrderAPI.Models;

/// <summary>
/// Represents an order loaded from a JSON feed/data source.
/// This model maps external JSON order data into the Order domain model.
/// </summary>
public class OrderJsonFeed
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("orderNumber")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumberSnakeCase { get; set; }

    [JsonPropertyName("orderDate")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDateSnakeCase { get; set; }

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerNameSnakeCase { get; set; }

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    [JsonPropertyName("customer_id")]
    public string? CustomerIdSnakeCase { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal? TotalAmountSnakeCase { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItemJsonFeed>? Items { get; set; } = new();

    [JsonPropertyName("orderItems")]
    public List<OrderItemJsonFeed>? OrderItems { get; set; } = new();

    [JsonPropertyName("order_items")]
    public List<OrderItemJsonFeed>? OrderItemsSnakeCase { get; set; } = new();

    /// <summary>
    /// Converts this JSON feed model to an Order domain model.
    /// Handles multiple naming conventions (camelCase, snake_case).
    /// </summary>
    public Order ToOrder()
    {
        var order = new Order
        {
            OrderNumber = OrderNumber ?? OrderNumberSnakeCase ?? Guid.NewGuid().ToString(),
            OrderDate = ParseDate(OrderDate ?? OrderDateSnakeCase),
            CustomerName = CustomerName ?? CustomerNameSnakeCase ?? "Unknown",
            CustomerId = CustomerId ?? CustomerIdSnakeCase ?? "UNKNOWN",
            TotalAmount = TotalAmount ?? TotalAmountSnakeCase ?? 0m,
            Status = Status ?? "Pending",
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>()
        };

        var items = Items ?? OrderItems ?? OrderItemsSnakeCase ?? new List<OrderItemJsonFeed>();
        foreach (var item in items)
        {
            order.OrderItems.Add(item.ToOrderItem());
        }

        return order;
    }

    private static DateTime ParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return DateTime.UtcNow;
        }

        // Try common date formats
        var formats = new[]
        {
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "MM/dd/yyyy",
            "dd/MM/yyyy"
        };

        if (DateTime.TryParseExact(dateString, formats, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }

        // Fallback: try generic parse
        if (DateTime.TryParse(dateString, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return DateTime.UtcNow;
    }
}

/// <summary>
/// Represents an order item from a JSON feed.
/// </summary>
public class OrderItemJsonFeed
{
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("item_id")]
    public string? ItemIdSnakeCase { get; set; }

    [JsonPropertyName("itemDescription")]
    public string? ItemDescription { get; set; }

    [JsonPropertyName("item_description")]
    public string? ItemDescriptionSnakeCase { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal? UnitPrice { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal? UnitPriceSnakeCase { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal? LineTotal { get; set; }

    [JsonPropertyName("line_total")]
    public decimal? LineTotalSnakeCase { get; set; }

    /// <summary>
    /// Converts this JSON feed model to an OrderItem domain model.
    /// </summary>
    public OrderItem ToOrderItem()
    {
        var unitPrice = UnitPrice ?? UnitPriceSnakeCase ?? 0m;
        var quantity = Quantity ?? 1;
        var lineTotal = LineTotal ?? LineTotalSnakeCase ?? (unitPrice * quantity);

        return new OrderItem
        {
            ItemId = ItemId ?? ItemIdSnakeCase ?? "ITEM-001",
            ItemDescription = ItemDescription ?? ItemDescriptionSnakeCase ?? "Order Item",
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = lineTotal
        };
    }
}
