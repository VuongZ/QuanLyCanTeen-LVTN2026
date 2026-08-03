using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LuanVanTotNghiep.DTOs;
using Tesseract;

namespace LuanVanTotNghiep.Services
{
    public partial class InvoiceOcrService
    {
private static string? DetectSupplierName(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var normalized = RemoveVietnameseMarks(lines[i]);

                if (normalized.Contains("nha cung cap") ||
                    normalized.Contains("don vi ban") ||
                    normalized.Contains("nguoi ban") ||
                    normalized.Contains("ten don vi"))
                {
                    var parts = lines[i].Split(':', 2);
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                        return parts[1].Trim();

                    if (i + 1 < lines.Count)
                        return lines[i + 1].Trim();
                }
            }

            return lines.FirstOrDefault(line =>
            {
                var n = RemoveVietnameseMarks(line);
                return !n.Contains("hoa don") &&
                       !n.Contains("invoice") &&
                       line.Length >= 5;
            });
        }

        private static string? DetectInvoiceCode(List<string> lines)
        {
            foreach (var line in lines)
            {
                var normalized = RemoveVietnameseMarks(line);

                if (normalized.Contains("so hoa don") ||
                    normalized.Contains("ma hoa don") ||
                    normalized.Contains("invoice no") ||
                    normalized.Contains("so:"))
                {
                    var match = Regex.Match(line, @"([A-Z0-9]{2,}[-/A-Z0-9]*)", RegexOptions.IgnoreCase);
                    if (match.Success)
                        return match.Value.Trim();
                }
            }

            return null;
        }

        private static string? DetectInvoiceDate(List<string> lines)
        {
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"\b(\d{1,2})[\/\-\.](\d{1,2})[\/\-\.](\d{4})\b");

                if (match.Success)
                {
                    var day = match.Groups[1].Value.PadLeft(2, '0');
                    var month = match.Groups[2].Value.PadLeft(2, '0');
                    var year = match.Groups[3].Value;

                    return $"{year}-{month}-{day}";
                }
            }

            return null;
        }
    }
}

