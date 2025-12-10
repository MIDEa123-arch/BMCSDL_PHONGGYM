using System;
using System.Collections.Generic;
using System.Linq;  
using System.Web.Mvc;
using UserApp.Models;
using UserApp.Repositories;
using UserApp.ViewModel;
using Oracle.ManagedDataAccess.Client;
using System.Data;
namespace UserApp.Controllers
{
    public class AdminController : AdminBaseController
    {
        public UserService userService;

       
        private QL_PHONGGYMEntities _context = new QL_PHONGGYMEntities();
        private readonly PhanQuyenRepository _phanQuyenRepository;

        public AdminController()
        {
            _phanQuyenRepository = new PhanQuyenRepository();
            userService = new UserService();
        }

        [HttpGet]
        public JsonResult GetUserSessions(string username)
        {
            try
            {
                // Gọi Service lấy list session
                var sessions = userService.GetUserSessions(username);

                // Trả về JSON cho Frontend vẽ bảng
                return Json(new { success = true, data = sessions }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        
        [HttpPost]
        public JsonResult KillSession(int sid, int serial)
        {
            if (Session["Admin"] == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập Admin đã hết hạn." });
            }

            try
            {                
                bool result = userService.KillSession(sid, serial);

                if (result)
                {
                    return Json(new { success = true, message = "Đã hủy phiên làm việc thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy phiên này." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi hủy session: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaNguoiDung(FormCollection form)
        {
            try
            {
                string username = form["username"];
                bool result = userService.DeleteUser(username);
                if (result) TempData["Success"] = "Xóa tài khoản thành công.";
                else TempData["Error"] = "Không thể xóa tài khoản.";
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
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");
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
                if (result) TempData["Success"] = "Đã tạo tài khoản thành công.";
                else TempData["Error"] = "Không thể thêm tài khoản.";
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

                if (locked) result = userService.UnlockUser(username);
                else result = userService.LockUser(username);

                if (result) TempData["Success"] = locked ? $"Đã mở khóa {username}." : $"Đã khóa {username}.";
                else TempData["Error"] = "Không thể thay đổi trạng thái.";
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
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");
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

        public ActionResult Index() => View();
        public ActionResult About() { ViewBag.Message = "Description page."; return View(); }
        public ActionResult Contact() { ViewBag.Message = "Contact page."; return View(); }

    

        // GET: Hiển thị form Phân quyền
        [HttpGet]
        public ActionResult PhanQuyen()
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            var model = new GrantViewModel();
            _phanQuyenRepository.LoadMetadata(model); // Tải danh sách User/Table/Role
            return View(model);
        }

        // POST: Xử lý nút Cấp quyền (Grant) hoặc Thu hồi (Revoke)
        [HttpPost]
        public ActionResult PhanQuyen(GrantViewModel model, string actionType)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            // 1. Validate: Chọn Bảng
            if (string.IsNullOrEmpty(model.SelectedTable))
            {
                model.Message = "Vui lòng chọn bảng dữ liệu!";
                model.MessageType = "error";
                _phanQuyenRepository.LoadMetadata(model);
                return View(model);
            }

            // 2. Validate: Chọn User hoặc Role
            string target = !string.IsNullOrEmpty(model.SelectedUser) ? model.SelectedUser : model.SelectedRole;
            if (string.IsNullOrEmpty(target))
            {
                model.Message = "Vui lòng chọn User hoặc Nhóm quyền!";
                model.MessageType = "error";
                _phanQuyenRepository.LoadMetadata(model);
                return View(model);
            }

            // 3. Validate: Chọn ít nhất 1 quyền
            List<string> permissions = new List<string>();
            if (model.Select) permissions.Add("SELECT");
            if (model.Insert) permissions.Add("INSERT");
            if (model.Update) permissions.Add("UPDATE");
            if (model.Delete) permissions.Add("DELETE");

            if (permissions.Count == 0)
            {
                model.Message = "Vui lòng chọn ít nhất một quyền!";
                model.MessageType = "error";
                _phanQuyenRepository.LoadMetadata(model);
                return View(model);
            }

            string permString = string.Join(", ", permissions);

            // 4. GỌI REPOSITORY ĐỂ THỰC THI (Đã sửa lỗi Connection tại đây)
            // Thay vì viết OracleCommand dài dòng ở đây, ta gọi hàm đã viết trong Repository
            _phanQuyenRepository.UpdatePermission(actionType, target, model.SelectedTable, permString, model);

            // 5. Load lại dữ liệu Dropdown
            _phanQuyenRepository.LoadMetadata(model);
            return View(model);
        }
    }
}