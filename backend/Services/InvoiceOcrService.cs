using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LuanVanTotNghiep.DTOs;
using Tesseract;

namespace LuanVanTotNghiep.Services
{
    public partial class InvoiceOcrService
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
    }
}
