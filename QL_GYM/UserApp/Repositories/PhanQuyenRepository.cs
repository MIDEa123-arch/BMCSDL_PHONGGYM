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

        // Thêm vào class PhanQuyenRepository

        public List<string> GetUsersByRole(string roleName)
        {
            var list = new List<string>();
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand("ADMINGYM.SP_GET_USERS_IN_ROLE", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true;

                        cmd.Parameters.Add("p_role_name", OracleDbType.Varchar2).Value = roleName;
                        cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor, ParameterDirection.Output);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    list.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                }
            }
            catch(OracleException ex) 
            {
                throw ex;
            }
            return list;
        }

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

                    using (var cmd = new OracleCommand("ADMINGYM.SP_GET_METADATA", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true; 

                        cmd.Parameters.Add("p_cursor_users", OracleDbType.RefCursor, ParameterDirection.Output);
                        cmd.Parameters.Add("p_cursor_tables", OracleDbType.RefCursor, ParameterDirection.Output);
                        cmd.Parameters.Add("p_cursor_roles", OracleDbType.RefCursor, ParameterDirection.Output);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0)) model.Users.Add(reader.GetString(0));
                            }
                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    if (!reader.IsDBNull(0))
                                    {
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
            string procName = (actionType == "GRANT") ? "sp_grant_permission" : "sp_revoke_permission";

            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();

                    using (var cmd = new OracleCommand(procName, conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.BindByName = false;

                        cmd.Parameters.Add("p_target", OracleDbType.Varchar2).Value = target;
                        cmd.Parameters.Add("p_table", OracleDbType.Varchar2).Value = tableName;
                        cmd.Parameters.Add("p_privs", OracleDbType.Varchar2).Value = privileges;

                        cmd.ExecuteNonQuery();

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

        public List<string> GetExistingPrivileges(string target, string tableName)
        {
            var existingPrivs = new List<string>();

            var parts = tableName.Split('.');
            if (parts.Length != 2) return existingPrivs;
            string name = parts[1]; 

            using (var conn = new OracleConnection(_adminRawConnection))
            {
                try
                {
                    conn.Open();

                    using (var cmd = new OracleCommand("ADMINGYM.SP_GET_EXISTING_PRIVS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true; 

                        cmd.Parameters.Add("p_target", OracleDbType.Varchar2).Value = target;

                        cmd.Parameters.Add("p_table_name", OracleDbType.Varchar2).Value = name;

                        cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor, ParameterDirection.Output);

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
                    Console.WriteLine("Lỗi khi gọi Procedure SP_GET_EXISTING_PRIVS: " + ex.Message);
                }
            }

            return existingPrivs;
        }
        public void RevokeRoleFromUser(string userName, string roleName, GrantViewModel model)
        {
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();

                    using (var cmd = new OracleCommand("ADMINGYM.SP_REVOKE_ROLE_FROM_USER", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true;

                        cmd.Parameters.Add("p_user_name", OracleDbType.Varchar2).Value = userName;
                        cmd.Parameters.Add("p_role_name", OracleDbType.Varchar2).Value = roleName;

                        cmd.ExecuteNonQuery();

                        model.Message = $"Thành công: Đã thu hồi nhóm quyền '{roleName}' khỏi User '{userName}'.";
                        model.MessageType = "warning"; // Màu vàng cảnh báo
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                model.Message = "Lỗi thu hồi Role: " + msg;
                model.MessageType = "error";
            }
        }

        public void GrantRoleToUser(string userName, string roleName, GrantViewModel model)
        {
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();

                    using (var cmd = new OracleCommand("ADMINGYM.SP_GRANT_ROLE_TO_USER", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true;

                        cmd.Parameters.Add("p_user_name", OracleDbType.Varchar2).Value = userName;
                        cmd.Parameters.Add("p_role_name", OracleDbType.Varchar2).Value = roleName;

                        cmd.ExecuteNonQuery();

                        model.Message = $"Thành công: Đã thêm User '{userName}' vào nhóm quyền '{roleName}'.";
                        model.MessageType = "success";
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                model.Message = "Lỗi cấp Role: " + msg;
                model.MessageType = "error";
            }
        }
    }
}