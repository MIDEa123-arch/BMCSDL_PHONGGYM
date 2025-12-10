using System;
using System.ComponentModel.DataAnnotations;

namespace UserApp.ViewModel
{
    // ViewModel để hiển thị dữ liệu Audit Trail từ SYS.DBA_AUDIT_TRAIL
    public class AuditLogViewModel
    {
        // Ánh xạ cột TIMESTAMP#
        [Display(Name = "Thời Gian")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime ThoiGian { get; set; }

        // Ánh xạ cột DB_USER
        [Display(Name = "Tài Khoản")]
        public string DbUser { get; set; }

        // Ánh xạ cột OBJECT_SCHEMA (OWNER) và OBJECT_NAME (Tên bảng bị tác động)
        [Display(Name = "Đối Tượng")]
        public string TenDoiTuong { get; set; } // Ví dụ: QL_GYM.KHACHHANG

        // Ánh xạ cột ACTION_NAME (ví dụ: INSERT, UPDATE, DELETE)
        [Display(Name = "Hành Động")]
        public string HanhDong { get; set; }

        // Ánh xạ cột SQL_TEXT (Câu lệnh SQL đầy đủ)
        [Display(Name = "Câu Lệnh SQL")]
        public string CauLenhSql { get; set; }
    }
}