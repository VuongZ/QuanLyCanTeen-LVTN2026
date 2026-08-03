using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LuanVanTotNghiep.DTOs;
using Tesseract;

namespace LuanVanTotNghiep.Services
{
    public partial class InvoiceOcrService
    {
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
                .Replace("â‚«", "")
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

