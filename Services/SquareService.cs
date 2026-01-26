using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneManVan.Services;

/// <summary>
/// Service for processing payments through Square.
/// Uses Square's REST API directly for simplicity.
/// </summary>
public class SquareService
{
    private readonly string _settingsPath;
    private SquareSettings _settings;
    private readonly HttpClient _httpClient;
    
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SquareService()
    {
        var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "OneManVan");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "square_settings.json");
        _settings = LoadSettings();
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Gets the current Square settings.
    /// </summary>
    public SquareSettings Settings => _settings;

    /// <summary>
    /// Whether Square is properly configured and enabled.
    /// </summary>
    public bool IsAvailable => _settings.IsEnabled && _settings.IsConfigured;

    private string BaseUrl => _settings.UseSandbox 
        ? "https://connect.squareupsandbox.com/v2" 
        : "https://connect.squareup.com/v2";

    /// <summary>
    /// Loads Square settings from disk.
    /// </summary>
    private SquareSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<SquareSettings>(json) ?? new SquareSettings();
            }
        }
        catch
        {
            // Return default settings on error
        }
        return new SquareSettings();
    }

    /// <summary>
    /// Saves Square settings to disk.
    /// </summary>
    public void SaveSettings(SquareSettings settings)
    {
        _settings = settings;
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("Square-Version", "2024-01-18");
    }

    /// <summary>
    /// Tests the Square connection by retrieving location info.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync()
    {
        if (!_settings.IsConfigured)
        {
            return (false, "Square settings are not configured");
        }

        try
        {
            ConfigureHttpClient();
            var response = await _httpClient.GetAsync($"{BaseUrl}/locations/{_settings.LocationId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var locationResponse = JsonSerializer.Deserialize<LocationResponse>(content);
                var locationName = locationResponse?.Location?.Name ?? "Unknown";
                return (true, $"Connected to: {locationName}");
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"API Error ({response.StatusCode}): {GetErrorMessage(errorContent)}");
        }
        catch (Exception ex)
        {
            return (false, $"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a payment using a card nonce from Square Web Payments SDK.
    /// </summary>
    public async Task<SquarePaymentResult> CreatePaymentAsync(
        long amountCents, 
        string sourceId, 
        string referenceId,
        string? note = null)
    {
        if (!IsAvailable)
        {
            return new SquarePaymentResult 
            { 
                Success = false, 
                ErrorMessage = "Square is not configured or enabled" 
            };
        }

        try
        {
            ConfigureHttpClient();

            var request = new
            {
                source_id = sourceId,
                idempotency_key = Guid.NewGuid().ToString(),
                amount_money = new
                {
                    amount = amountCents,
                    currency = "USD"
                },
                location_id = _settings.LocationId,
                reference_id = referenceId,
                autocomplete = true,
                note = note
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{BaseUrl}/payments", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(responseContent);
                var payment = paymentResponse?.Payment;

                return new SquarePaymentResult
                {
                    Success = true,
                    TransactionId = payment?.Id,
                    ReceiptUrl = payment?.ReceiptUrl,
                    CardBrand = payment?.CardDetails?.Card?.CardBrand,
                    Last4 = payment?.CardDetails?.Card?.Last4
                };
            }

            return new SquarePaymentResult
            {
                Success = false,
                ErrorMessage = $"Payment failed: {GetErrorMessage(responseContent)}"
            };
        }
        catch (Exception ex)
        {
            return new SquarePaymentResult
            {
                Success = false,
                ErrorMessage = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets available locations for the account.
    /// </summary>
    public async Task<List<SquareLocation>> GetLocationsAsync()
    {
        if (!_settings.IsConfigured)
        {
            return [];
        }

        try
        {
            ConfigureHttpClient();
            var response = await _httpClient.GetAsync($"{BaseUrl}/locations");
            
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var content = await response.Content.ReadAsStringAsync();
            var locationsResponse = JsonSerializer.Deserialize<LocationsResponse>(content);
            
            return locationsResponse?.Locations?
                .Where(l => l.Status == "ACTIVE")
                .Select(l => new SquareLocation
                {
                    Id = l.Id ?? string.Empty,
                    Name = l.Name ?? "Unknown",
                    Address = FormatAddress(l.Address)
                })
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string FormatAddress(AddressDto? address)
    {
        if (address == null) return "No address";
        return $"{address.AddressLine1}, {address.Locality}, {address.AdministrativeDistrictLevel1}";
    }

    private static string GetErrorMessage(string jsonResponse)
    {
        try
        {
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(jsonResponse);
            if (errorResponse?.Errors != null && errorResponse.Errors.Count > 0)
            {
                return string.Join("; ", errorResponse.Errors.Select(e => e.Detail ?? e.Code ?? "Unknown error"));
            }
        }
        catch { }
        return "Unknown error";
    }
}

#region API Response DTOs

internal class LocationResponse
{
    [JsonPropertyName("location")]
    public LocationDto? Location { get; set; }
}

internal class LocationsResponse
{
    [JsonPropertyName("locations")]
    public List<LocationDto>? Locations { get; set; }
}

internal class LocationDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("address")]
    public AddressDto? Address { get; set; }
}

internal class AddressDto
{
    [JsonPropertyName("address_line_1")]
    public string? AddressLine1 { get; set; }
    
    [JsonPropertyName("locality")]
    public string? Locality { get; set; }
    
    [JsonPropertyName("administrative_district_level_1")]
    public string? AdministrativeDistrictLevel1 { get; set; }
}

internal class PaymentResponse
{
    [JsonPropertyName("payment")]
    public PaymentDto? Payment { get; set; }
}

internal class PaymentDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("receipt_url")]
    public string? ReceiptUrl { get; set; }
    
    [JsonPropertyName("card_details")]
    public CardDetailsDto? CardDetails { get; set; }
}

internal class CardDetailsDto
{
    [JsonPropertyName("card")]
    public CardDto? Card { get; set; }
}

internal class CardDto
{
    [JsonPropertyName("card_brand")]
    public string? CardBrand { get; set; }
    
    [JsonPropertyName("last_4")]
    public string? Last4 { get; set; }
}

internal class ErrorResponse
{
    [JsonPropertyName("errors")]
    public List<ErrorDto>? Errors { get; set; }
}

internal class ErrorDto
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

#endregion

/// <summary>
/// Result of a Square payment operation.
/// </summary>
public class SquarePaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? CardBrand { get; set; }
    public string? Last4 { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Represents a Square business location.
/// </summary>
public class SquareLocation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
