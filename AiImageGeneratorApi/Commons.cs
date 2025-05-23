using System.Globalization;
using System.Text;

namespace AiImageGeneratorApi
{
    public static class Commons
    {
        public static string TiengVietKhongDau(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string normalized = input.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC)
                                    .Replace("Đ", "D").Replace("đ", "d")
                                    .Replace(" ", "_");
        }
    }

}
