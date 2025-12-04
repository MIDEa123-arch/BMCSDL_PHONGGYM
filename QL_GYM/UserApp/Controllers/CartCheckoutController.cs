using UserApp.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Models;
using UserApp.ViewModel;

namespace UserApp.Controllers
{
    public class CartCheckoutController : Controller
    {
        private readonly CartRepository _cartRepo;
        private readonly KhachHangRepository _cusRepo;

        public CartCheckoutController()
        {
            _cartRepo = new CartRepository(new QL_PHONGGYMEntities());
            _cusRepo = new KhachHangRepository(new QL_PHONGGYMEntities());
        }

        public ActionResult ThanhToanThanhCong()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TaoHoaDon(FormCollection form)
        {
            try
            {
                var cart = (List<GioHangViewModel>)Session["thanhtoan"];
                int maKH = (int)Session["MaKH"];

               
                int newInvoiceId = _cartRepo.TaoHoaDon(form, maKH, cart);

                // 2. Xóa session giỏ hàng sau khi tạo đơn thành công
                Session["thanhtoan"] = null;
                // Nếu muốn xóa cả giỏ hàng chính
                // _cartRepo.XoaGioHangSauKhiThanhToan(maKH); 

                // 3. CHUYỂN HƯỚNG SANG CONTROLLER KÝ SỐ
                // RedirectToAction("TênAction", "TênController", new { tham_số });
                return RedirectToAction("Sign", "Invoice", new { id = newInvoiceId });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                ViewBag.Error = "Lỗi tạo hóa đơn: " + ex.Message;
                return RedirectToAction("ThanhToanfinal"); // Quay lại trang thanh toán nếu lỗi
            }
        }

        public ActionResult ThanhToanfinal()
        {
            int maKH = (int)Session["MaKH"];
            var kh = _cusRepo.ThongTinKH(maKH);

            ViewBag.Khachhang = kh;
            ViewBag.LoaiKh = kh.MALOAIKH.HasValue ? _cusRepo.LoaiKh((int)kh.MALOAIKH) : null;
            ViewBag.DiaChi = Session["Diachi"] as DIACHI;

            var cart = (List<GioHangViewModel>)Session["thanhtoan"];
            return View(cart.OrderByDescending(sp => sp.NgayThem));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThanhToanfinal(FormCollection form)
        {
            int maKH = (int)Session["MaKH"];

            _cusRepo.ThemDiaChi(maKH, form);
            Session["Diachi"] = _cusRepo.GetDiaChi(maKH);

            var cart = (List<GioHangViewModel>)Session["thanhtoan"];

            return RedirectToAction("ThanhToanfinal");
        }

        public ActionResult GiamSoLuong(int id)
        {
            _cartRepo.Xoa(id);
            return RedirectToAction("ToCheckOut");

        }

        public ActionResult TangSoLuong(int id)
        {
            _cartRepo.Them(id);
            return RedirectToAction("ToCheckOut");

        }

        public ActionResult XoaDon(int id)
        {
            int maKH = (int)Session["MaKH"];
            _cartRepo.XoaDon(id, maKH);

            return RedirectToAction("ToCheckOut");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaVaThanhToan(FormCollection form, string actionType)
        {
            int maKH = (int)Session["MaKH"];

            if (actionType == "delete")
            {
                _cartRepo.XoaDaChon(form, maKH);
                return RedirectToAction("ToCheckOut");
            }
            else if (actionType == "checkout")
            {

                var list = _cartRepo.ChonSanPham(form, maKH);
                Session["thanhtoan"] = list;
                return RedirectToAction("ThanhToan");
            }

            return RedirectToAction("ToCheckOut");
        }

        public ActionResult ThanhToan()
        {
            int maKH = (int)Session["MaKH"];
            var list = (List<GioHangViewModel>)Session["thanhtoan"];
            var kh = _cusRepo.ThongTinKH(maKH);

            if (kh.MALOAIKH.HasValue)
            {
                ViewBag.LoaiKh = _cusRepo.LoaiKh(Convert.ToInt32(kh.MALOAIKH.Value));
            }
            else
            {
                ViewBag.LoaiKh = null;
            }
            var diaChi = _cusRepo.GetDiaChi(maKH);
            ViewBag.Khachhang = kh;
            ViewBag.DiaChi = diaChi;
            Session["Diachi"] = diaChi;
            return View(list.OrderByDescending(sp => sp.NgayThem));
        }
        public ActionResult ToCheckOut()
        {
            int maKH = (int)Session["MaKH"];
            var cart = _cartRepo.GetCart(maKH).OrderByDescending(sp => sp.NgayThem);

            return View(cart);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToCart(int maSP, int soLuong)
        {
            int maKH = Convert.ToInt32(Session["MaKH"]);


            var add = _cartRepo.AddToCart(maKH, maSP, null, null, soLuong);
            Session["cart"] = _cartRepo.GetCart(maKH);
            return RedirectToAction("ToCheckOut");
        }
    }
}