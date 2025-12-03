using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Models;
using UserApp.Repositories;

namespace UserApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly ProductRepository _productRepo;
        private readonly CartRepository _cartRepo;

        public ProductController()
        {
            _productRepo = new ProductRepository(new QL_PHONGGYMEntities());
            _cartRepo = new CartRepository(new QL_PHONGGYMEntities());
        }
        [HttpPost]
        public JsonResult AddToCart(int maSP, int soLuong)
        {
            int maKH = (int)Session["MaKH"];

            try
            {
                bool result = _cartRepo.AddToCart(maKH, maSP, null, null, soLuong);

                if (result)
                {
                    return Json(new { success = true, message = "Thêm vào giỏ hàng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Thêm vào giỏ hàng thất bại." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public ActionResult ProductDetail(int id)
        {
            var list = _productRepo.GetSanPhams().ToList();
            var sanpham = list.FirstOrDefault(sp => sp.MaSP == id);

            ViewBag.SpDiCung = list.Where(sp => sp.LoaiSP == sanpham.LoaiSP && sp.MaSP != sanpham.MaSP).Take(5).ToList();
            decimal giaHienTai = sanpham.GiaKhuyenMai ?? sanpham.DonGia;
            decimal giaMin, giaMax;

            if (giaHienTai < 1000000)
            {
                giaMin = Math.Floor(giaHienTai / 100000) * 100000;
                giaMax = giaMin + 99999;
            }
            else
            {
                giaMin = Math.Floor(giaHienTai / 1000000) * 1000000;
                giaMax = giaMin + 999999;
            }

            ViewBag.SpCungPhanKhuc = list.Where(sp =>
                sp.MaSP != sanpham.MaSP &&
                ((sp.GiaKhuyenMai ?? sp.DonGia) >= giaMin && (sp.GiaKhuyenMai ?? sp.DonGia) <= giaMax)
            ).Take(5).ToList();
            return View(sanpham);
        }

        public ActionResult Product(string loaisp, string hang, string xuatXu, decimal? maxPrice, decimal? minPrice)
        {
            var list = _productRepo.GetSanPhams();

            if (!string.IsNullOrEmpty(xuatXu))
            {
                list = list.Where(p => p.XuatXu.Contains(xuatXu)).OrderByDescending(sp => sp.SoLuongTon).ToList();
            }
            if (!string.IsNullOrEmpty(loaisp))
            {
                list = list.Where(p => p.LoaiSP.Contains(loaisp)).OrderByDescending(sp => sp.SoLuongTon).ToList();
            }

            if (!string.IsNullOrEmpty(hang))
            {
                list = list.Where(p => p.Hang.Contains(hang)).OrderByDescending(sp => sp.SoLuongTon).ToList();
            }

            if (minPrice.HasValue)
            {
                list = list.Where(p => p.DonGia >= minPrice.Value).OrderByDescending(sp => sp.SoLuongTon).ToList();
            }

            if (maxPrice.HasValue)
            {
                list = list.Where(p => p.DonGia <= maxPrice.Value).OrderByDescending(sp => sp.SoLuongTon).ToList();
            }

            ViewBag.LoaiSP = _productRepo.GetLoaiSanPhams().ToList();
            ViewBag.Hang = list.Where(p => p.Hang != null).Select(p => p.Hang).Distinct().ToList();
            return View(list);
        }
        public ActionResult ClassMenu()
        {
            var list = _productRepo.GetChuyenMons();
            return PartialView(list);
        }

        public ActionResult CategoriesMenu()
        {
            var list = _productRepo.GetLoaiSanPhams();
            return PartialView(list);
        }
        public ActionResult BrandMenu()
        {
            var hangs = _productRepo.GetHangsByLoai();
            return PartialView(hangs);
        }

        public ActionResult OriginMenu()
        {
            var xuatsu = _productRepo.GetXuatSu();
            return PartialView(xuatsu);
        }
    }
}
