using UserApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserApp.Helpers
{
    public static class GiaiMa
    {
        public static int n = CustomListAlphabet.Alphabet().Count;
        public static List<char> ListChar = CustomListAlphabet.Alphabet();

        public static string GiaiMaCong(string input, int k)
        {
            List<char> result = new List<char>();

            foreach (char c in input)
            {
                int index = ListChar.IndexOf(c);
                int newIndex = (index - k) % n;

                if (newIndex < 0)
                    newIndex = newIndex + n;

                result.Add(ListChar[newIndex]);
            }
            return new string(result.ToArray());
        }

        public static string GiaiMaNhan(string input, int k)
        {
            List<char> result = new List<char>();

            int kInv = Euclid.ExtendedEuclid(k, n);

            foreach (char c in input)
            {
                int index = ListChar.IndexOf(c);
                if (Euclid.NormalEuclid(k, n) != 1)
                    throw new Exception(string.Format("Khóa k={0} không hợp lệ vì gcd({0}, {1}) ≠ 1, không thể giải mã.", k, n));

                int newIndex = (index * kInv) % n;
                result.Add(ListChar[newIndex]);
            }

            return new string(result.ToArray());
        }
    }
}
