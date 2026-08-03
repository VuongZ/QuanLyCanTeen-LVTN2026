using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LuanVanTotNghiep.DTOs;
using Tesseract;

namespace LuanVanTotNghiep.Services
{
    public partial class InvoiceOcrService
    {
private static decimal DetectInvoiceTotal(List<string> lines)
{
    foreach (var line in lines)
    {
        var normalized = RemoveVietnameseMarks(line);

        if (!normalized.Contains("tong cong") &&
            !normalized.Contains("tong tien") &&
            !normalized.Contains("tong gia tri"))
        {
            continue;
        }

        var numbers = Regex.Matches(line, @"\d[\d\.,]*")
            .Select(m => m.Value)
            .ToList();

        if (numbers.Count == 0)
            continue;

        return ParseMoney(numbers[^1]);
    }

    return 0;
}

private static bool IsInvoiceMetaLine(string normalized)
{
    return normalized.Contains("hoa don") ||
           normalized.Contains("ngay") ||
           normalized.Contains("dia chi") ||
           normalized.Contains("sdt") ||
           normalized.Contains("so dien thoai") ||
           normalized.Contains("nha cung cap") ||
           normalized.Contains("ma hoa don") ||
           normalized.Contains("nguoi ban") ||
           normalized.Contains("don vi ban");
}

private static bool IsTableHeaderLine(string normalized)
{
    return normalized.Contains("stt") ||
           normalized.Contains("ma sp") ||
           normalized.Contains("ten san pham") ||
           normalized.Contains("don vi") ||
           normalized.Contains("so luong") ||
           normalized.Contains("don gia") ||
           normalized.Contains("thanh tien") ||
           normalized.Contains("hang hoa");
}

private static bool LooksLikeProductLine(string line, string normalized)
{
    var hasProductCode = Regex.IsMatch(
        line,
        @"\bS[PPO0]{1,3}\d{1,5}\b",
        RegexOptions.IgnoreCase
    );

    var startsWithIndex = Regex.IsMatch(
        line.Trim(),
        @"^\d{1,2}\s+"
    );

    var hasKnownUnit =
        normalized.Contains("chai") ||
        normalized.Contains("hop") ||
        normalized.Contains("goi") ||
        normalized.Contains("bich") ||
        normalized.Contains("thung") ||
        normalized.Contains("kg") ||
        normalized.Contains("cay") ||
        normalized.Contains("cai");

    return hasProductCode || (startsWithIndex && hasKnownUnit);
}

private static string? DetectProductCode(string line)
{
    var match = Regex.Match(
        line,
        @"\bS[PPO0]{1,3}\d{1,5}\b",
        RegexOptions.IgnoreCase
    );

    if (!match.Success)
        return null;

    return match.Value
        .ToUpperInvariant()
        .Replace("O", "0");
}

private static string CleanProductName(string value)
{
    var cleaned = value ?? "";

    cleaned = cleaned
        .Replace("|", " ")
        .Replace("¦", " ")
        .Replace("_", " ")
        .Replace(":", " ")
        .Replace(";", " ")
        .Replace("!", " ")
        .Replace("&", " ");

    // Xóa mã SP bị OCR dính lại, ví dụ SP001, SPO01, SP00...
    cleaned = Regex.Replace(
        cleaned,
        @"\bS[PPO0]{1,3}\d{0,5}\b",
        " ",
        RegexOptions.IgnoreCase
    );

    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

    var tokens = cleaned
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .ToList();

    // Xóa các token rác ở cuối tên sản phẩm do OCR đọc nhầm từ đường kẻ bảng, ký hiệu tiền, số 0...
    while (tokens.Count > 0 && IsTrailingOcrJunkToken(tokens[^1]))
    {
        tokens.RemoveAt(tokens.Count - 1);
    }

    cleaned = string.Join(" ", tokens);

    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
    cleaned = cleaned.Trim('-', '.', ',', '|', ';', ':', '!', '&', ' ');

    return cleaned;
}

 private static bool IsTrailingOcrJunkToken(string token)
{
    if (string.IsNullOrWhiteSpace(token))
        return true;

    var cleanedToken = token.Trim('-', '.', ',', '|', ';', ':', '!', '&', ' ');
    var normalized = RemoveVietnameseMarks(cleanedToken).ToLowerInvariant();

    if (string.IsNullOrWhiteSpace(normalized))
        return true;

    // OCR hay đọc số 0 thành chữ O ở cuối dòng
    if (Regex.IsMatch(normalized, @"^[o0]+$"))
        return true;

    // OCR hay dính ký hiệu tiền đ thành d/di/đi ở cuối tên
    if (normalized == "d" ||
        normalized == "đ" ||
        normalized == "di" ||
        normalized == "đi")
        return true;

    // OCR hay đọc đường kẻ hoặc phần đơn vị/thành tiền thành B ở cuối
    if (normalized == "b")
        return true;

    return false;
}
       private static string DetectUnit(string text)
{
    var normalized = RemoveVietnameseMarks(text);

    if (normalized.Contains("chai")) return "Chai";
    if (normalized.Contains("hop")) return "Hộp";
    if (normalized.Contains("goi")) return "Gói";
    if (normalized.Contains("bich")) return "Bịch";
    if (normalized.Contains("thung")) return "Thùng";
    if (normalized.Contains("ly")) return "Ly";
    if (normalized.Contains("kg")) return "Kg";
    if (normalized.Contains("cay")) return "Cây";
    if (normalized.Contains("cai")) return "Cái";

    return "Cái";
}
    }
}

