using Microsoft.Ajax.Utilities;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Helpers;
using UserApp.Models;
using UserApp.Repositories;
using UserApp.ViewModel;

namespace UserApp.Controllers
{
    public class StaffController : Controller
    {
        public UserService userService;
        public HoaDonRepository hoaDonRepository;
        public SanPhamRepository _spRepo;
        private QL_PHONGGYMEntities _context;
        public StaffController()
        {         
            _context = new QL_PHONGGYMEntities();
            _spRepo = new SanPhamRepository();
            userService = new UserService();
        }

        public ActionResult HoaDon()
        {
            hoaDonRepository = new HoaDonRepository(Session["connectionString"] as string);
            var list = hoaDonRepository.GetInvoice();
            return View(list);
        }
        public ActionResult SanPham()
        {

            _spRepo = new SanPhamRepository(Session["connectionString"] as string);
            var list = _spRepo.GetAll();
            return View(list);
        }



        [HttpPost]
        public JsonResult CheckSessionAlive()
        {
            if (Session["sid"] == null)
                return Json(new { alive = false, msg = "Mất kết nối với Server." });

            string username = Session["user"].ToString();
            string sid = Session["sid"].ToString();
            string serial = Session["serial"].ToString();

            // Gọi hàm trả về số nguyên (1, 0, -1)
            int status = userService.CheckSessionAlive(username, sid, serial);

            if (status != 1)
            {
                // Xóa sạch session Web
                Session.Clear();
                Session.Abandon();

                string message = "";
                if (status == 0)
                    message = "Phiên làm việc đã hết hạn.";
                else
                    message = "Tài khoản đã đăng xuất.";

                return Json(new { alive = false, msg = message });
            }

            return Json(new { alive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            string username = Session["user"]?.ToString();
            bool result = userService.Logout(username);

            Session.Clear();
            Session.Abandon();

            if (result)
                return RedirectToAction("Login");

            TempData["Error"] = "Lỗi đăng xuất!";
            return RedirectToAction("Index");
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(FormCollection form)
        {
            string username = form["TenDangNhap"];
            string password = form["MatKhau"];

            try
            {
                var loginResult = userService.Login(username, password);

                int status = loginResult.status;
                int sid = loginResult.sid;
                int serial = loginResult.serial;

                if (status == 1)  // Staff
                {
                    Session["user"] = username;
                    Session["connectionString"] = userService.ConnectionStringUser;
                    Session["sid"] = sid;
                    Session["serial"] = serial;
                    Session["loginDate"] = DateTime.Now.ToString("dd/MM/yyyy");

                    return RedirectToAction("HoaDon", "Staff");
                }
                else if (status == 2) // Admin
                {
                    Session["Admin"] = username;
                    return RedirectToAction("NguoiDung", "Admin");
                }
                else
                {
                    TempData["Error"] = "Sai thông tin đăng nhập!";
                    return RedirectToAction("Login");
                }
            }
            catch (OracleException ex)
            {
                if (ex.Number == 1017)
                {
                    TempData["Error"] = "Sai mật khẩu hoặc tài khoản!";
                }
                else if (ex.Number == 28000)
                {
                    TempData["Error"] = "Tài khoản bạn đã bị khóa!";
                }
                else
                {
                    TempData["Error"] = "Lỗi đăng nhập!";
                }

                return RedirectToAction("Login");
            }
        }
        public ActionResult VerifySignatureById(int id)
        {
            hoaDonRepository = new HoaDonRepository(Session["connectionString"] as string);
            // 1. Tìm hóa đơn trong Database
            // Dùng GetInvoice() và tìm kiếm trong danh sách (vì bạn chưa có GetById)
            var invoiceHeader = hoaDonRepository.GetInvoice().FirstOrDefault(i => i.Header.MAHD == id);

            if (invoiceHeader == null)
            {
                ViewBag.Status = "Error";
                ViewBag.Message = $"Không tìm thấy hóa đơn ID = {id} hoặc hóa đơn chưa được ký.";
                return View("VerifySignatureResult");
            }

            try
            {
                // 2. Lấy đường dẫn từ Database
                string dbPdfPath = hoaDonRepository.GetHOADON(id).FILE_PATH;
                string dbSigPath = hoaDonRepository.GetHOADON(id).SIGNATURE_PATH;

                if (string.IsNullOrEmpty(dbSigPath))
                {
                    ViewBag.Status = "Error";
                    ViewBag.Message = "Hóa đơn đã được lưu file, nhưng CHƯA CÓ FILE CHỮ KÝ (.sig).";
                    return View("VerifySignatureResult");
                }

                // 3. Chuyển đường dẫn ảo (Web) thành đường dẫn Vật lý (Server)
                string physicalPdfPath = Server.MapPath("~" + dbPdfPath);
                string physicalSigPath = Server.MapPath("~" + dbSigPath);

                // 4. Kiểm tra file có tồn tại trên server không
                if (!System.IO.File.Exists(physicalPdfPath) || !System.IO.File.Exists(physicalSigPath))
                {
                    ViewBag.Status = "Error";
                    ViewBag.Message = "Không tìm thấy file gốc hoặc file chữ ký trên server (Đường dẫn bị lỗi).";
                    return View("VerifySignatureResult");
                }

                // 5. Cấu hình PFX Admin và gọi Service kiểm tra
                string pfxPath = Server.MapPath("~/App_Data/GymAdmin.pfx");
                string pfxPass = "123456";

                var signService = new DigitalSignService();
                string msgResult = "";

                bool isValid = signService.VerifyFile(physicalPdfPath, physicalSigPath, pfxPath, pfxPass, out msgResult);

                // 6. Trả kết quả ra View
                ViewBag.InvoiceID = id;
                ViewBag.FileName = Path.GetFileName(physicalPdfPath);

                ViewBag.Status = isValid ? "Success" : "Error";
                ViewBag.Message = (isValid ? "✅ HỢP LỆ: " : "❌ KHÔNG HỢP LỆ: ") + msgResult;
            }
            catch (Exception ex)
            {
                ViewBag.Status = "Error";
                ViewBag.Message = "Lỗi hệ thống khi kiểm tra: " + ex.Message;
            }

            return View("VerifySignatureResult");
        }
        [HttpGet]
        public ActionResult ThemSanPham()
        {
            if (Session["user"] == null) return RedirectToAction("Login", "Staff");
            ViewBag.MaLoaiSP = new SelectList(_context.LOAISANPHAMs, "MALOAISP", "TENLOAISP");
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham(SANPHAM model, HttpPostedFileBase MainImage, HttpPostedFileBase[] SubImages)
        {
            if (Session["user"] == null) return RedirectToAction("Login", "Staff");
            _spRepo = new SanPhamRepository(Session["connectionString"] as string);

            List<string> uploadedFiles = new List<string>();

            try
            {
                if (!ModelState.IsValid)
                {
                    throw new Exception("Dữ liệu nhập vào không hợp lệ. Vui lòng kiểm tra lại.");
                }

                string uploadFolder = Server.MapPath("~/Content/Images/");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                List<HINHANH> listImagesDb = new List<HINHANH>();

                if (MainImage != null && MainImage.ContentLength > 0)
                {
                    string uniqueName = $"{DateTime.Now.Ticks}_Main_{MainImage.FileName}";
                    string path = Path.Combine(uploadFolder, uniqueName);

                    MainImage.SaveAs(path);
                    uploadedFiles.Add(path);

                    listImagesDb.Add(new HINHANH { 
                        URL = uniqueName, 
                        ISMAIN = true 
                    });
                }
                else
                {
                    throw new Exception("Vui lòng chọn ảnh chính cho sản phẩm!");
                }
                if (SubImages != null)
                {
                    int count = 0;
                    foreach (var file in SubImages)
                    {
                        if (file != null && file.ContentLength > 0 && count < 3)
                        {
                            string uniqueName = $"{DateTime.Now.Ticks}_Sub_{count}_{file.FileName}";
                            string path = Path.Combine(uploadFolder, uniqueName);

                            file.SaveAs(path);
                            uploadedFiles.Add(path);

                            listImagesDb.Add(new HINHANH { 
                                URL = uniqueName, 
                                ISMAIN = false
                            });
                            count++;
                        }
                    }
                }
                _spRepo.AddWithImages(model, listImagesDb);
                TempData["Success"] = $"Đã thêm sản phẩm '{model.TENSP}' thành công! Mời bạn nhập tiếp sản phẩm mới.";
                ModelState.Clear();
                ViewBag.MaLoaiSP = new SelectList(_context.LOAISANPHAMs, "MALOAISP", "TENLOAISP");
                return View();
            }
            catch (Exception ex)
            {
                foreach (var filePath in uploadedFiles)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch 
                        {
                        }
                    }
                }
                string message = ex.Message;
                if (ex.InnerException != null)
                {
                    message += " | Chi tiết: " + ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        message += " | Gốc: " + ex.InnerException.InnerException.Message;
                    }
                }

                TempData["Error"] = "Có lỗi xảy ra: " + message;
              ViewBag.MaLoaiSP = new SelectList(_context.LOAISANPHAMs, "MALOAISP", "TENLOAISP", model.MALOAISP);
                return View(model);
            }
        }
        // GET: Hiển thị form sửa sản phẩm
        [HttpGet]
        public ActionResult SuaSanPham(int id)
        {
            // Kiểm tra đăng nhập
            if (Session["user"] == null) return RedirectToAction("Login", "Staff");

            try
            {
                // 1. Tìm sản phẩm theo ID
                var sp = _context.SANPHAMs.Find(id);
                if (sp == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm có ID = " + id;
                    return RedirectToAction("SanPham");
                }

                // 2. Load danh sách hình ảnh (FIX lỗi NullReferenceException ở View)
                // Lưu ý: Kiểm tra kỹ tên bảng trong _context là HINHANH hay HINHANHs
                sp.HINHANHs = _context.HINHANHs.Where(x => x.MASP == id).ToList();

                // 3. Tạo Dropdown loại sản phẩm
                // Lưu ý: Value là MALOAISP, Text hiển thị là TENLOAISP (Sửa lỗi DataBinding)
                ViewBag.MaLoaiSP = new SelectList(_context.LOAISANPHAMs, "MALOAISP", "TENLOAISP", sp.MALOAISP);

                return View(sp);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tải dữ liệu: " + ex.Message;
                return RedirectToAction("SanPham");
            }
        }

        // POST: Xử lý cập nhật
        [HttpPost]
        [ValidateInput(false)] // Cho phép nhập HTML trong mô tả
        [ValidateAntiForgeryToken]
        public ActionResult SuaSanPham(SANPHAM model, HttpPostedFileBase MainImage, HttpPostedFileBase[] SubImages)
        {
            // 1. Kiểm tra session
         
            _spRepo = new SanPhamRepository(Session["connectionString"] as string);
            // Khai báo đường dẫn ảnh
            string uploadFolder = Server.MapPath("~/Content/Images/");

            try
            {
                // 2. Kiểm tra dữ liệu đầu vào (Server validation)
                if (!ModelState.IsValid)
                {
                    // Lấy danh sách lỗi chi tiết từ ModelState để hiện ra (nếu cần)
                    string err = string.Join("; ", ModelState.Values
                                               .SelectMany(v => v.Errors)
                                               .Select(e => e.ErrorMessage));
                    throw new Exception("Dữ liệu nhập chưa đúng: " + err);
                }

                // 3. LẤY DỮ LIỆU CŨ TỪ DB (Thay vì dùng Repo Update chung chung)
                // Cách này giúp ta kiểm soát được cột nào được sửa, cột nào không.
                var dbItem = _context.SANPHAMs.Find(model.MASP);
                if (dbItem == null) throw new Exception("Sản phẩm không tồn tại trong hệ thống.");

                // --- CẬP NHẬT THÔNG TIN (TRỪ SỐ LƯỢNG TỒN) ---
                dbItem.TENSP = model.TENSP;
                dbItem.MALOAISP = model.MALOAISP;
                dbItem.DONGIA = model.DONGIA;
                dbItem.GIAKHUYENMAI = model.GIAKHUYENMAI;
                dbItem.HANG = model.HANG;
                dbItem.XUATXU = model.XUATXU;
                dbItem.BAOHANH = model.BAOHANH;
                dbItem.MOTACHUNG = model.MOTACHUNG;
                dbItem.MOTACHITIET = model.MOTACHITIET; // Chuỗi đã nối dấu |

                // QUAN TRỌNG: KHÔNG cập nhật dbItem.SOLUONGTON
                // dbItem.SOLUONGTON = model.SOLUONGTON; // <-- Bỏ dòng này đi

                // 4. XỬ LÝ ẢNH CHÍNH (Nếu có upload mới)
                if (MainImage != null && MainImage.ContentLength > 0)
                {
                    // Tìm ảnh chính cũ
                    var oldMain = _context.HINHANHs.FirstOrDefault(x => x.MASP == model.MASP && x.ISMAIN == true);

                    // Xóa ảnh cũ (DB + File vật lý)
                    if (oldMain != null)
                    {
                        string oldPath = Path.Combine(uploadFolder, oldMain.URL);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        _context.HINHANHs.Remove(oldMain);
                    }

                    // Lưu ảnh mới
                    string uniqueName = $"{DateTime.Now.Ticks}_Main_{MainImage.FileName}";
                    MainImage.SaveAs(Path.Combine(uploadFolder, uniqueName));

                    // Thêm record vào DB
                    _context.HINHANHs.Add(new HINHANH { MASP = model.MASP, URL = uniqueName, ISMAIN = true });
                }

                // 5. XỬ LÝ ẢNH PHỤ (Nếu có upload mới)
                if (SubImages != null && SubImages[0] != null)
                {
                    // Tìm và xóa toàn bộ ảnh phụ cũ
                    var oldSubs = _context.HINHANHs.Where(x => x.MASP == model.MASP && x.ISMAIN == false).ToList();
                    foreach (var img in oldSubs)
                    {
                        string oldPath = Path.Combine(uploadFolder, img.URL);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        _context.HINHANHs.Remove(img);
                    }

                    // Thêm loạt ảnh phụ mới
                    int count = 0;
                    foreach (var file in SubImages)
                    {
                        if (file != null && file.ContentLength > 0 && count < 3)
                        {
                            string uniqueName = $"{DateTime.Now.Ticks}_Sub_{count}_{file.FileName}";
                            file.SaveAs(Path.Combine(uploadFolder, uniqueName));
                            _context.HINHANHs.Add(new HINHANH { MASP = model.MASP, URL = uniqueName, ISMAIN = false });
                            count++;
                        }
                    }
                }

                // 6. LƯU TẤT CẢ VÀO DATABASE
                _context.SaveChanges();

                TempData["Success"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("SanPham","Staff"); // Quay về danh sách
            }
            catch (Exception ex)
            {
                // 1. Logic tìm lỗi gốc (Deep dive)
                // Biến để xác định xem có phải lỗi 1031 không
                bool isPrivilegeError = false;

                // Tạo biến tạm để duyệt qua các lớp Exception con
                Exception tempEx = ex;
                while (tempEx != null)
                {
                    // Kiểm tra nếu exception hiện tại là OracleException
                    if (tempEx is Oracle.ManagedDataAccess.Client.OracleException oracleEx)
                    {
                        if (oracleEx.Number == 1031) // Bắt đúng mã 1031
                        {
                            isPrivilegeError = true;
                            break; // Tìm thấy rồi thì thoát vòng lặp
                        }
                    }
                    // Nếu không phải, đi tiếp vào bên trong
                    tempEx = tempEx.InnerException;
                }

                // 2. Xử lý thông báo lỗi ra màn hình
                if (isPrivilegeError)
                {
                    TempData["Error"] = "Tài khoản của bạn không đủ quyền để thực hiện thao tác này.";
                }
                else
                {
                    // Nếu không phải lỗi 1031 thì hiện lỗi chi tiết như bình thường
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += " | Chi tiết: " + ex.InnerException.Message;
                        // Đào thêm 1 cấp nữa nếu có
                        if (ex.InnerException.InnerException != null)
                        {
                            message += " | Gốc: " + ex.InnerException.InnerException.Message;
                        }
                    }
                    TempData["Error"] = "CÓ LỖI XẢY RA: " + message;
                }

                // 3. Load lại Dropdown và trả về View
                ViewBag.MaLoaiSP = new SelectList(_context.LOAISANPHAMs, "MALOAISP", "TENLOAISP", model.MALOAISP);
                return View(model);
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