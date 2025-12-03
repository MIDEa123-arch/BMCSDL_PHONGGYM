using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserApp.Helpers
{
    public class CustomListAlphabet
    {
        public static List<char> Alphabet()
        {
            var list = new List<char>();
            for (int ascii = 32; ascii <= 126; ascii++)
            {
                list.Add((char)ascii);
            }
            return list;
        }
    }
}