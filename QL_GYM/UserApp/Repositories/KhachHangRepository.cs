using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using UserApp.Helpers;
using UserApp.Models;
using UserApp.ViewModel;

namespace UserApp.Repositories
{
    public class KhachHangRepository
    {
        private readonly QL_PHONGGYMEntities _context;

        public KhachHangRepository(QL_PHONGGYMEntities context)
        {
            _context = context;
        }
        public KhachHangRepository(string connStr)
        {
            _context = new QL_PHONGGYMEntities(connStr);
        }

        public KhachHangRepository()
        {
            _context = new QL_PHONGGYMEntities(true);
        }
        public List<NHANVIEN> GetDanhSachHLV()
        {            
            return _context.NHANVIENs.Where(nv => nv.MACHUCVU == 2).ToList();
        }

        public bool PhanCongPT(int maKH, int maNV, int soBuoi, decimal giaMoiBuoi)
        {
            try
            {
                var kh = _context.KHACHHANGs.Find(maKH);
                var nv = _context.NHANVIENs.Find(maNV);

                if (kh == null || nv == null)
                {
                    return false;
                }

                var dangKy = new DANGKYPT
                {
                    MAKH = maKH,
                    MANV = maNV,
                    SOBUOI = soBuoi,
                    GIAMOIBUOI = giaMoiBuoi,
                    NGAYDANGKY = DateTime.Now,
                    TRANGTHAI = "Còn hiệu lực"
                };

                _context.DANGKYPTs.Add(dangKy);
                _context.SaveChanges();

                return true;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                var fullErrorMessage = string.Join("; ", errorMessages);
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);
                throw new Exception(exceptionMessage);
            }
            catch (Exception ex)
            {                
                throw ex;
            }
        }
        public bool SendOTP(string userEmail, out string otpCode)
        {
            string emailDaMaHoa = MaHoa.MaHoaCong(userEmail, 6);
            
            var khach = _context.KHACHHANGs.FirstOrDefault(x => x.EMAIL == emailDaMaHoa);
            if (khach == null)
                throw new Exception("Tài khoản không tồn tại");

            otpCode = new Random().Next(1000, 9999).ToString(); // Tạo mã OTP 6 số
            try
            {
                var fromAddress = new MailAddress("nguyenngadeptrai2005@gmail.com", "Gym");
                var toAddress = new MailAddress(userEmail);

                const string appPassword = "ridfwvtnwuukyece"; // Gmail App Password

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, appPassword)
                };

                string bodyContent = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; background:#f6f6f6'>
                    <div style='max-width: 500px; margin: auto; background: white; padding: 25px; border-radius: 10px; box-shadow: 0 3px 10px rgba(0,0,0,0.1);'>
                        <h2 style='color:#d92027; text-align:center; margin-bottom: 20px;'>THE GYM</h2>

                        <h3 style='color:#333;'>Khôi phục tài khoản</h3>
                        <p style='font-size: 15px; color:#555;'>
                            Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản The Gym.
                            Vui lòng sử dụng mã OTP bên dưới để xác thực:
                        </p>

                        <div style='text-align:center; margin: 30px 0;'>
                            <div style='
                                display:inline-block;
                                font-size:32px;
                                font-weight:bold;
                                color:white;
                                background:#d92027;
                                padding:15px 35px;
                                border-radius:8px;
                                letter-spacing:5px;
                            '>
                                {otpCode}
                            </div>
                        </div>

                        <p style='color:#777;'>
                            Mã OTP có hiệu lực trong <b>3 phút</b>.
                        </p>

                        <p style='color:#444;'>
                            Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email.
                        </p>

                        <hr style='margin-top:25px; opacity:0.3;' />
                        <p style='text-align:center; font-size:13px; color:#999;'>
                            © {DateTime.Now.Year} The Gym. All rights reserved.
                        </p>
                    </div>
                </div>";

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "THE GYM - Mã xác thực OTP khôi phục tài khoản",
                    Body = bodyContent,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public List<HocVienViewModel> GetHocVien()
        {
            var list = (from kh in _context.KHACHHANGs
                        join dk in _context.DANGKYPTs on kh.MAKH equals dk.MAKH
                        join nv in _context.NHANVIENs on dk.MANV equals nv.MANV                       
                        select new HocVienViewModel
                        {
                            MaKH = (int)kh.MAKH,
                            TenKH = kh.TENKH,
                            GioiTinh = kh.GIOITINH,
                            SDT = kh.SDT,
                            Email = kh.EMAIL,
                            SoBuoi = (int)(dk.SOBUOI),
                            TrangThai = dk.TRANGTHAI
                        }).ToList();
            
            foreach (var item in list)
            {
                item.SDT = GiaiMa.GiaiMaCong(item.SDT, 6);
                item.Email = GiaiMa.GiaiMaCong(item.Email, 6);
            }

            return list;
        }
        public List<KHACHHANG> GetMyCustomers()
        {
            var list = _context.KHACHHANGs.ToList();
            
            foreach (var item in list)
            {
                try
                {
                    item.SDT = GiaiMa.GiaiMaCong(item.SDT, 6);
                    item.EMAIL = GiaiMa.GiaiMaCong(item.EMAIL, 6);
                }
                catch
                {                    
                }
            }

            return list;
        }
        public KHACHHANG ThongTinKH(int makh)
        {
            var kh = _context.KHACHHANGs.FirstOrDefault(k => k.MAKH == makh);
            if (kh != null)
            {     
                if (kh.EMAIL != null) kh.EMAIL = GiaiMa.GiaiMaCong(kh.EMAIL, 6);
                if (kh.SDT != null) kh.SDT = GiaiMa.GiaiMaCong(kh.SDT, 6);
            }
            return kh;

        }
        public LOAIKHACHHANG LoaiKh(int maloai)
        {
            return _context.LOAIKHACHHANGs.FirstOrDefault(kh => kh.MALOAIKH == maloai);

        }

        public DIACHI GetDiaChi(int makh)
        {
            var diaChiList = _context.DIACHIs.Where(dc => dc.MAKH == makh).OrderByDescending(dc => dc.NGAYTHEM).ToList();

            if (!diaChiList.Any())
                return null;

            var diaChi = diaChiList.FirstOrDefault(dc => dc.LADIACHIMACDINH == true);

            return diaChi ?? diaChiList.First();
        }


        public void ThemDiaChi(int makh, FormCollection form)
        {
            string tinh = form["province"];
            string huyen = form["district"];
            string xa = form["ward"];
            string diaChiCuThe = form["address"];

            if (string.IsNullOrEmpty(tinh) || string.IsNullOrEmpty(huyen) || string.IsNullOrEmpty(xa)) return;

            var diaChiTonTai = _context.DIACHIs
                .FirstOrDefault(dc =>
                    dc.MAKH == makh &&
                    dc.TINHTHANHPHO == tinh &&
                    dc.QUANHUYEN == huyen &&
                    dc.PHUONGXA == xa &&
                    dc.DIACHICUTHE == diaChiCuThe);

            if (diaChiTonTai == null)
            {
                var diaChiMoi = new DIACHI
                {
                    MAKH = makh,
                    TINHTHANHPHO = tinh,
                    QUANHUYEN = huyen,
                    PHUONGXA = xa,
                    DIACHICUTHE = diaChiCuThe,
                    LADIACHIMACDINH = false,                    
                };

                _context.DIACHIs.Add(diaChiMoi);               
            }
            else
            {
                diaChiTonTai.NGAYTHEM = DateTime.Now;
                _context.Entry(diaChiTonTai).State = EntityState.Modified;
            }
            _context.SaveChanges();
        }
    }
}