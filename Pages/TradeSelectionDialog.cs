using System.Windows;
using System.Windows.Controls;
using OneManVan.Shared.Services;

namespace OneManVan.Pages;

/// <summary>
/// Dialog for selecting a trade type.
/// </summary>
public partial class TradeSelectionDialog : Window
{
    public TradeType? SelectedTrade { get; private set; }

    public TradeSelectionDialog(List<TradeInfo> trades, TradeType currentTrade)
    {
        Title = "Select Trade";
        Width = 500;
        Height = 450;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        
        // Build UI
        var stack = new StackPanel { Margin = new Thickness(20) };
        
        var title = new TextBlock
        {
            Text = "Select Your Trade",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"),
            Margin = new Thickness(0, 0, 0, 10)
        };
        stack.Children.Add(title);
        
        var subtitle = new TextBlock
        {
            Text = "Choose the primary trade type for your business",
            FontSize = 13,
            Foreground = (System.Windows.Media.Brush)FindResource("SubtextBrush"),
            Margin = new Thickness(0, 0, 0, 20)
        };
        stack.Children.Add(subtitle);
        
        foreach (var trade in trades)
        {
            var button = new Button
            {
                Height = 60,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(15),
                Background = trade.Type == currentTrade 
                    ? (System.Windows.Media.Brush)FindResource("PrimaryBrush") 
                    : (System.Windows.Media.Brush)FindResource("SurfaceBrush"),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var icon = new TextBlock
            {
                Text = trade.Icon,
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);
            
            var textStack = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            var name = new TextBlock
            {
                Text = trade.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = trade.Type == currentTrade
                    ? (System.Windows.Media.Brush)FindResource("BackgroundBrush")
                    : (System.Windows.Media.Brush)FindResource("TextBrush")
            };
            textStack.Children.Add(name);
            
            var desc = new TextBlock
            {
                Text = trade.Description,
                FontSize = 12,
                Foreground = trade.Type == currentTrade
                    ? (System.Windows.Media.Brush)FindResource("BackgroundBrush")
                    : (System.Windows.Media.Brush)FindResource("SubtextBrush")
            };
            textStack.Children.Add(desc);
            
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);
            
            button.Content = grid;
            button.Tag = trade.Type;
            button.Click += (s, e) =>
            {
                SelectedTrade = (TradeType)((Button)s!).Tag;
                DialogResult = true;
                Close();
            };
            
            stack.Children.Add(button);
        }
        
        var scrollViewer = new ScrollViewer
        {
            Content = stack,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        
        Content = scrollViewer;
    }
}
