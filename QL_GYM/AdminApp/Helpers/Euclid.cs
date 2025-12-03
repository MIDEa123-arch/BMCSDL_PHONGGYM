using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserApp.Helpers
{
    public static class Euclid
    {
        public static int NormalEuclid(int b, int n)
        {
            int r1 = n, r2 = b;
            int q, r;
            while (r2 > 0)
            {
                q = r1 / r2;
                r = r1 - q * r2;
                r1 = r2;
                r2 = r;
            }

            return r1;
        }

        public static int ExtendedEuclid(int b, int n)
        {
            int r1 = n, r2 = b;
            int t1 = 0, t2 = 1;
            int q, r, t;
            while (r2 > 0)
            {
                q = r1 / r2;
                r = r1 - q * r2;
                r1 = r2;
                r2 = r;

                t = t1 - q * t2;
                t1 = t2;
                t2 = t;
            }

            if (r1 == 1)
            {
                if (t1 < 0)
                    return t1 + n;
                else
                    return t1;
            }
            else
            {
                return -1;
            }

        }

    }
}
