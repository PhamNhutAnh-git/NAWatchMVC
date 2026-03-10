using System.Text;
using System.Text.RegularExpressions;

namespace NAWatchMVC.Helpers // 1. Đổi sang namespace của ní
{
    public class MyUtil
    {
        // Hàm tạo Slug cho URL (SEO)
        public static string ToUrlSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            value = value.ToLowerInvariant();
            value = value.Normalize(NormalizationForm.FormD);
            var chars = value.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray();
            value = new string(chars).Normalize(NormalizationForm.FormC);

            value = value.Replace("đ", "d");
            value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
            value = Regex.Replace(value, @"\s+", "-");
            value = value.Trim('-');

            return value;
        }

        // 2. NÊN THÊM: Hàm tạo chuỗi ngẫu nhiên (Dùng cho Token, Mã OTP, RandomKey)
        public static string GenerateRandomKey(int length = 5)
        {
            var pattern = @"qazwsxedcrfvtgbyhnujmikolpQAZWSXEDCRFVTGBYHNUJMIKOLP0123456789";
            var sb = new StringBuilder();
            var rd = new Random();
            for (int i = 0; i < length; i++)
            {
                sb.Append(pattern[rd.Next(0, pattern.Length)]);
            }
            return sb.ToString();
        }
    }
}