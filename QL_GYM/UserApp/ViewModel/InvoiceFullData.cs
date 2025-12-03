using System;
using System.Collections.Generic;

namespace UserApp.ViewModel
{
    // 1. Class bao bọc kết quả trả về (Đây là class bạn đang thiếu)
    public class InvoiceFullData
    {
        public InvoiceHeaderViewModel Header { get; set; }
        public List<InvoiceDetailViewModel> Details { get; set; }
    }

    // 2. Class chứa thông tin Header hóa đơn
    public class InvoiceHeaderViewModel
    {
        public int MAHD { get; set; }
        public DateTime NGAYLAP { get; set; }
        public decimal TONGTIEN { get; set; }
        public decimal GIAMGIA { get; set; }
        public decimal THANHTIEN { get; set; }
        public string TENKH { get; set; }
        public string SDT { get; set; }
        // public string DIACHI { get; set; } // Bỏ comment nếu DB có trả về địa chỉ
    }

    // 3. Class chứa chi tiết sản phẩm
    public class InvoiceDetailViewModel
    {
        public string TENSP { get; set; }
        public int SOLUONG { get; set; }
        public decimal DONGIA { get; set; }
        public decimal THANHTIEN_SP { get; set; }
    }
}