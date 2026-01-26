using Microsoft.Maui.Graphics;

namespace OneManVan.Mobile.Controls;

/// <summary>
/// Signature capture control using MAUI GraphicsView.
/// </summary>
public class SignaturePadDrawable : IDrawable
{
    private readonly List<List<PointF>> _lines = [];
    private List<PointF> _currentLine = [];
    
    public Color StrokeColor { get; set; } = Colors.Black;
    public float StrokeWidth { get; set; } = 3f;
    public bool HasSignature => _lines.Count > 0 || _currentLine.Count > 0;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = StrokeColor;
        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        // Draw completed lines
        foreach (var line in _lines)
        {
            DrawLine(canvas, line);
        }

        // Draw current line
        DrawLine(canvas, _currentLine);
    }

    private void DrawLine(ICanvas canvas, List<PointF> points)
    {
        if (points.Count < 2) return;

        var path = new PathF();
        path.MoveTo(points[0]);
        
        for (int i = 1; i < points.Count; i++)
        {
            path.LineTo(points[i]);
        }

        canvas.DrawPath(path);
    }

    public void StartLine(PointF point)
    {
        _currentLine = [point];
    }

    public void AddPoint(PointF point)
    {
        _currentLine.Add(point);
    }

    public void EndLine()
    {
        if (_currentLine.Count > 0)
        {
            _lines.Add(_currentLine);
            _currentLine = [];
        }
    }

    public void Clear()
    {
        _lines.Clear();
        _currentLine.Clear();
    }

    public byte[]? GetSignatureImage(int width, int height)
    {
        if (!HasSignature) return null;

        // Create a bitmap and draw the signature
        // This is a simplified version - in production you'd use SkiaSharp
        // For now, we'll just indicate a signature was captured
        return null;
    }
}

/// <summary>
/// Signature pad control with touch handling.
/// </summary>
public class SignaturePad : GraphicsView
{
    private readonly SignaturePadDrawable _drawable;
    private bool _isDrawing;

    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(SignaturePad), Colors.Black,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is SignaturePad pad && newValue is Color color)
                {
                    pad._drawable.StrokeColor = color;
                    pad.Invalidate();
                }
            });

    public static readonly BindableProperty StrokeWidthProperty =
        BindableProperty.Create(nameof(StrokeWidth), typeof(float), typeof(SignaturePad), 3f,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is SignaturePad pad && newValue is float width)
                {
                    pad._drawable.StrokeWidth = width;
                    pad.Invalidate();
                }
            });

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public float StrokeWidth
    {
        get => (float)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
    }

    public bool HasSignature => _drawable.HasSignature;

    public SignaturePad()
    {
        _drawable = new SignaturePadDrawable();
        Drawable = _drawable;
        
        BackgroundColor = Colors.White;

        // Handle touch events
        StartInteraction += OnStartInteraction;
        DragInteraction += OnDragInteraction;
        EndInteraction += OnEndInteraction;
    }

    private void OnStartInteraction(object? sender, TouchEventArgs e)
    {
        if (e.Touches.Length > 0)
        {
            _isDrawing = true;
            _drawable.StartLine(e.Touches[0]);
            Invalidate();
        }
    }

    private void OnDragInteraction(object? sender, TouchEventArgs e)
    {
        if (_isDrawing && e.Touches.Length > 0)
        {
            _drawable.AddPoint(e.Touches[0]);
            Invalidate();
        }
    }

    private void OnEndInteraction(object? sender, TouchEventArgs e)
    {
        if (_isDrawing)
        {
            _drawable.EndLine();
            _isDrawing = false;
            Invalidate();
            
            // Notify that signature changed
            SignatureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Clear()
    {
        _drawable.Clear();
        Invalidate();
        SignatureChanged?.Invoke(this, EventArgs.Empty);
    }

    public byte[]? GetSignatureImage(int width = 400, int height = 200)
    {
        return _drawable.GetSignatureImage(width, height);
    }

    public event EventHandler? SignatureChanged;
}
