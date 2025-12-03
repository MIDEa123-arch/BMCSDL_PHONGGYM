using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserApp.ViewModel
{
    public class UserViewModel
    {
        public string UserName { get; set; }
        public DateTime LoginDate { get; set; }

        public UserViewModel(string username)
        {
            UserName = username;
            LoginDate = DateTime.Now;
        }
    }

}