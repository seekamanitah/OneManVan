using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;

namespace OneManVan.ViewModels;

/// <summary>
/// ViewModel for the onboarding wizard that guides users through initial setup.
/// </summary>
public class WizardViewModel : INotifyPropertyChanged
{
    private int _currentStep = 1;
    private string _selectedTrade = "HVAC";
    private string _databaseMode = "Local";
    private string _connectionTestResult = string.Empty;
    private bool _isConnectionTested;
    private bool _isLoading;
    private bool _isWizardComplete;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? WizardCompleted;

    public int CurrentStep
    {
        get => _currentStep;
        set { _currentStep = value; OnPropertyChanged(); }
    }

    public string SelectedTrade
    {
        get => _selectedTrade;
        set
        {
            _selectedTrade = value;
            OnPropertyChanged();
            LoadPresetFields();
            IsConnectionTested = false;
            ConnectionTestResult = string.Empty;
        }
    }

    public string DatabaseMode
    {
        get => _databaseMode;
        set
        {
            _databaseMode = value;
            OnPropertyChanged();
            IsConnectionTested = false;
            ConnectionTestResult = string.Empty;
        }
    }

    public string ConnectionTestResult
    {
        get => _connectionTestResult;
        set { _connectionTestResult = value; OnPropertyChanged(); }
    }

    public bool IsConnectionTested
    {
        get => _isConnectionTested;
        set { _isConnectionTested = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsWizardComplete
    {
        get => _isWizardComplete;
        set { _isWizardComplete = value; OnPropertyChanged(); }
    }

    public ObservableCollection<PresetField> PresetFields { get; } = [];

    public string[] AvailableTrades { get; } = ["HVAC", "Plumbing", "Electrical", "General"];

    public string[] DatabaseModes { get; } = ["Local", "Remote"];

    public ICommand NextStepCommand { get; }
    public ICommand PreviousStepCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand CompleteWizardCommand { get; }

    public WizardViewModel()
    {
        NextStepCommand = new RelayCommand(NextStep);
        PreviousStepCommand = new RelayCommand(PreviousStep);
        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
        CompleteWizardCommand = new RelayCommand(async () => await CompleteWizardAsync());

        LoadPresetFields();
    }

    private void NextStep()
    {
        if (CurrentStep < 4)
        {
            CurrentStep++;

            if (CurrentStep == 2)
            {
                LoadPresetFields();
            }
        }
    }

    private void PreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
        }
    }

    private async Task TestConnectionAsync()
    {
        IsLoading = true;
        ConnectionTestResult = "Testing connection...";

        try
        {
            if (DatabaseMode == "Local")
            {
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OneManVan",
                    "OneManVan.db");

                var dir = Path.GetDirectoryName(dbPath)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await using var connection = new SqliteConnection($"Data Source={dbPath}");
                await connection.OpenAsync();

                ConnectionTestResult = $"? Local database ready at:\n{dbPath}";
                IsConnectionTested = true;
            }
            else
            {
                // Remote mode - would test SQL Server connection
                ConnectionTestResult = "? Remote mode requires Docker SQL Server.\nEnsure docker-compose is running.";
                IsConnectionTested = true;
            }
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"? Connection failed: {ex.Message}";
            IsConnectionTested = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CompleteWizardAsync()
    {
        IsLoading = true;

        try
        {
            // Save settings
            var settings = new
            {
                Database = new
                {
                    Mode = DatabaseMode,
                LocalPath = "OneManVan.db",
                    RemoteConnection = App.Settings.Database.RemoteConnection
                },
                Trade = new
                {
                    Preset = SelectedTrade,
                    PresetFile = $"Presets/{SelectedTrade}Preset.json"
                },
                Backup = new
                {
                    AutoBackupOnExit = true,
                    BackupFolder = "Backups"
                },
                Sync = new
                {
                    Enabled = DatabaseMode == "Remote",
                    IntervalMinutes = 5
                }
            };

            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(settingsPath, json);

            // Ensure database is created
            await App.DbContext.Database.EnsureCreatedAsync();

            IsWizardComplete = true;
            
            // Notify that wizard is complete
            WizardCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Setup failed: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadPresetFields()
    {
        PresetFields.Clear();

        var presetPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Presets",
            $"{SelectedTrade}Preset.json");

        if (!File.Exists(presetPath))
        {
            // Fallback to HVAC preset
            presetPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Presets",
                "HvacPreset.json");
        }

        if (File.Exists(presetPath))
        {
            try
            {
                var json = File.ReadAllText(presetPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("tables", out var tables))
                {
                    foreach (var table in tables.EnumerateArray())
                    {
                        if (table.TryGetProperty("fields", out var fields))
                        {
                            foreach (var field in fields.EnumerateArray())
                            {
                                PresetFields.Add(new PresetField
                                {
                                    Name = field.GetProperty("displayName").GetString() ?? "Unknown",
                                    Type = field.GetProperty("type").GetString() ?? "Text",
                                    Required = field.TryGetProperty("required", out var req) && req.GetBoolean(),
                                    HelpText = field.TryGetProperty("helpText", out var help) ? help.GetString() : null
                                });
                            }
                        }
                    }
                }

                if (root.TryGetProperty("customFields", out var customFields))
                {
                    foreach (var field in customFields.EnumerateArray())
                    {
                        PresetFields.Add(new PresetField
                        {
                            Name = field.GetProperty("displayName").GetString() ?? "Unknown",
                            Type = field.GetProperty("type").GetString() ?? "Text",
                            Required = false,
                            HelpText = field.TryGetProperty("helpText", out var help) ? help.GetString() : null,
                            IsCustomField = true
                        });
                    }
                }
            }
            catch
            {
                // If preset loading fails, show default fields
                PresetFields.Add(new PresetField { Name = "Serial Number", Type = "Text", Required = true });
                PresetFields.Add(new PresetField { Name = "Brand", Type = "Text", Required = false });
                PresetFields.Add(new PresetField { Name = "Model", Type = "Text", Required = false });
            }
        }
        else
        {
            // Default fields when no preset file found
            PresetFields.Add(new PresetField { Name = "Serial Number", Type = "Text", Required = true });
            PresetFields.Add(new PresetField { Name = "Brand", Type = "Text", Required = false });
            PresetFields.Add(new PresetField { Name = "Model", Type = "Text", Required = false });
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Represents a field from the trade preset for display in the wizard.
/// </summary>
public class PresetField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public bool Required { get; set; }
    public string? HelpText { get; set; }
    public bool IsCustomField { get; set; }
}

/// <summary>
/// Simple relay command implementation for MVVM.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action? _execute;
    private readonly Func<Task>? _executeAsync;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter)
    {
        if (_executeAsync != null)
        {
            await _executeAsync();
        }
        else
        {
            _execute?.Invoke();
        }
    }
}
