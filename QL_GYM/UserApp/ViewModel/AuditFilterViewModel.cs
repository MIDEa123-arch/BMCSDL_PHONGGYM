using System.Collections.Generic;
using System.Web.Mvc; // Dùng cho SelectListItem

namespace UserApp.ViewModel
{
    public class AuditFilterViewModel
    {
        public List<AuditLogViewModel> AuditLogs { get; set; }

        public List<SelectListItem> Users { get; set; }
        public List<SelectListItem> Tables { get; set; }
        public string SelectedUsername { get; set; }
        public string SelectedTableName { get; set; }
        public AuditFilterViewModel()
        {
            AuditLogs = new List<AuditLogViewModel>();
            Users = new List<SelectListItem>();
        }
    }
}