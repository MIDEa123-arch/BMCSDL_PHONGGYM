using UserApp.Helpers;
using UserApp.Repositories;
using UserApp.ViewModel;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
using System.Web;


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