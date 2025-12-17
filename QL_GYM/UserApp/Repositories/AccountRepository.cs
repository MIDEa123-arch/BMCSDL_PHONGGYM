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


        public bool KhoiPhucMK(string email, string mk)
        {            
            string matKhauDaMaHoaLan1 = MaHoa.MaHoaNhan(mk, 23);

            string connStr = _context.Database.Connection.ConnectionString;

            using (OracleConnection conn = new OracleConnection(connStr))
            {
                using (OracleCommand cmd = new OracleCommand("ADMINGYM.SP_KHOIPHUCMATKHAU", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    
                    cmd.Parameters.Add(new OracleParameter("p_Email", OracleDbType.NVarchar2)).Value = email;
                    
                    cmd.Parameters.Add(new OracleParameter("p_MatKhauMoi", OracleDbType.NVarchar2)).Value = matKhauDaMaHoaLan1;

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception ex)
                    {                        
                        string msg = ex.Message;
                        int idx = msg.IndexOf(":");
                        if (idx >= 0 && idx + 1 < msg.Length) msg = msg.Substring(idx + 1).Trim();
                        if (msg.Contains("\n")) msg = msg.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                        throw new Exception(msg);
                    }
                }
            }
        }

        public bool CusRegister(KhachHangRegisterViewModel model)
        {
            model.MatKhau = MaHoa.MaHoaNhan(model.MatKhau, 23);

            string connStr = _context.Database.Connection.ConnectionString;

            using (OracleConnection conn = new OracleConnection(connStr))
            {
                using (OracleCommand cmd = new OracleCommand("SP_KHACHHANGDANGKY", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new OracleParameter("p_TenKH", OracleDbType.NVarchar2)).Value = model.TenKH;

                    // p_GioiTinh
                    cmd.Parameters.Add(new OracleParameter("p_GioiTinh", OracleDbType.NVarchar2)).Value = model.GioiTinh;

                    // p_NgaySinh
                    var pNgaySinh = new OracleParameter("p_NgaySinh", OracleDbType.Date); // Hoặc TimeStamp
                    pNgaySinh.Value = model.NgaySinh.HasValue ? (object)model.NgaySinh.Value : DBNull.Value;
                    cmd.Parameters.Add(pNgaySinh);

                    // p_SDT
                    var pSDT = new OracleParameter("p_SDT", OracleDbType.NVarchar2);
                    pSDT.Value = string.IsNullOrEmpty(model.SDT) ? DBNull.Value : (object)model.SDT;
                    cmd.Parameters.Add(pSDT);

                    // p_Email
                    var pEmail = new OracleParameter("p_Email", OracleDbType.NVarchar2);
                    pEmail.Value = string.IsNullOrEmpty(model.Email) ? DBNull.Value : (object)model.Email;
                    cmd.Parameters.Add(pEmail);

                    // p_TenDangNhap
                    cmd.Parameters.Add(new OracleParameter("p_TenDangNhap", OracleDbType.NVarchar2)).Value = model.TenDangNhap;

                    // p_MatKhau
                    cmd.Parameters.Add(new OracleParameter("p_MatKhau", OracleDbType.NVarchar2)).Value = model.MatKhau;

                    try
                    {
                        if (conn.State != ConnectionState.Open)
                            conn.Open();

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                    catch (Exception ex)
                    {                        
                        string msg = ex.Message;
                        int idx = msg.IndexOf(":");
                        if (idx >= 0 && idx + 1 < msg.Length)
                            msg = msg.Substring(idx + 1).Trim();

                        if (msg.Contains("\n"))
                            msg = msg.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

                        int linkIdx = msg.IndexOf("https://");
                        if (linkIdx >= 0)
                            msg = msg.Substring(0, linkIdx).Trim();

                        throw new Exception(msg);
                    }
                }
            }
        }

        public KhachHangLoginResult CusLogin(string tenDangNhap, string matKhau)
        {
            matKhau = MaHoa.MaHoaNhan(matKhau, 23);
            string connStr = _context.Database.Connection.ConnectionString;

            try
            {
                // 2. Tạo kết nối mới (Không dùng chung với _context để tránh lỗi đóng kết nối)
                using (OracleConnection conn = new OracleConnection(connStr))
                {
                    using (OracleCommand cmd = new OracleCommand("sp_KhachHangLogin", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // 3. QUAN TRỌNG: Phải dùng NVarchar2
                        cmd.Parameters.Add(new OracleParameter("p_TenDangNhap", OracleDbType.NVarchar2)).Value = tenDangNhap;
                        cmd.Parameters.Add(new OracleParameter("p_MatKhau", OracleDbType.NVarchar2)).Value = matKhau;

                        var refCursor = new OracleParameter("p_ResultSet", OracleDbType.RefCursor);
                        refCursor.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(refCursor);

                        conn.Open(); // Mở kết nối riêng này

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
                    }
                }
                return null; // Không tìm thấy user
            }
            catch (Exception ex)
            {
                // Ném lỗi thật ra để debug
                throw new Exception("Lỗi đăng nhập: " + ex.Message);
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
