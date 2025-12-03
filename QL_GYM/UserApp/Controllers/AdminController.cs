using UserApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using UserApp.Repositories;

namespace UserApp.Controllers
{
    public class AdminController : Controller
    {
        public UserService userService;

        public AdminController()
        {
            userService = new UserService();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaNguoiDung(FormCollection form)
        {
            try
            {
                string username = form["username"];

                bool result = userService.DeleteUser(username);
                if (result)
                {
                    TempData["Success"] = "Xóa tài khoản thành công.";
                }
                else
                {
                    TempData["Error"] = "Không thể xóa tài khoản.";
                }


                return RedirectToAction("NguoiDung");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("NguoiDung");
            }
        }

        public ActionResult ThemNguoiDung()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemNguoiDung(FormCollection form)
        {
            try
            {
                string username = form["TenDangNhap"];
                string password = form["MatKhau"];

                bool result = userService.AddUser(username, password);
                if (result)
                {
                    TempData["Success"] = "Đã tạo tài khoản thành công.";
                }
                else
                {
                    TempData["Error"] = "Không thể thêm tài khoản.";
                }


                return RedirectToAction("NguoiDung");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("NguoiDung");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TrangThai(FormCollection form)
        {
            try
            {
                string username = form["username"];
                string isLocked = form["isLocked"];

                bool result;
                bool locked = isLocked == "true";

                if (locked)
                    result = userService.UnlockUser(username);
                else
                    result = userService.LockUser(username);

                if (result)
                {
                    TempData["Success"] = locked
                        ? $"Đã mở khóa tài khoản {username}."
                        : $"Đã khóa tài khoản {username}.";
                }
                else
                {
                    TempData["Error"] = "Không thể thay đổi trạng thái tài khoản.";
                }

                return RedirectToAction("NguoiDung");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return RedirectToAction("NguoiDung");
            }
        }

        public ActionResult NguoiDung()
        {
            try
            {
                List<UserInfo> users = userService.GetAllUsers();

                return View(users);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View(new List<UserInfo>());
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
