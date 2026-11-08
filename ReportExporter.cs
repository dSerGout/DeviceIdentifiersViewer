using System.Text;
using System.Text.Json;
using DeviceIdentifiersViewer.Models;

namespace DeviceIdentifiersViewer.Services;

public static class ReportExporter
{
    public static string ToText(IReadOnlyList<IdentifierItem> items, IReadOnlyList<string> warnings, DateTime generatedAt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("============================================================");
        sb.AppendLine("  Device Identifiers Viewer — Report");
        sb.AppendLine($"  Generated: {generatedAt:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine("============================================================");
        sb.AppendLine();

        string? currentCategory = null;
        foreach (var item in items.OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                                  .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (currentCategory != item.Category)
            {
                currentCategory = item.Category;
                sb.AppendLine();
                sb.AppendLine($"--- {currentCategory} ---");
            }
            sb.AppendLine($"{item.Name,-40} : {item.Value}");
            sb.AppendLine($"{new string(' ', 40)}   [{item.Source}]");
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- Warnings ---");
            foreach (var w in warnings) sb.AppendLine("  " + w);
        }

        sb.AppendLine();
        sb.AppendLine("Note: this report is READ-ONLY. The application cannot change");
        sb.AppendLine("hardware identifiers (SMBIOS, disk serials, CPU ID, TPM EK).");
        return sb.ToString();
    }

    public static string ToJson(IReadOnlyList<IdentifierItem> items, IReadOnlyList<string> warnings, DateTime generatedAt)
    {
        var payload = new
        {
            tool = "DeviceIdentifiersViewer",
            version = "1.0.0",
            generatedAt = generatedAt.ToString("o"),
            readOnly = true,
            note = "Hardware identifiers (SMBIOS UUID, disk serials, CPU ID, TPM EK) are read-only and cannot be changed from user-space.",
            warnings,
            identifiers = items.OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                               .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                               .Select(x => new { category = x.Category, name = x.Name, value = x.Value, source = x.Source })
                               .ToList()
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
