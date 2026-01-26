using OneManVan.Mobile.Services;
using ZXing.Net.Maui;
using ZxingBarcodeFormat = ZXing.Net.Maui.BarcodeFormat;

namespace OneManVan.Mobile.Pages;

/// <summary>
/// Full-screen barcode scanner page with ZXing camera integration.
/// </summary>
public partial class BarcodeScannerPage : ContentPage
{
    private readonly IBarcodeScannerService _scannerService;
    private string? _lastScannedValue;
    private bool _isFlashOn;
    private bool _isProcessing;
    private TaskCompletionSource<BarcodeScanResult?>? _scanCompletionSource;

    public bool IsTorchOn { get; set; }

    public BarcodeScannerPage(IBarcodeScannerService scannerService)
    {
        InitializeComponent();
        _scannerService = scannerService;
        BindingContext = this;
    }

    public BarcodeScannerPage() : this(new BarcodeScannerService())
    {
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isProcessing = false;
        BarcodeReader.IsDetecting = true;
        StartScanAnimation();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
        StopScanAnimation();
    }

    /// <summary>
    /// Configures the scanner with a custom title.
    /// </summary>
    public void Configure(string title, string subtitle = "Point camera at barcode")
    {
        TitleLabel.Text = title;
        SubtitleLabel.Text = subtitle;
    }

    /// <summary>
    /// Sets up for async result return.
    /// </summary>
    public Task<BarcodeScanResult?> GetScanResultAsync()
    {
        _scanCompletionSource = new TaskCompletionSource<BarcodeScanResult?>();
        return _scanCompletionSource.Task;
    }

    /// <summary>
    /// Called when ZXing detects barcodes.
    /// </summary>
    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;

        var result = e.Results?.FirstOrDefault();
        if (result == null) return;

        _isProcessing = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Stop detecting while processing
            BarcodeReader.IsDetecting = false;

            var format = result.Format.ToString();
            var value = result.Value;

            await ProcessScanResult(value, format);
        });
    }

    private void StartScanAnimation()
    {
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
        {
            if (!IsVisible) return;
            AnimateScanLine();
        });
    }

    private async void AnimateScanLine()
    {
        while (IsVisible)
        {
            await ScanLine.TranslateToAsync(0, 180, 2000, Easing.SinInOut);
            await ScanLine.TranslateToAsync(0, 0, 2000, Easing.SinInOut);
        }
    }

    private void StopScanAnimation()
    {
        ScanLine.CancelAnimations();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        _scanCompletionSource?.TrySetResult(null);
        Navigation.PopModalAsync();
    }

    private void OnFlashToggle(object sender, EventArgs e)
    {
        _isFlashOn = !_isFlashOn;
        IsTorchOn = _isFlashOn;
        OnPropertyChanged(nameof(IsTorchOn));

        BarcodeReader.IsTorchOn = _isFlashOn;

        FlashButton.Text = _isFlashOn ? "ON" : "Flash";
        FlashButton.TextColor = _isFlashOn ? Colors.Yellow : Colors.White;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
    }

    private void OnFormatSelected(object sender, EventArgs e)
    {
        // Reset all buttons
        FormatAllBtn.BackgroundColor = Color.FromArgb("#444");
        FormatQRBtn.BackgroundColor = Color.FromArgb("#444");
        Format1DBtn.BackgroundColor = Color.FromArgb("#444");

        // Highlight selected and update reader options
        if (sender is Button btn)
        {
            btn.BackgroundColor = Color.FromArgb("#4CAF50");
            
            BarcodeReader.Options = btn.Text switch
            {
                "QR Code" => new BarcodeReaderOptions 
                { 
                    Formats = ZxingBarcodeFormat.QrCode,
                    AutoRotate = true,
                    TryHarder = true
                },
                "1D Barcode" => new BarcodeReaderOptions 
                { 
                    Formats = ZxingBarcodeFormat.Ean13 | ZxingBarcodeFormat.Ean8 | ZxingBarcodeFormat.Code128 | ZxingBarcodeFormat.Code39 | ZxingBarcodeFormat.UpcA | ZxingBarcodeFormat.UpcE,
                    AutoRotate = true,
                    TryHarder = true
                },
                _ => new BarcodeReaderOptions 
                { 
                    // Use combination of all common formats since there's no "All" value
                    Formats = ZxingBarcodeFormat.QrCode | ZxingBarcodeFormat.Ean13 | ZxingBarcodeFormat.Ean8 | 
                              ZxingBarcodeFormat.Code128 | ZxingBarcodeFormat.Code39 | ZxingBarcodeFormat.UpcA | 
                              ZxingBarcodeFormat.UpcE | ZxingBarcodeFormat.DataMatrix | ZxingBarcodeFormat.Aztec,
                    AutoRotate = true,
                    TryHarder = true
                }
            };
        }
    }

    private async void OnManualEntryCompleted(object sender, EventArgs e)
    {
        await ProcessManualEntry();
    }

    private async void OnManualSubmitClicked(object sender, EventArgs e)
    {
        await ProcessManualEntry();
    }

    private async Task ProcessManualEntry()
    {
        var value = ManualEntry.Text?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            await DisplayAlertAsync("Error", "Please enter a barcode value", "OK");
            return;
        }

        await ProcessScanResult(value, "Manual Entry");
    }

    private void OnUseLastScanClicked(object sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_lastScannedValue))
        {
            _ = ProcessScanResult(_lastScannedValue, "Recent Scan");
        }
    }

    /// <summary>
    /// Called when a barcode is detected (by camera or manual entry).
    /// </summary>
    public async Task ProcessScanResult(string value, string format)
    {
        // Haptic feedback
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        // Update recent scan display
        _lastScannedValue = value;
        LastScanLabel.Text = value;
        RecentScansFrame.IsVisible = true;

        // Get GPS location if available
        Location? location = null;
        try
        {
            var locationRequest = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            location = await Geolocation.Default.GetLocationAsync(locationRequest);
        }
        catch { /* Location not available */ }

        var result = new BarcodeScanResult
        {
            Value = value,
            Format = format,
            ScannedAt = DateTime.Now,
            Location = location
        };

        // Complete the task if waiting
        if (_scanCompletionSource != null)
        {
            _scanCompletionSource.TrySetResult(result);
            await Navigation.PopModalAsync();
        }
        else
        {
            // Re-enable detection for next scan
            _isProcessing = false;
            BarcodeReader.IsDetecting = true;
        }
    }

    /// <summary>
    /// Simulates a camera scan detection.
    /// In production, this would be called from camera preview callbacks.
    /// </summary>
    public void OnBarcodeDetected(string value, string format)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await ProcessScanResult(value, format);
        });
    }
}

/// <summary>
/// Barcode format filter options.
/// </summary>
public enum BarcodeFormat
{
    All,
    QRCode,
    OneDimensional,  // EAN, UPC, Code128, Code39, etc.
    DataMatrix
}
