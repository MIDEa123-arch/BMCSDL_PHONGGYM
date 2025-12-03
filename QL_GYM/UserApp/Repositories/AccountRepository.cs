using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.Linq;
using UserApp.Models;
using UserApp.ViewModel;
using UserApp.Helpers;
namespace UserApp.Repositories
{
    public class AccountRepository
    {
        private readonly QL_PHONGGYMEntities _context;

        public AccountRepository(QL_PHONGGYMEntities context)
        {
            _context = context;
        }

        public bool CusRegister(KhachHangRegisterViewModel model)
        {
            model.MatKhau = MaHoa.MaHoaNhan(model.MatKhau, 23);
            try
            {
                _context.SP_KHACHHANGDANGKY(
                    model.TenKH,
                    model.GioiTinh,
                    model.NgaySinh,
                    model.SDT,
                    model.Email,
                    model.TenDangNhap,
                    model.MatKhau
                );
                return true;
            }
            catch (Exception ex)
            {
                // Lấy inner exception nếu có
                string msg = ex.InnerException?.Message ?? ex.Message;

                // Tách message sau dấu ORA-xxxxx:
                int idx = msg.IndexOf(":");
                if (idx >= 0 && idx + 1 < msg.Length)
                    msg = msg.Substring(idx + 1).Trim();

                // Nếu có nhiều dòng, chỉ lấy dòng đầu tiên
                if (msg.Contains("\n"))
                    msg = msg.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                // Bỏ link docs nếu có
                int linkIdx = msg.IndexOf("https://");
                if (linkIdx >= 0)
                    msg = msg.Substring(0, linkIdx).Trim();

                throw new Exception(msg);
            }
        }

        public KhachHangLoginResult CusLogin(string tenDangNhap, string matKhau)
        {
            matKhau = MaHoa.MaHoaNhan(matKhau, 23);
            try
            {
                using (var cmd = _context.Database.Connection.CreateCommand())
                {
                    cmd.CommandText = "SP_KHACHHANGLOGIN";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add(new OracleParameter("p_TenDangNhap", tenDangNhap));
                    cmd.Parameters.Add(new OracleParameter("p_MatKhau", matKhau));

                    var refCursor = new OracleParameter("p_ResultSet", OracleDbType.RefCursor);
                    refCursor.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(refCursor);

                    if (_context.Database.Connection.State != ConnectionState.Open)
                        _context.Database.Connection.Open();
   
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new KhachHangLoginResult
                            {
                                MaKH = reader.GetInt32(0),
                                TenKH = reader.GetString(1),
                                SDT = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? null : reader.GetString(3)
                            };
                        }
                    }

                    _context.Database.Connection.Close();
                }

                return null; 
            }
            catch 
            {               
                throw new Exception("Đăng nhập thất bại.");
            }
        }

        public bool DangKyThu(string HoTen, string SoDienThoai, string Email)
        {
            try
            {
                _context.SP_DANGKYTAPTHU(HoTen, SoDienThoai, Email);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi đăng ký tập thử: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }
    }
}
