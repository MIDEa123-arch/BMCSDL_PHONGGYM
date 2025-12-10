using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserApp.ViewModel
{
    public class SessionInfo
    {
        public int Sid { get; set; }
        public int Serial { get; set; }
        public string Machine { get; set; }
        public string Program { get; set; }
        public string OsUser { get; set; }
        public DateTime LogonTime { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
    }
}