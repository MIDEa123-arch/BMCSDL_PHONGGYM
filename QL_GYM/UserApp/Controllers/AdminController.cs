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

        [HttpPost]
        public ActionResult GrantUserRole(string userToGrant, string roleToGrant, string actionType)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");

            var model = new GrantViewModel();

            if (string.IsNullOrEmpty(userToGrant) || string.IsNullOrEmpty(roleToGrant))
            {
                model.Message = "Vui lòng chọn cả User và Role!";
                model.MessageType = "error";
            }
            else
            {
                if (actionType == "GRANT")
                {
                    _phanQuyenRepository.GrantRoleToUser(userToGrant, roleToGrant, model);
                }
                else if (actionType == "REVOKE")
                {
                    _phanQuyenRepository.RevokeRoleFromUser(userToGrant, roleToGrant, model);
                }
            }
            _phanQuyenRepository.LoadMetadata(model);

            return View("PhanQuyen", model);
        }

        [HttpGet]
        public JsonResult GetUsersInRole(string roleName)
        {
            if (Session["Admin"] == null)
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn." }, JsonRequestBehavior.AllowGet);

            if (string.IsNullOrEmpty(roleName))
                return Json(new { success = false, message = "Chưa chọn chức vụ." }, JsonRequestBehavior.AllowGet);

            try
            {
                var users = _phanQuyenRepository.GetUsersByRole(roleName);
                return Json(new { success = true, data = users }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
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

        [HttpGet]
        public JsonResult GetExistingPrivileges(string target, string tableName)
        {
            // target là SelectedUser hoặc SelectedRole
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(tableName))
            {
                return Json(new { success = false, message = "Thiếu User/Role hoặc Bảng." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // Gọi Repository để lấy danh sách quyền
                var privileges = _phanQuyenRepository.GetExistingPrivileges(target, tableName);

                return Json(new { success = true, data = privileges }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Trả về lỗi
                return Json(new { success = false, message = "Lỗi server khi lấy quyền: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    } 
}