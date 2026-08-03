using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LuanVanTotNghiep.DTOs;
using Tesseract;

namespace LuanVanTotNghiep.Services
{
    public partial class InvoiceOcrService
    {
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
    }
}

