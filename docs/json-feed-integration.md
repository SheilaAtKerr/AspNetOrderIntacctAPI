# JSON Feed Integration Guide

The AspNetOrderIntacctAPI now supports importing orders from JSON data feeds via multiple methods.

## Supported JSON Formats

### 1. Array of Orders (camelCase)

```json
[
  {
    "orderNumber": "ORD-001",
    "orderDate": "2024-01-15",
    "customerName": "ACME Corp",
    "customerId": "CUST-001",
    "totalAmount": 1500.50,
    "status": "Pending",
    "items": [
      {
        "itemId": "SKU-001",
        "itemDescription": "Product A",
        "quantity": 10,
        "unitPrice": 100.00,
        "lineTotal": 1000.00
      }
    ]
  }
]
```

### 2. Array of Orders (snake_case)

```json
[
  {
    "order_number": "ORD-002",
    "order_date": "2024-01-16",
    "customer_name": "Tech Inc",
    "customer_id": "CUST-002",
    "total_amount": 2500.00,
    "status": "Pending",
    "order_items": [
      {
        "item_id": "SKU-002",
        "item_description": "Service",
        "quantity": 1,
        "unit_price": 2500.00,
        "line_total": 2500.00
      }
    ]
  }
]
```

### 3. Wrapped Array

```json
{
  "orders": [
    { /* order object */ }
  ]
}
```

Or:

```json
{
  "data": [
    { /* order object */ }
  ]
}
```

### 4. Single Order Object

```json
{
  "orderNumber": "ORD-003",
  "orderDate": "2024-01-17",
  "customerName": "Global Enterprises",
  "customerId": "CUST-003",
  "totalAmount": 3200.00,
  "status": "Pending",
  "items": []
}
```

## Required Fields

### Order
- `orderNumber` (or `order_number`) - Unique order identifier
- `customerId` (or `customer_id`) - Customer reference
- `totalAmount` (or `total_amount`) - Order total
- `customerName` (or `customer_name`) - Customer name
- `orderDate` (or `order_date`) - Order date (optional, defaults to now)
- `status` - Order status (optional, defaults to "Pending")
- `items` (or `orderItems` or `order_items`) - Array of line items

### OrderItem
- `itemId` (or `item_id`) - Item SKU/ID
- `itemDescription` (or `item_description`) - Item description
- `quantity` - Quantity ordered
- `unitPrice` (or `unit_price`) - Price per unit
- `lineTotal` (or `line_total`) - Line total (auto-calculated if missing)

## API Endpoints

### 1. Import from JSON String (POST)

```bash
POST /api/orderfeed/import-from-json
Content-Type: application/json

{
  "jsonContent": "[{...}]",
  "saveToDatabase": true
}
```

**Response:**

```json
{
  "totalParsed": 2,
  "totalSaved": 2,
  "orders": [
    { /* order object */ },
    { /* order object */ }
  ],
  "savedToDatabase": true
}
```

### 2. Import from File (POST)

```bash
POST /api/orderfeed/import-from-file?filePath=/path/to/orders.json&saveToDatabase=true
```

### 3. Import from URL (POST)

```bash
POST /api/orderfeed/import-from-url?url=https://api.example.com/orders&saveToDatabase=true
```

### 4. Validate JSON (POST)

```bash
POST /api/orderfeed/validate-json
Content-Type: application/json

{
  "jsonContent": "[{...}]"
}
```

**Response:**

```json
{
  "isValid": true,
  "message": "JSON is valid and contains order data.",
  "orderCount": 5
}
```

## Date Format Support

The parser supports multiple date formats:

- `yyyy-MM-dd` (2024-01-15)
- `yyyy-MM-ddTHH:mm:ss` (2024-01-15T10:30:00)
- `yyyy-MM-ddTHH:mm:ssZ` (2024-01-15T10:30:00Z)
- `yyyy-MM-ddTHH:mm:ss.fffZ` (2024-01-15T10:30:00.000Z)
- `MM/dd/yyyy` (01/15/2024)
- `dd/MM/yyyy` (15/01/2024)

If parsing fails, the current UTC time is used as the default.

## Sample JSON Files

Sample JSON files are provided in the `samples/` directory:

- `orders-sample.json` - camelCase format
- `orders-snake-case.json` - snake_case format
- `orders-wrapped.json` - Wrapped array format

## Usage Examples

### Using cURL

**Import from JSON string:**

```bash
curl -X POST https://localhost:5001/api/orderfeed/import-from-json \
  -H "Content-Type: application/json" \
  -d '{"jsonContent":"[{\"orderNumber\":\"ORD-001\",\"customerId\":\"CUST-001\",\"totalAmount\":100.00}]"}'
```

**Import from file:**

```bash
curl -X POST 'https://localhost:5001/api/orderfeed/import-from-file?filePath=samples/orders-sample.json&saveToDatabase=true'
```

**Import from URL:**

```bash
curl -X POST 'https://localhost:5001/api/orderfeed/import-from-url?url=https://api.example.com/orders&saveToDatabase=true'
```

**Validate JSON:**

```bash
curl -X POST https://localhost:5001/api/orderfeed/validate-json \
  -H "Content-Type: application/json" \
  -d '{"jsonContent":"[{\"orderNumber\":\"ORD-001\"}]"}'  
```

### Using PowerShell

```powershell
$json = @'
[
  {
    "orderNumber": "ORD-001",
    "customerId": "CUST-001",
    "totalAmount": 1000.00,
    "customerName": "Test Company"
  }
]
'@

$body = @{
  jsonContent = $json
  saveToDatabase = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri 'https://localhost:5001/api/orderfeed/import-from-json' \
  -Method Post \
  -ContentType 'application/json' \
  -Body $body
```

## Error Handling

The API returns descriptive error messages:

- **Invalid JSON**: "Invalid JSON format: ..."
- **Missing required fields**: "Invalid JSON structure. Expected orders with required fields."
- **File not found**: "File not found: /path/to/file.json"
- **HTTP error**: "Failed to fetch JSON feed from [URL]"

## Workflow

1. **Validate**: Use the `/validate-json` endpoint to validate JSON before importing
2. **Import**: Use one of the import endpoints to load orders
3. **Preview**: Set `saveToDatabase=false` to preview orders without saving
4. **Save**: Set `saveToDatabase=true` to persist orders to PostgreSQL
5. **Sync**: Use existing `/api/orders/sync-to-intacct` endpoints to sync orders to Sage Intacct

## Best Practices

1. **Validate before importing** - Always validate JSON structure first
2. **Test with sample data** - Use provided sample files to test the integration
3. **Handle missing fields** - The parser provides sensible defaults (e.g., status defaults to "Pending")
4. **Use correct naming** - Supports both camelCase and snake_case field names
5. **Check error responses** - Review error messages for field naming or format issues
6. **Monitor sync status** - Check ProcessLog entries to confirm Intacct sync success

## Troubleshooting

### JSON Parse Error

**Problem**: "Invalid JSON format"

**Solution**: Validate JSON syntax using a JSON validator (e.g., jsonlint.com)

### Missing Required Fields

**Problem**: "Invalid JSON structure. Expected orders..."

**Solution**: Ensure orders have at least `orderNumber` and one of `customerId` or `totalAmount`

### Field Names Not Recognized

**Problem**: Orders are parsed but fields are empty

**Solution**: Check that field names match supported naming conventions (camelCase or snake_case)

### Date Parsing Fails

**Problem**: Order dates are set to current time instead of the provided date

**Solution**: Ensure dates are in one of the supported formats listed above

## Migration from Other Systems

To migrate orders from another system:

1. Export orders from source system as JSON
2. Map source fields to the required schema (see Required Fields section)
3. Use `/validate-json` to verify structure
4. Use `/import-from-json` or `/import-from-file` to load orders
5. Verify orders appear in PostgreSQL
6. Begin syncing to Intacct using standard sync endpoints

## Performance Considerations

- **Batch size**: The API will parse any number of orders, but very large batches (>1000) may take time
- **Database inserts**: Each order is inserted individually; use database bulk insert if needed
- **URL fetches**: HTTP timeouts are set to 30 seconds; ensure external feeds respond quickly
- **Validation**: Validate JSON structure before importing large batches to avoid repeated parse errors
