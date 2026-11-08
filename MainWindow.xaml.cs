using System.Collections.ObjectModel;
using DeviceIdentifiersViewer.Models;
using DeviceIdentifiersViewer.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace DeviceIdentifiersViewer;

public sealed partial class MainWindow : Window
{
    private readonly IdentifierCollector _collector = new();
    private readonly ObservableCollection<IdentifierItem> _allItems = new();
    private readonly ObservableCollection<IdentifierItem> _visibleItems = new();
    private List<string> _warnings = new();
    private DateTime _lastGenerated = DateTime.Now;

    public MainWindow()
    {
        InitializeComponent();
        ItemsList.ItemsSource = _visibleItems;
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        SetBusy(true, "Собираю идентификаторы…");
        try
        {
            var (items, warnings) = await Task.Run(() => _collector.CollectAll());
            _allItems.Clear();
            foreach (var i in items) _allItems.Add(i);
            _warnings = warnings;
            _lastGenerated = DateTime.Now;
            ApplyFilter();

            string status = $"Готово. Записей: {_allItems.Count}.";
            if (warnings.Count > 0) status += $"  Предупреждений: {warnings.Count}.";
            SetBusy(false, status);
        }
        catch (Exception ex)
        {
            SetBusy(false, $"Ошибка сбора: {ex.Message}");
        }
    }

    private void ApplyFilter()
    {
        string q = SearchBox.Text?.Trim() ?? string.Empty;
        _visibleItems.Clear();
        foreach (var item in _allItems)
        {
            if (q.Length == 0 ||
                item.Category.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                item.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                item.Value.Contains(q, StringComparison.OrdinalIgnoreCase))
            {
                _visibleItems.Add(item);
            }
        }
    }

    private void SetBusy(bool busy, string status)
    {
        BusyRing.IsActive = busy;
        StatusText.Text = status;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var selected = ItemsList.SelectedItems.Count > 0
            ? ItemsList.SelectedItems.Cast<IdentifierItem>().ToList()
            : _visibleItems.ToList();

        if (selected.Count == 0) { StatusText.Text = "Нечего копировать."; return; }

        var text = string.Join(Environment.NewLine,
            selected.Select(i => $"{i.Category}\t{i.Name}\t{i.Value}"));

        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
        StatusText.Text = $"Скопировано строк: {selected.Count}.";
    }

    private async void ExportTxt_Click(object sender, RoutedEventArgs e)
    {
        var content = ReportExporter.ToText(_allItems, _warnings, _lastGenerated);
        await SaveWithPickerAsync(content, "DeviceReport", ".txt");
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        var content = ReportExporter.ToJson(_allItems, _warnings, _lastGenerated);
        await SaveWithPickerAsync(content, "DeviceReport", ".json");
    }

    private async Task SaveWithPickerAsync(string content, string suggestedName, string extension)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = $"{suggestedName}_{_lastGenerated:yyyyMMdd_HHmmss}"
            };
            picker.FileTypeChoices.Add(extension.TrimStart('.').ToUpperInvariant(), new List<string> { extension });

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file is null) { StatusText.Text = "Сохранение отменено."; return; }

            await Windows.Storage.FileIO.WriteTextAsync(file, content);
            StatusText.Text = $"Сохранено: {file.Path}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка сохранения: {ex.Message}";
        }
    }
}
