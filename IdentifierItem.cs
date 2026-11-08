namespace DeviceIdentifiersViewer.Models;

public sealed class IdentifierItem
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;

    public IdentifierItem() { }

    public IdentifierItem(string category, string name, string value, string source)
    {
        Category = category;
        Name = name;
        Value = value;
        Source = source;
    }

    public string Display => $"{Category} | {Name} | {Value}";
}
