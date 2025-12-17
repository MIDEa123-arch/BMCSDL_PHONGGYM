using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;  
using System.Text;
using System.Web;
using System.Web.Mvc;
using UserApp.Helpers;
using UserApp.Models;
using UserApp.Repositories;
using UserApp.ViewModel;


namespace UserApp.Controllers
{
    public class AdminController : AdminBaseController
    {
        public UserService userService;


        private QL_PHONGGYMEntities _context = new QL_PHONGGYMEntities(true);
        private readonly PhanQuyenRepository _phanQuyenRepository;
        private readonly KhachHangRepository _khachHang;

        public AdminController()
        {
            _khachHang = new KhachHangRepository();
            _phanQuyenRepository = new PhanQuyenRepository();
            userService = new UserService();
        }

        public ActionResult DecryptReport()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DecryptReport(HttpPostedFileBase encryptedFile)
        {
            if (encryptedFile == null || encryptedFile.ContentLength == 0)
            {
                ViewBag.Error = "Vui lòng chọn file .secure cần giải mã.";
                return View();
            }

            string tempFolder = Server.MapPath("~/App_Data/Temp/");
            if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

            // Đặt tên file tạm
            string uniqueId = Guid.NewGuid().ToString();
            string inputPath = Path.Combine(tempFolder, uniqueId + ".secure");
            string outputPath = Path.Combine(tempFolder, uniqueId + ".csv");
            string privateKeyPath = Server.MapPath("~/App_Data/private_rsa_key.xml");

            try
            {
                // A. Lưu file upload xuống server tạm thời
                encryptedFile.SaveAs(inputPath);

                // B. Kiểm tra file Private Key có tồn tại không
                if (!System.IO.File.Exists(privateKeyPath))
                {
                    ViewBag.Error = "Không tìm thấy file Private Key trên server (App_Data/private_rsa_key.xml).";
                    return View();
                }

                // Đọc Private Key
                string privateKeyXml = System.IO.File.ReadAllText(privateKeyPath);

                // Tiến hành giải mã
                var crypto = new DesRsaCrypto();
                crypto.DecryptFile(inputPath, outputPath, privateKeyXml);

                // Đọc nội dung file CSV đã giải mã
                List<string[]> csvData = new List<string[]>();

                // Đọc file CSV 
                using (var fs = new FileStream(outputPath, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var values = line.Split(',');
                            csvData.Add(values);
                        }
                    }
                }

                // Gửi dữ liệu sang View
                ViewBag.CsvData = csvData;
                ViewBag.Success = "Giải mã thành công!";
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi giải mã: " + ex.Message + " (Kiểm tra lại xem File upload có đúng là file được mã hóa bởi Public Key tương ứng không)";
            }
            finally
            {
                // Dọn dẹp file tạm để bảo mật và tiết kiệm dung lượng
                if (System.IO.File.Exists(inputPath)) System.IO.File.Delete(inputPath);
                if (System.IO.File.Exists(outputPath)) System.IO.File.Delete(outputPath);
            }

            return View();
        }
        public ActionResult KhachHang()
        {           
            return View(_khachHang.GetMyCustomers());

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

            var viewModel = new NhanVienViewModel
            {
                ChucVuList = new SelectList(_context.CHUCVUs, "MaChucVu", "TenChucVu"),
                ChuyenMonList = _context.CHUYENMONs.ToList()
            };

            return View(viewModel);
        }

        // 2. Action POST: Xử lý dữ liệu gửi lên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemNguoiDung(NhanVienViewModel viewModel)
        {
            if (Session["Admin"] == null) return RedirectToAction("Login", "Staff");
            if (ModelState.IsValid)
            {
                bool trungUser = _context.NHANVIENs.Any(x => x.TENDANGNHAP == viewModel.TenDangNhap);
                if (trungUser)
                {
                    ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại!");
                }

                // Kiểm tra trùng SĐT
                bool trungSDT = _context.NHANVIENs.Any(x => x.SDT == viewModel.SDT);
                if (trungSDT)
                {
                    ModelState.AddModelError("SDT", "Số điện thoại này đã được sử dụng!");
                }
            }

            if (ModelState.IsValid)
            {
                bool result = userService.AddUser(viewModel);

                if (result)
                {
                    if (viewModel.SelectedChuyenMonIds != null && viewModel.SelectedChuyenMonIds.Count > 0)
                    {
                        var newNV = _context.NHANVIENs.FirstOrDefault(x => x.TENDANGNHAP == viewModel.TenDangNhap.ToUpper());

                        if (newNV != null)
                        {
                            if (newNV.CHUYENMONs == null) newNV.CHUYENMONs = new HashSet<CHUYENMON>();

                            var selectedChuyenMons = _context.CHUYENMONs
                                .Where(cm => viewModel.SelectedChuyenMonIds.Contains((int)cm.MACM))
                                .ToList();

                            foreach (var cm in selectedChuyenMons)
                            {
                                newNV.CHUYENMONs.Add(cm);
                            }
                            _context.SaveChanges();
                        }
                    }

                    TempData["Success"] = "Thêm nhân viên và tạo User Oracle thành công!";
                    return RedirectToAction("NguoiDung");
                }
                else
                {
                    TempData["Error"] = "Lỗi: Không thể tạo tài khoản (Có thể trùng tên User Oracle).";
                }
            }
            viewModel.ChucVuList = new SelectList(_context.CHUCVUs, "MaChucVu", "TenChucVu", viewModel.MaChucVu);
            viewModel.ChuyenMonList = _context.CHUYENMONs.ToList();

            return View(viewModel);
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
            try
            {
                var privs = _phanQuyenRepository.GetExistingPrivileges(target, tableName);

                return Json(new { success = true, data = privs }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult ViewSecureFile(string fileName)
        {            
            if (Session["Admin"] == null)
                return RedirectToAction("Login", "Staff");

            try
            {
                string securePath = Server.MapPath("~/SecureFiles/" + fileName);
                string outputPath = Server.MapPath("~/Temp/" + Path.GetFileNameWithoutExtension(fileName));

                string privateKeyPath = Server.MapPath("~/App_Data/private_rsa_key.xml");

                string privateKeyXml = System.IO.File.ReadAllText(privateKeyPath);


                DesRsaCrypto crypto = new DesRsaCrypto();
                crypto.DecryptFile(securePath, outputPath, privateKeyXml);
                string content = System.IO.File.ReadAllText(outputPath);
                System.IO.File.Delete(outputPath);

                return Content(content, "text/plain");
            }
            catch (Exception ex)
            {
                return Content("Lỗi giải mã: " + ex.Message);
            }
        }
    } 


}