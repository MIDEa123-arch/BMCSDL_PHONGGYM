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
        public ActionResult AuditTrail(string username)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            var model = new AuditFilterViewModel();

            try
            {
                // 1. Lấy danh sách Users cho ComboBox
                var allUsers = userService.GetAllUsers();

                // Chuyển đổi List<UserInfo> sang List<SelectListItem>
                model.Users = allUsers.Select(u => new SelectListItem
                {
                    Value = u.Username,
                    Text = u.Username,
                    Selected = u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)
                }).ToList();

                // Thêm mục "Tất cả Users" vào đầu danh sách
                model.Users.Insert(0, new SelectListItem { Value = "", Text = "--- Tất cả Users ---", Selected = string.IsNullOrEmpty(username) });

                // 2. Thiết lập Username đã chọn (nếu có)
                model.SelectedUsername = username;

                // 3. Lấy dữ liệu Audit Logs (có lọc nếu username khác rỗng)
                model.AuditLogs = userService.GetAuditLogs(username);

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi khi tải Audit Trail: " + ex.Message;
                // Trả về model rỗng nếu có lỗi
                return View(model);
            }
        }
    }
}