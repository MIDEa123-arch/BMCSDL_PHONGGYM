using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;   
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using UserApp.Helpers; 
using UserApp.Models;
using UserApp.Repositories;

namespace QL_PHONGGYM.Controllers
{
    public class AdminReportController : Controller
    {
        private QL_PHONGGYMEntities _context;

        private const string PublicKeyXml = @"<RSAKeyValue>
            <Modulus>qGgRe0G7s9b7d7c5bhehV2DwGBqw9uyQIO72tYjyGmcbbsbgX7KKwlf7g9QUub5Cljj4HA3xaAS97AfY7VQW5MiRux+xz9pZH7qZevwk2RJ8YknJ2DbREHwoancunen6QfTFLPX8VxplCNaejkBlIpt9x0MiDi1J3DqKYxpq+rAnCT9sYF2QCe4Eq5UcDjtGu+zhxT6jQyjisLLEdLwJBxrAf06ACHDm2FdgSkJUjZdLXR6wNexoxH2c2Zp0re9r1wOtijaWKX4O8FG+0Px57cilZXMaz+stuta1ae6wImucNd9T4ZBMb+6kGF2EISnqkL1yY8Y4A54pFHDQOfz8mQ==</Modulus>
            <Exponent>AQAB</Exponent>
            </RSAKeyValue>";




        public ActionResult GenerateRSAKey()
        {
            using (var rsa = new System.Security.Cryptography.RSACryptoServiceProvider(2048))
            {
                string publicKeyXml = rsa.ToXmlString(false); // public
                string privateKeyXml = rsa.ToXmlString(true); // private

                return Content(
                    "PUBLIC KEY XML:\n\n" + publicKeyXml +
                    "\n\n====================\n\nPRIVATE KEY XML:\n\n" + privateKeyXml,
                    "text/plain"
                );
            }
        }
        
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["connectionString"] != null)
            {               
                _context = new QL_PHONGGYMEntities(true);
            }
            else
            {
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new { success = false, message = "Phiên làm việc hết hạn. Vui lòng F5 đăng nhập lại." },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                else
                {
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary(new { controller = "AdminHome", action = "Login" })
                    );
                }
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetRevenueData(string fromDate, string toDate)
        {
            try
            {
                DateTime start = DateTime.Parse(fromDate);
                DateTime end = DateTime.Parse(toDate).AddDays(1).AddSeconds(-1);

                var rawData = _context.CHITIETHOADONs
                    .Where(ct => ct.HOADON.TRANGTHAI == "Đã thanh toán"
                              && ct.HOADON.NGAYLAP >= start
                              && ct.HOADON.NGAYLAP <= end)
                    .Select(ct => new
                    {
                        IsSanPham = ct.SANPHAM != null,
                        IsGoiTap = ct.DANGKYGOITAP != null,
                        IsPT = ct.DANGKYPT != null,
                        IsLopHoc = ct.DANGKYLOP != null,
                        DonGia = ct.DONGIA,
                        SoLuong = ct.SOLUONG ?? 0
                    })
                    .ToList();

                var reportList = new List<dynamic>();

                foreach (var item in rawData)
                {
                    string loaiHinh = "Khác";
                    if (item.IsSanPham) loaiHinh = "Bán hàng & Dụng cụ";
                    else if (item.IsGoiTap) loaiHinh = "Gói tập Gym";
                    else if (item.IsPT) loaiHinh = "Huấn luyện viên (PT)";
                    else if (item.IsLopHoc) loaiHinh = "Lớp học";

                    decimal thanhTien = item.DonGia * item.SoLuong;
                    reportList.Add(new { LoaiHinh = loaiHinh, ThanhTien = thanhTien });
                }

                var reportData = reportList
                    .GroupBy(x => x.LoaiHinh)
                    .Select(g => new
                    {
                        Label = g.Key,
                        Value = g.Sum(x => (decimal)x.ThanhTien)
                    })
                    .OrderByDescending(x => x.Value)
                    .ToList();

                decimal totalRevenue = reportData.Sum(x => x.Value);

                return Json(new
                {
                    success = true,
                    data = reportData,
                    total = totalRevenue.ToString("N0")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetMonthlyRevenue(int year)
        {
            var data = _context.HOADONs
                .Where(h => h.TRANGTHAI == "Đã thanh toán" && h.NGAYLAP.Year == year)
                .Select(h => new { h.NGAYLAP.Month, h.TONGTIEN })
                .ToList();

            decimal[] monthlyData = new decimal[12];
            foreach (var item in data)
            {
                monthlyData[item.Month - 1] += item.TONGTIEN ?? 0;
            }

            return Json(new { success = true, data = monthlyData, year = year });
        }

        [HttpGet]
        public ActionResult ExportToExcel(string fromDate, string toDate)
        {
            try
            {
                DateTime start = DateTime.Parse(fromDate);
                DateTime end = DateTime.Parse(toDate).AddDays(1).AddSeconds(-1);

                // Dùng .Select để tối ưu SQL và tránh lỗi Mapping
                var dataExport = _context.CHITIETHOADONs
                    .Where(ct => ct.HOADON.TRANGTHAI == "Đã thanh toán"
                                && ct.HOADON.NGAYLAP >= start
                                && ct.HOADON.NGAYLAP <= end)
                    .Select(ct => new
                    {
                        MaHD = ct.HOADON.MAHD,
                        NgayLap = ct.HOADON.NGAYLAP,
                        TenSP = ct.SANPHAM != null ? ct.SANPHAM.TENSP : null,
                        TenGoi = ct.DANGKYGOITAP != null ? ct.DANGKYGOITAP.GOITAP.TENGOI : null,
                        TenLop = ct.DANGKYLOP != null ? ct.DANGKYLOP.LOPHOC.TENLOP : null,
                        IsPT = ct.DANGKYPT != null,
                        SoLuong = ct.SOLUONG ?? 1,
                        DonGia = ct.DONGIA
                    })
                    .ToList();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Mã Hóa Đơn,Ngày Lập,Nội Dung Chi Tiết,Loại Hình,Số Lượng,Đơn Giá,Thành Tiền");

                foreach (var item in dataExport)
                {
                    string loaiHinh = "Khác";
                    string noiDung = "Dịch vụ khác";

                    if (item.TenSP != null) { loaiHinh = "Bán hàng & Dụng cụ"; noiDung = item.TenSP; }
                    else if (item.TenGoi != null) { loaiHinh = "Gói tập Gym"; noiDung = item.TenGoi; }
                    else if (item.TenLop != null) { loaiHinh = "Lớp học"; noiDung = item.TenLop; }
                    else if (item.IsPT) { loaiHinh = "Huấn luyện viên (PT)"; noiDung = "Thuê PT"; }

                    noiDung = "\"" + noiDung.Replace("\"", "\"\"") + "\"";

                    var line = string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        item.MaHD,
                        // Đã bỏ HasValue/Value theo yêu cầu, truy xuất trực tiếp
                        item.NgayLap.ToString("dd/MM/yyyy HH:mm"),
                        noiDung,
                        loaiHinh,
                        item.SoLuong,                        
                        item.DonGia.ToString("0.##"),
                        (item.DonGia * item.SoLuong).ToString("0.##")
                    );
                    sb.AppendLine(line);
                }

                byte[] buffer = Encoding.UTF8.GetPreamble()
                    .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

                // 1. Tạo thư mục tạm và lưu file CSV
                string fileName = $"BaoCaoDoanhThu_{start:ddMMyyyy}_{end:ddMMyyyy}.csv";
                string tempFolder = Server.MapPath("~/App_Data/Temp/");
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                string tempCsvPath = Path.Combine(tempFolder, fileName);
                System.IO.File.WriteAllBytes(tempCsvPath, buffer);

                // Mã hóa file bằng DesRsaCrypto
                string securePath = tempCsvPath + ".secure";
                var crypto = new DesRsaCrypto();

                // Mã hóa file CSV bằng khóa DES sinh ngẫu nhiên, sau đó khóa DES được mã hóa bằng RSA Public Key
                crypto.EncryptFile(tempCsvPath, securePath, PublicKeyXml);

                // Đọc file .secure đã mã hóa để trả về
                byte[] encryptedBytes = System.IO.File.ReadAllBytes(securePath);

                // Xóa file gốc và file tạm để bảo mật
                if (System.IO.File.Exists(tempCsvPath)) System.IO.File.Delete(tempCsvPath);
                if (System.IO.File.Exists(securePath)) System.IO.File.Delete(securePath);

                // Trả về file đã mã hóa (.secure)
                return File(encryptedBytes, "application/octet-stream", fileName + ".secure");
            }
            catch (Exception ex)
            {
                return Content("Lỗi khi xuất file: " + ex.Message);
            }
        }
    }
}