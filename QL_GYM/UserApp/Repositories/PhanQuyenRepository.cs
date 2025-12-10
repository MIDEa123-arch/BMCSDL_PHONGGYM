using Oracle.ManagedDataAccess.Client; 
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using UserApp.Models;
using UserApp.ViewModel;

namespace UserApp.Repositories
{
    public class PhanQuyenRepository
    {

        private readonly string _adminRawConnection = ConfigurationManager.ConnectionStrings["ADMIN_DB"].ConnectionString;
  
        public void LoadMetadata(GrantViewModel model)
        {
            model.Users = new List<string>();
            model.Tables = new List<string>();
            model.Roles = new List<string>();

            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();

                    // Gọi Procedure
                    using (var cmd = new OracleCommand("ADMINGYM.SP_GET_METADATA", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true; // Bắt buộc

                        // Khai báo tham số Output
                        cmd.Parameters.Add("p_cursor_users", OracleDbType.RefCursor, ParameterDirection.Output);
                        cmd.Parameters.Add("p_cursor_tables", OracleDbType.RefCursor, ParameterDirection.Output);
                        cmd.Parameters.Add("p_cursor_roles", OracleDbType.RefCursor, ParameterDirection.Output);

                        using (var reader = cmd.ExecuteReader())
                        {
                            // 1. Đọc Users
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) model.Users.Add(reader.GetString(0));
                            }

                            // 2. Đọc Tables (Chỉ thuộc ADMINGYM)
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
                                        // Lấy tên bảng (Ví dụ: KHACHHANG)
                                        string tableName = reader.GetString(0);

                                        model.Tables.Add("ADMINGYM." + tableName);

                                    }
                                }
                            }

                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0)) model.Roles.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                model.Message = "Lỗi tải dữ liệu: " + msg;
                model.MessageType = "error";
            }
        }
        public void UpdatePermission(string actionType, string target, string tableName, string privileges, GrantViewModel model)
        {
            // Xác định tên Procedure dựa trên hành động
            string procName = (actionType == "GRANT") ? "sp_grant_permission" : "sp_revoke_permission";

            try
            {
                // Tạo kết nối MỚI HOÀN TOÀN để tránh lỗi ORA-50001
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();

                    using (var cmd = new OracleCommand(procName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        // Map tham số theo vị trí (quan trọng nếu tên biến trong Proc khác tên tham số C#)
                        cmd.BindByName = false;

                        // 1. User/Role
                        cmd.Parameters.Add("p_target", OracleDbType.Varchar2).Value = target;
                        // 2. Table Name
                        cmd.Parameters.Add("p_table", OracleDbType.Varchar2).Value = tableName;
                        // 3. Privileges string
                        cmd.Parameters.Add("p_privs", OracleDbType.Varchar2).Value = privileges;

                        // Thực thi
                        cmd.ExecuteNonQuery();

                        // Thông báo thành công
                        if (actionType == "GRANT")
                        {
                            model.Message = $"Thành công: Đã cấp quyền {privileges} trên bảng {tableName} cho {target}.";
                            model.MessageType = "success";
                        }
                        else
                        {
                            model.Message = $"Thành công: Đã thu hồi quyền {privileges} trên bảng {tableName} khỏi {target}.";
                            model.MessageType = "warning";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var realError = ex.Message;
                if (ex.InnerException != null) realError += " | " + ex.InnerException.Message;

                model.Message = "Lỗi thực thi: " + realError;
                model.MessageType = "error";
            }
        }
        // Trong PhanQuyenRepository.cs (Thay thế phương thức hiện tại)

        public List<string> GetExistingPrivileges(string target, string tableName)
        {
            var existingPrivs = new List<string>();

            // Phân tích Table Name (chỉ cần lấy tên bảng)
            var parts = tableName.Split('.');
            if (parts.Length != 2) return existingPrivs;
            string name = parts[1]; // Tên bảng (ví dụ: KHACHHANG)

            // Sử dụng _adminRawConnection
            using (var conn = new OracleConnection(_adminRawConnection))
            {
                try
                {
                    conn.Open();

                    // ----------------------------------------------------
                    // THAY THẾ: Gọi Stored Procedure SP_GET_EXISTING_PRIVS
                    // ----------------------------------------------------
                    using (var cmd = new OracleCommand("ADMINGYM.SP_GET_EXISTING_PRIVS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true; // Sử dụng tên tham số

                        // 1. Tham số đầu vào: TARGET (User/Role)
                        cmd.Parameters.Add("p_target", OracleDbType.Varchar2).Value = target;

                        // 2. Tham số đầu vào: TABLE_NAME
                        cmd.Parameters.Add("p_table_name", OracleDbType.Varchar2).Value = name;

                        // 3. Tham số đầu ra: CURSOR (chứa kết quả SELECT)
                        cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor, ParameterDirection.Output);

                        // ExecuteReader sẽ đọc kết quả từ RefCursor
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    existingPrivs.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi
                    // Log lỗi vào Console (hoặc logger thực tế)
                    Console.WriteLine("Lỗi khi gọi Procedure SP_GET_EXISTING_PRIVS: " + ex.Message);
                }
            }

            return existingPrivs;
        }
    }
}