using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;

namespace NAWatchMVC.Helpers // 1. Đã đổi sang namespace của ní
{
    public class VnPayLibrary
    {
        private SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _requestData.Add(key, value);
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _responseData.Add(key, value);
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out string retValue) ? retValue : string.Empty;
        }

        // Tạo URL để chuyển hướng khách sang trang thanh toán VNPAY
        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            // 1. Sắp xếp dữ liệu (SortedList đã làm sẵn)
            // 2. Tạo chuỗi QueryString đã Encode
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // Sửa đoạn Append trong vòng lặp CreateRequestUrl
                    string encodedValue = WebUtility.UrlEncode(kv.Value).Replace("+", "%20");

                    // Thêm dòng này để chắc chắn các ký tự sau dấu % là viết HOA (VNPAY rất soi chỗ này)
                    encodedValue = System.Text.RegularExpressions.Regex.Replace(encodedValue, @"%[a-f0-9]{2}", m => m.Value.ToUpper());

                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + encodedValue + "&");
                }
            }

            string queryString = data.ToString();
            if (queryString.EndsWith("&"))
            {
                queryString = queryString.Remove(queryString.Length - 1);
            }

            // 3. Tính toán SecureHash dựa trên CHÍNH chuỗi queryString vừa tạo
            string vnp_SecureHash = Utils.HmacSHA512(vnp_HashSecret, queryString);

            // 4. Tạo URL cuối cùng
            return baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
        }

        // Kiểm tra chữ ký từ VNPAY gửi về để đảm bảo không bị hack dữ liệu
        public bool ValidateSignature(string inputHash, string storedHashSecret)
        {
            string rspRaw = GetResponseDataRaw();
            string myChecksum = Utils.HmacSHA512(storedHashSecret, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseDataRaw()
        {
            StringBuilder data = new StringBuilder();
            // Loại bỏ các trường không dùng để băm
            var hashKeys = _responseData.Keys.Where(k => k != "vnp_SecureHashType" && k != "vnp_SecureHash").ToList();

            foreach (string key in hashKeys)
            {
                if (!string.IsNullOrEmpty(_responseData[key]))
                {
                    // QUAN TRỌNG: Không dùng UrlEncode ở đây
                    data.Append(key + "=" + _responseData[key] + "&");
                }
            }
            if (data.Length > 0) data.Remove(data.Length - 1, 1);
            return data.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }

    public class Utils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    // BẮT BUỘC LÀ X VIẾT HOA
                    hash.Append(theByte.ToString("X2"));
                }
            }
            return hash.ToString();
        }
    }
}