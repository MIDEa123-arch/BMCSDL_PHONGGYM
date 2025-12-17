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
using static System.Net.WebRequestMethods;

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

        public ActionResult ForgotPassword()
        {
            return View();

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(FormCollection form)
        {
            if (String.IsNullOrEmpty(form["email"]))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập email";
                return View();
            }

            try
            {
                string otp;
                var result = _customerRepo.SendOTP(form["email"], out otp);

                if (result)
                {
                    Session["OTP"] = MaHoa.MahoaDes(otp, "123");
                    Session["OTP_Expire"] = DateTime.Now.AddMinutes(3);
                    Session["AllowOTPPage"] = true;

                    return RedirectToAction("XacThuc", new { email = form["email"] });
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View();
            }
        }

        public ActionResult XacThuc(string email)
        {
            if (Session["AllowOTPPage"] == null)
                return RedirectToAction("ForgotPassword");

            Session.Remove("AllowOTPPage");

            if (Session["OTP"] == null || Session["OTP_Expire"] == null)
                return RedirectToAction("ForgotPassword");

            if (String.IsNullOrEmpty(email))
                return RedirectToAction("ForgotPassword");            

            return View((object)email);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult KhoiPhucMatKhau(string otp1, string otp2, string otp3, string otp4, string emailXacthuc)
        {
            string userOtp = otp1 + otp2 + otp3 + otp4;

            string savedOtp = Session["OTP"] as string;
            DateTime? expire = Session["OTP_Expire"] as DateTime?;

            if (savedOtp == null || expire == null)
                return RedirectToAction("ForgotPassword");

            if (DateTime.Now > expire)
            {
                TempData["OtpError"] = "Mã OTP đã hết hạn!";
                return RedirectToAction("ForgotPassword");
            }

            if (MaHoa.MahoaDes(userOtp, "123") != savedOtp)
            {
                TempData["OtpError"] = "OTP không chính xác!";
                Session["AllowOTPPage"] = true;
                return RedirectToAction("XacThuc", new { email = emailXacthuc });
            }

            return RedirectToAction("DatMatKhauMoi", new { email = emailXacthuc });
        }

        public ActionResult DatMatKhauMoi(string email)
        {
            if (Session["OTP"] == null || Session["OTP_Expire"] == null)
                return RedirectToAction("ForgotPassword");

            ViewBag.Email = email;

            Session.Remove("OTP");
            Session.Remove("OTP_Expire");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatMatKhauMoi(ResetPasswordViewModel model, string email)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Email = email;
                return View(model);
            }
            try
            {
                var result = _accountRepo.KhoiPhucMK(email, model.MatKhau);
                if (result)
                {
                    TempData["ThongBao"] = "Đổi mật khẩu thành công vui lòng đăng nhập lại";
                    return RedirectToAction("Login");
                }
                else
                    return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("ForgotPassword");
            }
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
                    Session["MaKH"] = user.MaKH;

                    string fullName = user.TenKH;
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        string firstName = fullName.Trim().Split(' ').Last();
                        Session["TenKH"] = firstName;
                    }
                    else
                    {
                        Session["TenKH"] = "Khách";
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

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