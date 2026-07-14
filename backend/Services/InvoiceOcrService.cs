using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LuanVanTotNghiep.DTOs;
using Tesseract;

namespace LuanVanTotNghiep.Services
{
    public class InvoiceOcrService
    {
        private readonly IWebHostEnvironment _env;

        public InvoiceOcrService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<ParsedInvoiceDto> ParseInvoiceImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Vui lòng chọn ảnh hóa đơn.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Chỉ hỗ trợ ảnh JPG, JPEG hoặc PNG.");

            if (file.Length > 8 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh hóa đơn không được vượt quá 8MB.");

            var tempDir = Path.Combine(_env.ContentRootPath, "TempUploads");
            Directory.CreateDirectory(tempDir);

            var tempPath = Path.Combine(tempDir, $"{Guid.NewGuid()}{extension}");

            try
            {
                await using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var tessDataPath = Path.Combine(_env.ContentRootPath, "tessdata");

                if (!Directory.Exists(tessDataPath))
                    throw new InvalidOperationException("Không tìm thấy thư mục tessdata.");

                using var engine = new TesseractEngine(tessDataPath, "vie+eng", EngineMode.Default);
                using var image = Pix.LoadFromFile(tempPath);
                using var page = engine.Process(image);

                var rawText = page.GetText() ?? "";
                var confidence = page.GetMeanConfidence();

                var result = ParseRawInvoiceText(rawText);
                result.RawText = rawText;
                result.Confidence = confidence;

                if (result.Items.Count == 0)
                {
                    result.Warnings.Add("Không nhận diện được dòng sản phẩm rõ ràng. Vui lòng kiểm tra ảnh hoặc nhập bằng Excel.");
                }

                if (confidence < 0.55f)
                {
                    result.Warnings.Add("Độ tin cậy OCR thấp. Vui lòng kiểm tra kỹ dữ liệu trước khi nhập kho.");
                }

                return result;
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

       private static ParsedInvoiceDto ParseRawInvoiceText(string rawText)
{
    var result = new ParsedInvoiceDto();

    var lines = rawText
        .Split('\n')
        .Select(CleanLine)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToList();

    result.DetectedSupplierName = DetectSupplierName(lines);
    result.InvoiceCode = DetectInvoiceCode(lines);
    result.InvoiceDate = DetectInvoiceDate(lines);
    result.Items = DetectItems(lines);

    var calculatedTotal = result.Items.Sum(i => i.Quantity * i.UnitPrice);
    var detectedTotal = DetectInvoiceTotal(lines);

    result.TotalAmount = detectedTotal > 0 ? detectedTotal : calculatedTotal;

    if (detectedTotal > 0 && calculatedTotal > 0 && Math.Abs(detectedTotal - calculatedTotal) > 1)
    {
        result.Warnings.Add(
            $"Tổng cộng OCR đọc được là {detectedTotal:N0}đ nhưng tổng tính từ sản phẩm là {calculatedTotal:N0}đ. Vui lòng kiểm tra lại số lượng hoặc đơn giá."
        );
    }

    return result;
}

        private static string CleanLine(string value)
        {
            return Regex.Replace(value ?? "", @"\s+", " ").Trim();
        }

        private static string RemoveVietnameseMarks(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var ch in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

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

        private static int ParseInt(string value)
        {
            var cleaned = value
                .Replace(".", "")
                .Replace(",", "")
                .Trim();

            return int.TryParse(cleaned, out var parsed) ? parsed : 0;
        }

        private static decimal ParseMoney(string value)
        {
            var cleaned = value
                .Replace("₫", "")
                .Replace("đ", "")
                .Replace("Đ", "")
                .Replace(" ", "")
                .Replace(".", "")
                .Replace(",", ".");

            return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }
    }
}