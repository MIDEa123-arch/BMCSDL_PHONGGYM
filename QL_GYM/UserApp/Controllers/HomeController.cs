using UserApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Models;
using UserApp.Repositories;

namespace QL_PHONGGYM.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProductRepository _productRepo;
        private readonly AccountRepository _accountRepo;
        public HomeController()
        {
            _productRepo = new ProductRepository(new QL_PHONGGYMEntities());
            _accountRepo = new AccountRepository(new QL_PHONGGYMEntities());
        }
        public ActionResult Index()
        {
            var list = _productRepo.GetGoiTaps();
            ViewBag.usaProducts = _productRepo.GetSanPhams().Where(sp => sp.XuatXu == "USA" && sp.LoaiSP == "Thực phẩm bổ sung").OrderByDescending(sp => sp.SoLuongTon).Take(10).ToList();
            ViewBag.nikeProducts = _productRepo.GetSanPhams().Where(sp => sp.Hang == "Nike").OrderByDescending(sp => sp.SoLuongTon).Take(10).ToList();
            ViewBag.Class = _productRepo.GetChuyenMons().Take(5).ToList();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SignForFree(string HoTen, string SoDienThoai, string Email)
        {
            try
            {
                bool isSuccess = _accountRepo.DangKyThu(HoTen, SoDienThoai, Email);

                if (isSuccess)
                    return Json(new { success = true, message = "Đăng ký thành công!" });
                else
                    return Json(new { success = false, message = "Đăng ký thất bại." });
            }
            catch (Exception ex)
            {
                string err = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = err });
            }
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}