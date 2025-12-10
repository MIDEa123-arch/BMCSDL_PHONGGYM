using System.Collections.Generic;
using System.Web.Mvc; // Dùng cho SelectListItem

namespace UserApp.ViewModel
{
    public class AuditFilterViewModel
    {
        // Danh sách Audit Logs sau khi đã lọc (Model cho bảng)
        public List<AuditLogViewModel> AuditLogs { get; set; }

        // Danh sách các User để hiển thị trong ComboBox
        public List<SelectListItem> Users { get; set; }

        // User đang được chọn để lọc
        public string SelectedUsername { get; set; }

        public AuditFilterViewModel()
        {
            AuditLogs = new List<AuditLogViewModel>();
            Users = new List<SelectListItem>();
        }
    }
}