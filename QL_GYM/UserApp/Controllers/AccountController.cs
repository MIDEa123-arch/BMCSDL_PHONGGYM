using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Models;
using UserApp.Repositories;
using UserApp.ViewModel;

namespace UserApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        private readonly AccountRepository _accountRepo;

        public AccountController()
        {
            _accountRepo = new AccountRepository(new QL_PHONGGYMEntities());
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


        // GET: /Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Account");
        }
    }
}