using UserApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using UserApp.Repositories;
using UserApp.Models;
using System.Linq;  

namespace UserApp.Controllers
{
    public class AdminController : AdminBaseController
    {
        public UserService userService;

       
        private QL_PHONGGYMEntities _context = new QL_PHONGGYMEntities();

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
            LoadMetadata(model); // Tải danh sách User/Table/Role
            return View(model);
        }

        // POST: Xử lý nút Cấp quyền (Grant) hoặc Thu hồi (Revoke)
        [HttpPost]
        public ActionResult PhanQuyen(GrantViewModel model, string actionType)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            // 1. Validate dữ liệu
            if (string.IsNullOrEmpty(model.SelectedTable))
            {
                model.Message = "Vui lòng chọn bảng dữ liệu!";
                model.MessageType = "error";
                LoadMetadata(model);
                return View(model);
            }

            string target = !string.IsNullOrEmpty(model.SelectedUser) ? model.SelectedUser : model.SelectedRole;
            if (string.IsNullOrEmpty(target))
            {
                model.Message = "Vui lòng chọn User hoặc Nhóm quyền!";
                model.MessageType = "error";
                LoadMetadata(model);
                return View(model);
            }

            List<string> permissions = new List<string>();
            if (model.Select) permissions.Add("SELECT");
            if (model.Insert) permissions.Add("INSERT");
            if (model.Update) permissions.Add("UPDATE");
            if (model.Delete) permissions.Add("DELETE");

            if (permissions.Count == 0)
            {
                model.Message = "Vui lòng chọn ít nhất một quyền!";
                model.MessageType = "error";
                LoadMetadata(model);
                return View(model);
            }

            string permString = string.Join(", ", permissions);

            // 2. Thực thi Stored Procedure
            try
            {
                string sql = "";
                if (actionType == "GRANT")
                {
                    sql = $"BEGIN sp_grant_permission('{target}', '{model.SelectedTable}', '{permString}'); END;";
                    _context.Database.ExecuteSqlCommand(sql);
                    model.Message = $"Thành công: Đã cấp quyền {permString} trên bảng {model.SelectedTable} cho {target}.";
                    model.MessageType = "success";
                }
                else if (actionType == "REVOKE")
                {
                    sql = $"BEGIN sp_revoke_permission('{target}', '{model.SelectedTable}', '{permString}'); END;";
                    _context.Database.ExecuteSqlCommand(sql);
                    model.Message = $"Thành công: Đã thu hồi quyền {permString} trên bảng {model.SelectedTable} khỏi {target}.";
                    model.MessageType = "warning";
                }
            }
            catch (Exception ex)
            {
                // Bắt lỗi chi tiết từ Oracle
                var realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                {
                    realError += " | Gốc: " + ex.InnerException.InnerException.Message;
                }
                model.Message = "Lỗi Database: " + realError;
                model.MessageType = "error";
            }

            LoadMetadata(model);
            return View(model);
        }

        // Hàm hỗ trợ: Load dữ liệu từ DBA_USERS, DBA_TABLES, DBA_ROLES
        private void LoadMetadata(GrantViewModel model)
        {
            try
            {
                // Load TẤT CẢ Users (cần quyền SELECT ON dba_users)
                model.Users = _context.Database.SqlQuery<string>(
                    "SELECT username FROM SYS.dba_users ORDER BY username"
                ).ToList();

                // Load TẤT CẢ Tables (cần quyền SELECT ON dba_tables)
                // Lọc bỏ các bảng hệ thống
                model.Tables = _context.Database.SqlQuery<string>(
                    "SELECT owner || '.' || table_name FROM SYS.dba_tables WHERE owner NOT IN ('SYS', 'SYSTEM', 'XDB', 'CTXSYS', 'MDSYS') ORDER BY owner, table_name"
                ).ToList();

                // Load TẤT CẢ Roles (cần quyền SELECT ON dba_roles)
                model.Roles = _context.Database.SqlQuery<string>(
                    "SELECT role FROM SYS.dba_roles ORDER BY role"
                ).ToList();
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                model.Message = "Không tải được danh sách từ Oracle (Kiểm tra quyền DBA). Lỗi: " + msg;
                model.MessageType = "error";

                // Dữ liệu giả để tránh crash trang web
                model.Users = new List<string>();
                model.Tables = new List<string>();
                model.Roles = new List<string>();
            }
        }

    }
}