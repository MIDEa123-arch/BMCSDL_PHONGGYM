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
        // GET: Hiển thị form sửa
        // File: StaffController.cs (Action SuaSanPham - HTTP GET)
        [HttpGet]
        public ActionResult SuaSanPham(int id)
        {
            if (Session["user"] == null || Session["connectionString"] == null) return RedirectToAction("Login", "Staff");

            string connStr = Session["connectionString"].ToString();

            using (var userContext = new QL_PHONGGYMEntities(connStr))
            {
                try
                {
                    // Tải sản phẩm VÀ ép tải luôn các bảng liên quan (LOAISANPHAM, HINHANHS)
                    var sp = userContext.SANPHAMs
                        // .Include("LOAISANPHAM") // Thử nếu cách dưới không hoạt động
                        // .Include("HINHANHs")    // Thử nếu cách dưới không hoạt động
                        .FirstOrDefault(s => s.MASP == id);

                    if (sp == null)
                    {
                        TempData["Error"] = "Không tìm thấy sản phẩm.";
                        return RedirectToAction("SanPham");
                    }

                    // Nếu bạn không muốn dùng .Include, hãy ép tải dữ liệu như sau:
                    // 1. Ép tải LOAISANPHAM (FIX lỗi Model.LOAISANPHAM.TENLOAISP)
                    var tenLoai = sp.LOAISANPHAM.TENLOAISP;

                    // 2. Ép tải HINHANHS (FIX lỗi Model.HINHANHs)
                    sp.HINHANHs = userContext.HINHANHs.Where(x => x.MASP == id).ToList();

                    // FIX lỗi DataBinding: TENLOAI -> TENLOAISP
                    ViewBag.MaLoaiSP = new SelectList(userContext.LOAISANPHAMs.ToList(), "MALOAISP", "TENLOAISP", sp.MALOAISP);

                    return View(sp);
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Lỗi khi tải dữ liệu: " + ex.Message;
                    return RedirectToAction("SanPham");
                }
            }
        }

        // POST: Xử lý cập nhật CHỈ TÊN VÀ GIÁ
        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public ActionResult SuaSanPham(SANPHAM model, HttpPostedFileBase MainImage, HttpPostedFileBase[] SubImages)
        {
            // 1. Kiểm tra session
            if (Session["user"] == null || Session["connectionString"] == null)
                return RedirectToAction("Login", "Staff");

            string connStr = Session["connectionString"].ToString();

            // Khởi tạo Context với user hiện tại
            using (var userContext = new QL_PHONGGYMEntities(connStr))
            {
                // Sử dụng Transaction
                using (var transaction = userContext.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra dữ liệu đầu vào cơ bản
                        if (string.IsNullOrEmpty(model.TENSP) || model.DONGIA <= 0 || !ModelState.IsValid)
                            throw new Exception("Vui lòng nhập Tên sản phẩm và Đơn giá hợp lệ.");

                        // 2. TÌM DỮ LIỆU CŨ VÀ CẬP NHẬT
                        var dbItem = userContext.SANPHAMs.Find(model.MASP);
                        if (dbItem == null) throw new Exception("Sản phẩm không tồn tại.");

                        // --- CHỈ CẬP NHẬT 3 THUỘC TÍNH NÀY ---
                        dbItem.TENSP = model.TENSP;
                        dbItem.DONGIA = model.DONGIA;
                        dbItem.GIAKHUYENMAI = model.GIAKHUYENMAI;

                        // KHÔNG SỬA: MALOAISP, SOLUONGTON, HANG, XUATXU, MOTACHITIET, HINHANHs

                        // 3. LƯU VÀ HOÀN TẤT
                        userContext.SaveChanges();
                        transaction.Commit();

                        TempData["Success"] = "Cập nhật Tên và Giá sản phẩm thành công!";
                        return View(model);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        // 1. Logic bóc tách lỗi Oracle (ORA-01031)
                        string message = ex.Message;
                        bool is1031 = false;
                        Exception tempEx = ex;

                        while (tempEx != null)
                        {
                            if (tempEx is Oracle.ManagedDataAccess.Client.OracleException oracleEx)
                            {
                                if (oracleEx.Number == 1031)
                                {
                                    is1031 = true;
                                    break;
                                }
                            }
                            tempEx = tempEx.InnerException;
                        }

                        // 2. Xử lý thông báo
                        if (is1031)
                        {
                            message = "LỖI QUYỀN HẠN (ORA-01031): Tài khoản " + Session["user"] + " không đủ quyền để cập nhật sản phẩm này. Vui lòng kiểm tra quyền DELETE/UPDATE trên bảng SANPHAM và HINHANH.";
                        }
                        else
                        {
                            // Nếu không phải 1031, hiển thị lỗi chi tiết
                            // (Code này giữ nguyên logic hiển thị lỗi gốc đã viết trước đó)
                            tempEx = ex;
                            while (tempEx.InnerException != null) tempEx = tempEx.InnerException;
                            message = "Lỗi Database: " + tempEx.Message;
                        }

                        TempData["Error"] = "CÓ LỖI XẢY RA: " + message;

                        // 3. Tải lại dữ liệu cho Dropdown và trả về View
                        using (var freshContext = new QL_PHONGGYMEntities(connStr))
                        {
                            ViewBag.MaLoaiSP = new SelectList(freshContext.LOAISANPHAMs.ToList(), "MALOAISP", "TENLOAISP", model.MALOAISP);
                        }

                        // Cần tải lại item gốc để View không bị lỗi
                        var currentItem = userContext.SANPHAMs.Find(model.MASP);
                        if (currentItem != null) currentItem.HINHANHs = userContext.HINHANHs.Where(x => x.MASP == currentItem.MASP).ToList();

                        return View(currentItem ?? model);
                    }
                }
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