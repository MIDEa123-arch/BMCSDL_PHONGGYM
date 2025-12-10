using System.Collections.Generic;

namespace UserApp.Models
{
    public class GrantViewModel
    {
        // Dữ liệu cho Dropdown
        public List<string> Users { get; set; }
        public List<string> Tables { get; set; }
        public List<string> Roles { get; set; }

        // Dữ liệu nhận từ Form
        public string SelectedUser { get; set; }
        public string SelectedTable { get; set; }
        public string SelectedRole { get; set; }

        // Các quyền được chọn
        public bool Select { get; set; }
        public bool Insert { get; set; }
        public bool Update { get; set; }
        public bool Delete { get; set; }

        // Thông báo lỗi/thành công
        public string Message { get; set; }
        public string MessageType { get; set; } // "success", "error", "warning"
        public List<string> ExistingPrivileges { get; set; } = new List<string>();
        public GrantViewModel()
        {
            Users = new List<string>();
            Tables = new List<string>();
            Roles = new List<string>();
        }
        public string NewRoleName { get; set; }

        // Dữ liệu cho chức năng 2: Gán User vào Role
        public string UserToAssign { get; set; }
        public string RoleToAssign { get; set; }
    }
}