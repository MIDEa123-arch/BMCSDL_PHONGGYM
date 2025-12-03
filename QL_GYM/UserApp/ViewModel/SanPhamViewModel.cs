using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserApp.ViewModel
{
    public class SanPhamViewModel
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public string LoaiSP { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuongTon { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public string Hang { get; set; }
        public string XuatXu { get; set; }
        public string BaoHanh { get; set; }
        public string MoTaChung { get; set; }
        public string MaTaChiTiet { get; set; }
        public string UrlHinhAnhChinh { get; set; }
        public List<string> UrlHinhAnhsPhu { get; set; }
    }
}