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

        [HttpGet]
        public ActionResult PhanQuyen()
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            var model = new GrantViewModel();
            _phanQuyenRepository.LoadMetadata(model); 
            return View(model);
        }

        [HttpPost]
        public ActionResult PhanQuyen(GrantViewModel model, string actionType)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            if (string.IsNullOrEmpty(model.SelectedTable))
            {
                model.Message = "Vui lòng chọn bảng dữ liệu!";
                model.MessageType = "error";
                _phanQuyenRepository.LoadMetadata(model);
                return View(model);
            }

            string target = !string.IsNullOrEmpty(model.SelectedUser) ? model.SelectedUser : model.SelectedRole;
            if (string.IsNullOrEmpty(target))
            {
                model.Message = "Vui lòng chọn User hoặc Nhóm quyền!";
                model.MessageType = "error";
                _phanQuyenRepository.LoadMetadata(model);
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
                _phanQuyenRepository.LoadMetadata(model);
                return View(model);
            }

            string permString = string.Join(", ", permissions);

            _phanQuyenRepository.UpdatePermission(actionType, target, model.SelectedTable, permString, model);

            _phanQuyenRepository.LoadMetadata(model);
            return View(model);
        }

        private void LoadMetadata(GrantViewModel model)
        {
            try
            {
                model.Users = _context.Database.SqlQuery<string>(
                    "SELECT username FROM SYS.dba_users ORDER BY username"
                ).ToList();

                model.Tables = _context.Database.SqlQuery<string>(
                    "SELECT owner || '.' || table_name FROM SYS.dba_tables WHERE owner NOT IN ('SYS', 'SYSTEM', 'XDB', 'CTXSYS', 'MDSYS') ORDER BY owner, table_name"
                ).ToList();

                model.Roles = _context.Database.SqlQuery<string>(
                    "SELECT role FROM SYS.dba_roles ORDER BY role"
                ).ToList();
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                model.Message = "Không tải được danh sách từ Oracle (Kiểm tra quyền DBA). Lỗi: " + msg;
                model.MessageType = "error";

                model.Users = new List<string>();
                model.Tables = new List<string>();
                model.Roles = new List<string>();
            }
        }
        public ActionResult AuditTrail(string username, string tableName) 
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            var model = new AuditFilterViewModel();

            try
            {
                var allUsers = userService.GetAllUsers();
                model.Users = allUsers.Select(u => new SelectListItem
                {
                    Value = u.Username,
                    Text = u.Username,
                    Selected = u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                }).ToList();
                model.Users.Insert(0, new SelectListItem { Value = "", Text = "--- Tất cả Users ---", Selected = string.IsNullOrEmpty(username) });
                var allTables = userService.GetAuditedTables();
                model.Tables = allTables.Select(t => new SelectListItem
                {
                    Value = t,
                    Text = t,
                    Selected = t.Equals(tableName, StringComparison.OrdinalIgnoreCase)
                }).ToList();
                model.Tables.Insert(0, new SelectListItem { Value = "", Text = "--- Tất cả Bảng ---", Selected = string.IsNullOrEmpty(tableName) });
                model.SelectedUsername = username;
                model.SelectedTableName = tableName;
                model.AuditLogs = userService.GetAuditLogs(username, tableName);

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải Audit Trail: " + ex.Message;
                return View(model);
            }
        }
        [HttpGet]
        public JsonResult GetExistingPrivileges(string target, string tableName)
        {
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(tableName))
            {
                return Json(new { success = false, message = "Thiếu User/Role hoặc Bảng." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var privileges = _phanQuyenRepository.GetExistingPrivileges(target, tableName);

                return Json(new { success = true, data = privileges }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server khi lấy quyền: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRole(GrantViewModel model)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            if (string.IsNullOrWhiteSpace(model.NewRoleName))
            {
                TempData["Error"] = "Vui lòng nhập tên Role.";
                return RedirectToAction("PhanQuyen");
            }

            try
            {
                _phanQuyenRepository.CreateNewRole(model.NewRoleName.Trim().ToUpper());
                TempData["Success"] = $"Đã tạo Role '{model.NewRoleName}' thành công. Vui lòng reload trang để cập nhật danh sách Role.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tạo Role: " + ex.Message;
            }
            return RedirectToAction("PhanQuyen");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignRole(GrantViewModel model)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            if (string.IsNullOrWhiteSpace(model.UserToAssign) || string.IsNullOrWhiteSpace(model.RoleToAssign))
            {
                TempData["Error"] = "Vui lòng chọn User và Role để gán.";
                return RedirectToAction("PhanQuyen");
            }

            try
            {
                _phanQuyenRepository.AddUserToRole(model.UserToAssign, model.RoleToAssign);
                TempData["Success"] = $"Đã gán Role '{model.RoleToAssign}' cho User '{model.UserToAssign}' thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi gán Role: " + ex.Message;
            }
            return RedirectToAction("PhanQuyen");
        }
    } 
}