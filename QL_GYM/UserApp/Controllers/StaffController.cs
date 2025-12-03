using UserApp.Helpers;
using UserApp.Repositories;
using UserApp.ViewModel;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
using System.Web;
using System.IO;
using System.Data.Entity;

namespace UserApp.Controllers
{
    public class StaffController : Controller
    {
        public UserService userService;
        public SanPhamRepository connStr;
        public HoaDonRepository hoaDonRepository;

        public StaffController()
        {
            userService = new UserService();
        }

        public ActionResult HoaDon()
        {
            hoaDonRepository = new HoaDonRepository(Session["connectionString"] as string);
            var list = hoaDonRepository.GetInvoice();
            return View(list);
        }
        public ActionResult SanPham()
        {
            connStr = new SanPhamRepository(Session["connectionString"] as string);
            var list = connStr.GetAll();
            return View(list);
        }

        [HttpPost]
        public JsonResult CheckSessionAlive()
        {
            var connStr = Session["connectionString"] as string;

            if (string.IsNullOrEmpty(connStr))
            {
                return Json(new { alive = false });
            }

            bool alive = userService.CheckConnectionAlive(connStr);

            if (!alive)
            {

                Session.Clear();
                Session.Abandon();
            }

            return Json(new { alive = alive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            string username = Session["user"]?.ToString();
            bool result = userService.Logout(username);

            Session.Clear();
            Session.Abandon();

            if (result)
                return RedirectToAction("Login");

            TempData["Error"] = "Lỗi đăng xuất!";
            return RedirectToAction("Index");
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(FormCollection form)
        {
            string username = form["TenDangNhap"];
            string password = form["MatKhau"];

            try
            {

                int result = userService.Login(username, password);

                if (result == 1)
                {
                    Session["user"] = username;
                    Session["connectionString"] = userService.ConnectionStringUser;
                    Session["loginDate"] = DateTime.Now.ToString("dd/MM/yyyy");
                    return RedirectToAction("HoaDon", "Staff");
                }
                else
                {
                    Session["Admin"] = username;
                    
                    return RedirectToAction("NguoiDung", "Admin");
                }
            }
            catch (OracleException ex)
            {
                if (ex.Number == 1017)
                {
                    TempData["Error"] = "Sai mật khẩu hoặc tài khoản!";

                }
                if (ex.Number == 28000)
                {
                    TempData["Error"] = "Tài khoản bạn đã bị khóa!";

                }
                return RedirectToAction("Login");
            }

        }
        public ActionResult VerifySignatureById(int id)
        {
            hoaDonRepository = new HoaDonRepository(Session["connectionString"] as string);
            // 1. Tìm hóa đơn trong Database
            // Dùng GetInvoice() và tìm kiếm trong danh sách (vì bạn chưa có GetById)
            var invoiceHeader = hoaDonRepository.GetInvoice().FirstOrDefault(i => i.Header.MAHD == id);

            if (invoiceHeader == null)
            {
                ViewBag.Status = "Error";
                ViewBag.Message = $"Không tìm thấy hóa đơn ID = {id} hoặc hóa đơn chưa được ký.";
                return View("VerifySignatureResult");
            }

            try
            {
                // 2. Lấy đường dẫn từ Database
                string dbPdfPath = hoaDonRepository.GetHOADON(id).FILE_PATH;
                string dbSigPath = hoaDonRepository.GetHOADON(id).SIGNATURE_PATH;

                if (string.IsNullOrEmpty(dbSigPath))
                {
                    ViewBag.Status = "Error";
                    ViewBag.Message = "Hóa đơn đã được lưu file, nhưng CHƯA CÓ FILE CHỮ KÝ (.sig).";
                    return View("VerifySignatureResult");
                }

                // 3. Chuyển đường dẫn ảo (Web) thành đường dẫn Vật lý (Server)
                string physicalPdfPath = Server.MapPath("~" + dbPdfPath);
                string physicalSigPath = Server.MapPath("~" + dbSigPath);

                // 4. Kiểm tra file có tồn tại trên server không
                if (!System.IO.File.Exists(physicalPdfPath) || !System.IO.File.Exists(physicalSigPath))
                {
                    ViewBag.Status = "Error";
                    ViewBag.Message = "Không tìm thấy file gốc hoặc file chữ ký trên server (Đường dẫn bị lỗi).";
                    return View("VerifySignatureResult");
                }

                // 5. Cấu hình PFX Admin và gọi Service kiểm tra
                string pfxPath = Server.MapPath("~/App_Data/GymAdmin.pfx");
                string pfxPass = "123456";

                var signService = new DigitalSignService();
                string msgResult = "";

                bool isValid = signService.VerifyFile(physicalPdfPath, physicalSigPath, pfxPath, pfxPass, out msgResult);

                // 6. Trả kết quả ra View
                ViewBag.InvoiceID = id;
                ViewBag.FileName = Path.GetFileName(physicalPdfPath);

                ViewBag.Status = isValid ? "Success" : "Error";
                ViewBag.Message = (isValid ? "✅ HỢP LỆ: " : "❌ KHÔNG HỢP LỆ: ") + msgResult;
            }
            catch (Exception ex)
            {
                ViewBag.Status = "Error";
                ViewBag.Message = "Lỗi hệ thống khi kiểm tra: " + ex.Message;
            }

            return View("VerifySignatureResult");
        }
        public ActionResult Index()
        {
            return View();
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