using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UserApp.Helpers;

namespace UserApp.Helpers
{
    public static class MaHoa
    {
        public static int n = CustomListAlphabet.Alphabet().Count;
        public static List<char> ListChar = CustomListAlphabet.Alphabet();
        public static string MaHoaCong(string input, int k)
        {
            List<char> result = new List<char>();

            foreach (char c in input)
            {
                int index = ListChar.IndexOf(c);
                int newIndex = (index + k) % n;

                result.Add(ListChar[newIndex]);
            }
            return new string(result.ToArray());
        }

        public static string MaHoaNhan(string input, int k)
        {
            List<char> result = new List<char>();

            if (Euclid.NormalEuclid(k, n) != 1)
                throw new Exception(string.Format("Khóa k={0} không hợp lệ vì gcd({0}, {1}) ≠ 1, không thể giải mã.", k, n));

            foreach (char c in input)
            {
                int index = ListChar.IndexOf(c);
                int newIndex = (index * k) % n;

                result.Add(ListChar[newIndex]);
            }
            return new string(result.ToArray());
        }
        public static string MahoaDes(string input, int k)
        {
            byte[] keyBytes = new byte[8];
            byte[] kBytes = BitConverter.GetBytes(k);
            for (int i = 0; i < 8; i++)
                keyBytes[i] = (i < kBytes.Length) ? kBytes[i] : (byte)0;

            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = keyBytes;
                des.IV = keyBytes;

                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputBytes, 0, inputBytes.Length);
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }
}
