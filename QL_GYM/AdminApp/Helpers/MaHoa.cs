using UserApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
