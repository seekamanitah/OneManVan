using OneManVan.Mobile.Pages;

namespace OneManVan.Mobile.Services;

/// <summary>
/// Service for barcode and QR code scanning using device camera.
/// Provides integration with camera-based scanning.
/// </summary>
public interface IBarcodeScannerService
{
    /// <summary>
    /// Scans a barcode or QR code using the device camera.
    /// </summary>
    /// <param name="title">Title for the scanner UI.</param>
    /// <returns>The scanned barcode result, or null if cancelled.</returns>
    Task<BarcodeScanResult?> ScanAsync(string title = "Scan Barcode");

    /// <summary>
    /// Scans a barcode using a simple prompt (fallback mode).
    /// </summary>
    Task<string?> ScanSimpleAsync(string title = "Scan Barcode");

    /// <summary>
    /// Checks if barcode scanning is supported on this device.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Checks if camera is available for scanning.
    /// </summary>
    bool IsCameraAvailable { get; }

    /// <summary>
    /// Gets or sets whether to use full-screen scanner.
    /// </summary>
    bool UseFullScreenScanner { get; set; }

    /// <summary>
    /// Event raised when a barcode is scanned.
    /// </summary>
    event EventHandler<BarcodeScanResult>? BarcodeScanned;

    /// <summary>
    /// Gets scan history.
    /// </summary>
    IReadOnlyList<BarcodeScanResult> ScanHistory { get; }

    /// <summary>
    /// Clears scan history.
    /// </summary>
    void ClearHistory();
}

/// <summary>
/// Enhanced barcode scanner implementation with camera support.
/// </summary>
public class BarcodeScannerService : IBarcodeScannerService
{
    private readonly List<BarcodeScanResult> _scanHistory = [];
    private const int MaxHistorySize = 50;

    public bool IsSupported => true;
    public bool IsCameraAvailable => MediaPicker.Default.IsCaptureSupported;
    public bool UseFullScreenScanner { get; set; } = true;

    public event EventHandler<BarcodeScanResult>? BarcodeScanned;

    public IReadOnlyList<BarcodeScanResult> ScanHistory => _scanHistory.AsReadOnly();

    public async Task<BarcodeScanResult?> ScanAsync(string title = "Scan Barcode")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return null;

        if (UseFullScreenScanner)
        {
            // Use full-screen scanner page
            var scannerPage = new BarcodeScannerPage(this);
            scannerPage.Configure(title);

            var resultTask = scannerPage.GetScanResultAsync();
            await page.Navigation.PushModalAsync(scannerPage);

            var result = await resultTask;

            if (result != null)
            {
                AddToHistory(result);
                BarcodeScanned?.Invoke(this, result);
            }

            return result;
        }
        else
        {
            // Fallback to simple prompt
            var value = await ScanSimpleAsync(title);
            if (value == null) return null;

            var result = new BarcodeScanResult
            {
                Value = value,
                Format = "Manual",
                ScannedAt = DateTime.Now
            };

            AddToHistory(result);
            BarcodeScanned?.Invoke(this, result);

            return result;
        }
    }

    public async Task<string?> ScanSimpleAsync(string title = "Scan Barcode")
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return null;

        // Show action sheet for scan method
        var action = await page.DisplayActionSheetAsync(
            title,
            "Cancel",
            null,
            "Camera Scan",
            "Enter Manually",
            "Paste from Clipboard");

        switch (action)
        {
            case "Camera Scan":
                return await CameraScanFallbackAsync();

            case "Enter Manually":
                return await page.DisplayPromptAsync(
                    "Enter Barcode",
                    "Type or paste the barcode value:",
                    "OK",
                    "Cancel",
                    keyboard: Keyboard.Default);

            case "Paste from Clipboard":
                try
                {
                    var clipboardText = await Clipboard.Default.GetTextAsync();
                    if (!string.IsNullOrWhiteSpace(clipboardText))
                    {
                        return clipboardText.Trim();
                    }
                    await page.DisplayAlertAsync("Empty", "Clipboard is empty", "OK");
                }
                catch
                {
                    await page.DisplayAlertAsync("Error", "Could not read clipboard", "OK");
                }
                return null;

            default:
                return null;
        }
    }

    private async Task<string?> CameraScanFallbackAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return null;

        // If camera not available, fall back to manual entry
        if (!IsCameraAvailable)
        {
            await page.DisplayAlertAsync("Camera Not Available",
                "Camera scanning is not available on this device. Please enter the barcode manually.",
                "OK");

            return await page.DisplayPromptAsync(
                "Enter Barcode",
                "Type the barcode value:",
                "OK",
                "Cancel",
                keyboard: Keyboard.Default);
        }

        // Take a photo of the barcode
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
            {
                Title = "Photograph Barcode"
            });

            if (photo != null)
            {
                // In a full implementation, we would decode the barcode from the image
                // For now, prompt for the value after taking the photo
                await page.DisplayAlertAsync("Photo Captured",
                    "Barcode photo captured. Please enter the value you see:",
                    "OK");

                return await page.DisplayPromptAsync(
                    "Enter Barcode Value",
                    "Enter the barcode value from the photo:",
                    "OK",
                    "Cancel",
                    keyboard: Keyboard.Default);
            }
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync("Error", $"Camera error: {ex.Message}", "OK");
        }

        return null;
    }

    private void AddToHistory(BarcodeScanResult result)
    {
        _scanHistory.Insert(0, result);

        // Trim history to max size
        while (_scanHistory.Count > MaxHistorySize)
        {
            _scanHistory.RemoveAt(_scanHistory.Count - 1);
        }
    }

    public void ClearHistory()
    {
        _scanHistory.Clear();
    }
}

/// <summary>
/// Barcode scan result with metadata.
/// </summary>
public class BarcodeScanResult
{
    /// <summary>
    /// The decoded barcode/QR code value.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    /// The format of the barcode (QR, EAN13, Code128, etc.).
    /// </summary>
    public string Format { get; set; } = "Unknown";

    /// <summary>
    /// Timestamp when the scan occurred.
    /// </summary>
    public DateTime ScannedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// GPS location where scan occurred (if available).
    /// </summary>
    public Location? Location { get; set; }

    /// <summary>
    /// Additional metadata about the scan.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Gets a display-friendly format name.
    /// </summary>
    public string FormatDisplay => Format switch
    {
        "QR_CODE" => "QR Code",
        "EAN_13" => "EAN-13",
        "EAN_8" => "EAN-8",
        "UPC_A" => "UPC-A",
        "UPC_E" => "UPC-E",
        "CODE_128" => "Code 128",
        "CODE_39" => "Code 39",
        "CODE_93" => "Code 93",
        "DATA_MATRIX" => "Data Matrix",
        "PDF_417" => "PDF417",
        "ITF" => "ITF",
        "CODABAR" => "Codabar",
        _ => Format
    };

    public override string ToString() => $"{Value} ({FormatDisplay})";
}
