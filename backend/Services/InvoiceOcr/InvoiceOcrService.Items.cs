using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LuanVanTotNghiep.DTOs;
using Tesseract;

namespace LuanVanTotNghiep.Services
{
    public partial class InvoiceOcrService
    {
private static List<ParsedInvoiceItemDto> DetectItems(List<string> lines)
{
    var items = new List<ParsedInvoiceItemDto>();
    var productAreaStarted = false;

    foreach (var originalLine in lines)
    {
        var line = CleanLine(originalLine);
        var normalized = RemoveVietnameseMarks(line);

        if (string.IsNullOrWhiteSpace(line))
            continue;

        if (normalized.Contains("danh sach hang hoa"))
        {
            productAreaStarted = true;
            continue;
        }

        if (!productAreaStarted)
            continue;

        if (normalized.Contains("tong cong") ||
            normalized.Contains("tong tien") ||
            normalized.Contains("ghi chu") ||
            normalized.Contains("nguoi giao") ||
            normalized.Contains("nguoi nhan"))
        {
            break;
        }

        if (IsInvoiceMetaLine(normalized) || IsTableHeaderLine(normalized))
            continue;

        if (!LooksLikeProductLine(line, normalized))
            continue;

        var cleanedLine = line
            .Replace("|", " ")
            .Replace("¦", " ")
            .Replace("!", " ")
            .Replace(":", " ")
            .Replace(";", " ");

        cleanedLine = Regex.Replace(cleanedLine, @"\s+", " ").Trim();

        var numbers = Regex.Matches(cleanedLine, @"\d[\d\.,]*")
            .Select(m => m.Value)
            .ToList();

        if (numbers.Count < 3)
            continue;

        int quantity;
        decimal unitPrice;

        // Mẫu mới có cột thành tiền:
        // STT Mã SP Tên SP Đơn vị Số lượng Đơn giá Thành tiền
        if (numbers.Count >= 5)
        {
            quantity = ParseInt(numbers[^3]);
            unitPrice = ParseMoney(numbers[^2]);
        }
        else
        {
            // Mẫu cũ không có cột thành tiền:
            // STT Mã SP Tên SP Đơn vị Số lượng Đơn giá
            quantity = ParseInt(numbers[^2]);
            unitPrice = ParseMoney(numbers[^1]);
        }

        if (quantity <= 0 || unitPrice <= 0)
            continue;

        var productCode = DetectProductCode(cleanedLine);
        var productNamePart = cleanedLine;

        // Xóa STT đầu dòng
        productNamePart = Regex.Replace(
            productNamePart,
            @"^\s*\d+\s+",
            "",
            RegexOptions.IgnoreCase
        );

        // Xóa mã sản phẩm
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            productNamePart = Regex.Replace(
                productNamePart,
                Regex.Escape(productCode),
                " ",
                RegexOptions.IgnoreCase
            );
        }

        // Xóa tất cả số: STT, mã số, số lượng, đơn giá, thành tiền
        foreach (var number in numbers)
        {
            productNamePart = productNamePart.Replace(number, " ");
        }

        var unit = DetectUnit(productNamePart);

        // Xóa đơn vị ra khỏi tên sản phẩm
        productNamePart = Regex.Replace(
            productNamePart,
            @"\b(cai|cái|chai|hop|hộp|goi|gói|bich|bịch|ly|kg|thung|thùng|cay|cây)\b",
            "",
            RegexOptions.IgnoreCase
        );

        productNamePart = CleanProductName(productNamePart);

        if (productNamePart.Length < 2)
            continue;

        items.Add(new ParsedInvoiceItemDto
        {
            ProductCode = productCode,
            ProductName = productNamePart,
            Unit = unit,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }

    return items;
}
    }
}

