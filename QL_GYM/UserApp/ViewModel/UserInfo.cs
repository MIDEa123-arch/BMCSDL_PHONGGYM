using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserApp.ViewModel
{
    public class UserInfo
    {
        public string Username { get; set; }
        public string AccountStatus { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastLogin { get; set; }

        public bool IsLocked { get; set; }
        public int ActiveSessionCount { get; set; }
    }
}