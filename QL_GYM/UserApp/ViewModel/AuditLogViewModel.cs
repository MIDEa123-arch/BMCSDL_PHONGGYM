using System;
using System.ComponentModel.DataAnnotations;

namespace UserApp.ViewModel
{
    // ViewModel để hiển thị dữ liệu Audit Trail từ SYS.DBA_AUDIT_TRAIL
    public class AuditLogViewModel
    {
        [Display(Name = "Thời Gian")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime ThoiGian { get; set; }

        [Display(Name = "Tài Khoản")]
        public string DbUser { get; set; }

        [Display(Name = "Đối Tượng")]
        public string TenDoiTuong { get; set; }
        [Display(Name = "Hành Động")]
        public string HanhDong { get; set; }
        [Display(Name = "Câu Lệnh SQL")]
        public string CauLenhSql { get; set; }
    }
}