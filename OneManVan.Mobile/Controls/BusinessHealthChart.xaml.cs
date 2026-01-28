using Microsoft.Maui.Graphics;
using OneManVan.Shared.Models;

namespace OneManVan.Mobile.Controls;

/// <summary>
/// Business health chart showing revenue trends and completion rates.
/// </summary>
public partial class BusinessHealthChart : ContentView
{
    private List<decimal> _revenueTrend = new();
    private double _completionRate = 0;
    private decimal _avgRevenue = 0;

    public BusinessHealthChart()
    {
        InitializeComponent();
        RevenueChartView.Drawable = new RevenueChartDrawable();
    }

    /// <summary>
    /// Update chart with dashboard metrics.
    /// </summary>
    public void UpdateMetrics(DashboardMetrics metrics)
    {
        _revenueTrend = metrics.RevenueTrend;
        _completionRate = metrics.CompletionRate;
        _avgRevenue = metrics.RevenueTrend.Any() ? metrics.RevenueTrend.Average() : 0;

        // Update labels
        CompletionRateLabel.Text = $"{_completionRate:F1}%";
        CompletionProgressBar.Progress = _completionRate / 100.0;
        
        AvgRevenueLabel.Text = _avgRevenue.ToString("C0");

        // Show trend badge if trending up
        if (metrics.IsRevenueTrendingUp)
        {
            TrendBadge.IsVisible = true;
            TrendLabel.Text = metrics.RevenueTrendText;
            TrendBadge.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#1B3D1F")
                : Color.FromArgb("#E8F5E9");
            TrendLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#81C784")
                : Color.FromArgb("#388E3C");
        }
        else if (metrics.RevenueChangePercent < 0)
        {
            TrendBadge.IsVisible = true;
            TrendLabel.Text = metrics.RevenueTrendText;
            TrendBadge.BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#3D1F1F")
                : Color.FromArgb("#FFEBEE");
            TrendLabel.TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#EF9A9A")
                : Color.FromArgb("#C62828");
        }

        // Update footer
        FooterLabel.Text = $"Updated {DateTime.Now:h:mm tt}";

        // Hide placeholder and draw chart
        ChartPlaceholder.IsVisible = false;
        
        // Update the chart drawable with data
        if (RevenueChartView.Drawable is RevenueChartDrawable drawable)
        {
            drawable.SetData(_revenueTrend);
            RevenueChartView.Invalidate();
        }
    }

    /// <summary>
    /// Custom drawable for revenue trend chart.
    /// </summary>
    private class RevenueChartDrawable : IDrawable
    {
        private List<decimal> _data = new();
        private readonly Color _lineColor = Color.FromArgb("#2196F3");
        private readonly Color _fillColor = Color.FromArgb("#2196F3").WithAlpha(0.2f);
        private readonly Color _gridColor = Color.FromArgb("#E0E0E0").WithAlpha(0.3f);

        public void SetData(List<decimal> data)
        {
            _data = data ?? new();
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_data == null || _data.Count == 0)
            {
                return;
            }

            var width = dirtyRect.Width;
            var height = dirtyRect.Height;
            var padding = 20f;
            var chartWidth = width - (padding * 2);
            var chartHeight = height - (padding * 2);

            // Draw grid lines (horizontal)
            canvas.StrokeColor = _gridColor;
            canvas.StrokeSize = 1;
            for (int i = 0; i <= 4; i++)
            {
                var y = padding + (chartHeight * i / 4);
                canvas.DrawLine(padding, y, width - padding, y);
            }

            // Get min/max for scaling
            var max = (float)_data.Max();
            var min = (float)_data.Min();
            var range = max - min;
            
            // Avoid division by zero
            if (range == 0) range = 1;

            // Create points for line
            var points = new List<PointF>();
            for (int i = 0; i < _data.Count; i++)
            {
                var x = padding + (chartWidth * i / (_data.Count - 1));
                var normalized = (_data[i] - (decimal)min) / (decimal)range;
                var y = padding + chartHeight - (chartHeight * (float)normalized);
                points.Add(new PointF(x, y));
            }

            // Draw filled area under line
            if (points.Count > 1)
            {
                var path = new PathF();
                path.MoveTo(padding, height - padding);
                foreach (var point in points)
                {
                    path.LineTo(point.X, point.Y);
                }
                path.LineTo(width - padding, height - padding);
                path.Close();

                canvas.FillColor = _fillColor;
                canvas.FillPath(path);
            }

            // Draw line
            if (points.Count > 1)
            {
                canvas.StrokeColor = _lineColor;
                canvas.StrokeSize = 3;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.StrokeLineJoin = LineJoin.Round;

                var path = new PathF();
                path.MoveTo(points[0].X, points[0].Y);
                for (int i = 1; i < points.Count; i++)
                {
                    path.LineTo(points[i].X, points[i].Y);
                }

                canvas.DrawPath(path);
            }

            // Draw dots on data points
            canvas.FillColor = _lineColor;
            foreach (var point in points)
            {
                canvas.FillCircle(point.X, point.Y, 4);
            }

            // Draw labels (days)
            canvas.FontColor = Color.FromArgb("#757575");
            canvas.FontSize = 10;
            var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var today = DateTime.Now.DayOfWeek;
            
            for (int i = 0; i < Math.Min(_data.Count, 7); i++)
            {
                var dayIndex = ((int)today - (6 - i) + 7) % 7;
                var x = padding + (chartWidth * i / (_data.Count - 1));
                canvas.DrawString(days[dayIndex], x - 12, height - 5, 30, 15, HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }
    }
}
