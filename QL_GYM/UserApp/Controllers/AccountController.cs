using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Helpers;
using UserApp.Models;
using UserApp.Repositories;
using UserApp.ViewModel;

namespace UserApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        private readonly AccountRepository _accountRepo;
        private readonly XacThucRepository _verifyRepo;
        private readonly KhachHangRepository _customerRepo;

        public AccountController()
        {
            _accountRepo = new AccountRepository(new QL_PHONGGYMEntities());
            _verifyRepo = new XacThucRepository(true);
            _customerRepo = new KhachHangRepository(new QL_PHONGGYMEntities());
        }

        // GET: /Account/Register
        public ActionResult Register(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(KhachHangRegisterViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _accountRepo.CusRegister(model);

                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Account");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(model);
        }


        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(KhachHangLoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = _accountRepo.CusLogin(model.TenDangNhap, model.MatKhau);

                if (user != null)
                {
                    string currentIp = System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                    if (string.IsNullOrEmpty(currentIp))
                    {
                        currentIp = System.Web.HttpContext.Current.Request.Headers["X-Forwarded-For"];
                    }

                    if (string.IsNullOrEmpty(currentIp))
                    {
                        currentIp = System.Web.HttpContext.Current.Request.UserHostAddress;
                    }

   
                    if (!string.IsNullOrEmpty(currentIp) && currentIp.Contains(","))
                    {
                        currentIp = currentIp.Split(',')[0].Trim();
                    }

                    // 5. Chuẩn hóa Localhost
                    if (currentIp == "::1") currentIp = "127.0.0.1";                    

                    int historyId; 

                    string otpCode = _verifyRepo.CheckIpSecurity(user.MaKH, currentIp, out historyId);

                    if (otpCode != null)
                    {
                        bool isSent = GmailService.SendOTP(user.Email, otpCode);

                        if (isSent)
                        {         
                            Session["PendingLogId"] = historyId; 
                            Session["PendingUserId"] = user.MaKH; 

                            return RedirectToAction("VerifyOTP", "Account");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Hệ thống không gửi được Email xác thực. Vui lòng thử lại.");
                            return View(model);
                        }
                    }

                    Session["MaKH"] = user.MaKH;
                    string fullName = user.TenKH;
                    string firstName = fullName.Split(' ').Last();
                    Session["TenKH"] = firstName;

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.Err = "Tên đăng nhập hoặc mật khẩu không chính xác.";
                }
            }
            return View(model);
        }

        public ActionResult VerifyOTP()
        {
            if (Session["PendingLogId"] == null) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public ActionResult VerifyOTP(string otpInput)
        {
            if (Session["PendingLogId"] == null) return RedirectToAction("Login");
            int logId = (int)Session["PendingLogId"];

            var result = _verifyRepo.Verify(logId, otpInput);

            if (result)
            {               
                int userId = (int)Session["PendingUserId"];
                var user = _customerRepo.ThongTinKH(userId);

                Session["MaKH"] = user.MAKH;
                Session["TenKH"] = user.TENKH.Split(' ').Last();

                Session.Remove("PendingLogId");
                Session.Remove("PendingUserId");

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Mã xác thực không đúng hoặc đã hết hạn!";
            return View();
        }

        // GET: /Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}