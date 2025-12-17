using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Models;

namespace UserApp.ViewModel
{
    public class NhanVienViewModel
    {
        public int MaNV { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên nhân viên!")]
        public string TenNV { get; set; }

        //[Required(ErrorMessage = "Vui lòng chọn chức vụ!")]
        public int? MaChucVu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại!")]
        [StringLength(10, ErrorMessage = "Số điện thoại không được vượt quá 10 ký tự!")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Số điện thoại chỉ được chứa ký tự số!")]
        [NhanVienSDTValidation(ErrorMessage = "Số điện thoại phải bắt đầu bằng số 0!")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập!")]
        public string TenDangNhap { get; set; }

        public string MatKhau { get; set; }

        [NgaySinhValidation(ErrorMessage = "Ngày sinh không được lớn hơn ngày hiện tại!")]
        public DateTime? NgaySinh { get; set; }

        public string GioiTinh { get; set; }
        public int? TrangThaiTaiKhoan { get; set; } = 1;

        [Display(Name = "Chuyên môn")]
        public List<int> SelectedChuyenMonIds { get; set; }

        public SelectList ChucVuList { get; set; }

        public List<CHUYENMON> ChuyenMonList { get; set; }
    }

    public class NhanVienSDTValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var sdt = value as string;
            if (sdt != null && !sdt.StartsWith("0"))
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
    public class NgaySinhValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var ngaySinh = value as DateTime?;
            if (ngaySinh.HasValue && ngaySinh.Value > DateTime.Now)
            {
                return new ValidationResult(ErrorMessage);
            }
            return ValidationResult.Success;
        }
    }
}