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

                                        model.Tables.Add(tableName);

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
    }
}