using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using UserApp.Helpers;
using UserApp.ViewModel;

namespace UserApp.Repositories
{
    public class UserService
    {
        private readonly string _adminRawConnection = ConfigurationManager.ConnectionStrings["ADMIN_DB"].ConnectionString;
        private readonly string _adminEfConnection = ConfigurationManager.ConnectionStrings["ADMIN_ENTITY"].ConnectionString;
        private string _connectionStringUser;

        // Connection string EF/USER sau khi login
        public string ConnectionStringUser => !string.IsNullOrEmpty(_connectionStringUser) ? _connectionStringUser : _adminEfConnection;

        // --- 1. LẤY DANH SÁCH SESSION CỦA USER ---
        public List<SessionInfo> GetUserSessions(string username)
        {
            var list = new List<SessionInfo>();

            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();

                    using (var cmd = new OracleCommand("GET_USER_SESSIONS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();

                        var refCursor = cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor);
                        refCursor.Direction = ParameterDirection.Output;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var session = new SessionInfo
                                {
                                    Sid = Convert.ToInt32(reader["SID"]),
                                    Serial = Convert.ToInt32(reader["SERIAL#"]),
                                    Machine = reader["MACHINE"]?.ToString(),
                                    Program = reader["PROGRAM"]?.ToString(),
                                    OsUser = reader["OSUSER"]?.ToString(),
                                    Status = reader["STATUS"]?.ToString(),
                                    Type = reader["TYPE"]?.ToString()
                                };

                                if (reader["LOGON_TIME"] != DBNull.Value)
                                {
                                    session.LogonTime = Convert.ToDateTime(reader["LOGON_TIME"]);
                                }

                                list.Add(session);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi lấy session: " + ex.Message);
            }

            return list;
        }

        public bool KillSession(int sid, int serial)
        {
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                {
                    conn.Open();

                    using (var cmd = new OracleCommand("KILL_USER_SESSION", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("p_sid", OracleDbType.Int32).Value = sid;
                        cmd.Parameters.Add("p_serial", OracleDbType.Int32).Value = serial;

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể hủy phiên: " + ex.Message);
            }
        }

        public bool DeleteUser(string username)
        {
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                using (var cmd = new OracleCommand("SP_DROP_USER", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        public bool AddUser(string username, string password)
        {
            password = MaHoa.MaHoaNhan(password, 7);
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                using (var cmd = new OracleCommand("SP_CREATE_MANAGER_USER", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();
                    cmd.Parameters.Add("p_password", OracleDbType.Varchar2).Value = password;
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        public bool LockUser(string username)
        {
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                using (var cmd = new OracleCommand("SP_LOCK_USER", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool UnlockUser(string username)
        {
            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                using (var cmd = new OracleCommand("SP_UNLOCK_USER", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public List<UserInfo> GetAllUsers()
        {
            var list = new List<UserInfo>();

            using (var conn = new OracleConnection(_adminRawConnection))
            {
                conn.Open();

                using (var cmd = new OracleCommand("GET_ORACLE_USERS", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // REF CURSOR output
                    var refCursor = cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor);
                    refCursor.Direction = ParameterDirection.Output;

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new UserInfo
                            {
                                Username = reader["USERNAME"]?.ToString(),
                                AccountStatus = reader["ACCOUNT_STATUS"]?.ToString(),
                                CreatedDate = reader["CREATED"] as DateTime?,
                                LastLogin = reader["LAST_LOGIN"] == DBNull.Value
                                    ? (DateTime?)null : ((DateTimeOffset)reader["LAST_LOGIN"]).DateTime,
                                IsLocked = Convert.ToBoolean(reader["IS_LOCKED"] ?? false),
                                ActiveSessionCount = reader["ACTIVE_SESSION_COUNT"] != DBNull.Value
                                    ? Convert.ToInt32(reader["ACTIVE_SESSION_COUNT"])
                                    : 0
                            };

                            list.Add(user);
                        }
                    }
                }
            }

            return list;
        }
        public (int status, int sid, int serial) Login(string username, string password)
        {
            var adminBuilder = new OracleConnectionStringBuilder(_adminRawConnection);
            string adminUser = adminBuilder.UserID;
            string adminPassword = adminBuilder.Password;

            if (username.Equals(adminUser, StringComparison.OrdinalIgnoreCase)
                && password == adminPassword)
            {
                return (2, 0, 0); // admin không cần SID, SERIAL
            }

            // Encrypt
            password = MaHoa.MaHoaNhan(MaHoa.MaHoaNhan(password, 7), 7);

            try
            {
                // Resolve login limit
                using (var connAdmin = new OracleConnection(_adminRawConnection))
                using (var cmd = new OracleCommand("SP_RESOLVE_LOGIN_LIMIT", connAdmin))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();
                    connAdmin.Open();
                    cmd.ExecuteNonQuery();
                }

                // Build EF connection for staff
                var entityBuilder = new EntityConnectionStringBuilder(_adminEfConnection);
                var oracleBuilder = new OracleConnectionStringBuilder(entityBuilder.ProviderConnectionString);

                oracleBuilder.UserID = username.ToUpper();
                oracleBuilder.Password = password;

                entityBuilder.ProviderConnectionString = oracleBuilder.ToString();
                _connectionStringUser = entityBuilder.ToString();

                // Open user connection to get SID & SERIAL#
                int sid = 0, serial = 0;

                using (var connUser = new OracleConnection(oracleBuilder.ToString()))
                {
                    connUser.Open();

                    using (var cmd2 = new OracleCommand("ADMINGYM.SP_CHECK_OWN_SESSION", connUser))
                    {
                        cmd2.CommandType = CommandType.StoredProcedure;

                        var sidParam = cmd2.Parameters.Add("p_sid", OracleDbType.Int32);
                        sidParam.Direction = ParameterDirection.Output;

                        var serialParam = cmd2.Parameters.Add("p_serial", OracleDbType.Int32);
                        serialParam.Direction = ParameterDirection.Output;

                        cmd2.ExecuteNonQuery();

                        sid = ((OracleDecimal)cmd2.Parameters["p_sid"].Value).ToInt32();
                        serial = ((OracleDecimal)cmd2.Parameters["p_serial"].Value).ToInt32();
                    }
                }
                return (1, sid, serial);
            }
            catch
            {
                throw;
            }
        }


        // Check connection alive
        // Trả về int để phân biệt lỗi: 1=OK, 0=Bị Kill, -1=Mất tích
        public int CheckSessionAlive(string username, string sid, string serial)
        {
            try
            {
                using (var conn = new OracleConnection(ConfigurationManager.ConnectionStrings["ADMIN_DB"].ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand("SP_CHECK_USER_SESSION", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Input
                        cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();
                        cmd.Parameters.Add("p_sid", OracleDbType.Int32).Value = int.Parse(sid);
                        cmd.Parameters.Add("p_serial", OracleDbType.Int32).Value = int.Parse(serial);

                        // Output
                        var pStatus = new OracleParameter("p_status", OracleDbType.Int32);
                        pStatus.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(pStatus);

                        cmd.ExecuteNonQuery();

                        // Lấy kết quả trả về
                        if (pStatus.Value != DBNull.Value)
                        {
                            // Convert OracleDecimal to int
                            return int.Parse(pStatus.Value.ToString());
                        }
                        return -1;
                    }
                }
            }
            catch
            {
                return -1; // Lỗi kết nối coi như mất session
            }
        }


        // Logout staff
        public bool Logout(string username)
        {
            if (string.IsNullOrEmpty(username)) return true;

            try
            {
                using (var conn = new OracleConnection(_adminRawConnection))
                using (var cmd = new OracleCommand("SP_LOGOUT_USER", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<AuditLogViewModel> GetAuditLogs(string username = null)
        {
            var list = new List<AuditLogViewModel>();

            using (var conn = new OracleConnection(_adminRawConnection))
            {
                try
                {
                    conn.Open();

                    using (var cmd = new OracleCommand("ADMINGYM.SP_GET_AUDIT_LOGS", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.BindByName = true;

                        if (!string.IsNullOrEmpty(username))
                        {
                            cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();
                        }
                        else
                        {
                            cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = DBNull.Value;
                        }

                        cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor, ParameterDirection.Output);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime thoiGian;
                                DateTime.TryParseExact(
                                    reader["THOI_GIAN"]?.ToString(),
                                    "dd/MM/yyyy HH:mm:ss",
                                    null,
                                    System.Globalization.DateTimeStyles.None,
                                    out thoiGian
                                );

                                var log = new AuditLogViewModel
                                {
                                    ThoiGian = thoiGian,
                                    DbUser = reader["DB_USER"]?.ToString(),
                                    TenDoiTuong = reader["TEN_BANG"] != DBNull.Value ? reader["TEN_BANG"].ToString() : "N/A",
                                    HanhDong = reader["HANH_DONG"]?.ToString(),
                                    CauLenhSql = reader["CAU_LENH_SQL"] != DBNull.Value ? reader["CAU_LENH_SQL"].ToString() : "(Không có)"
                                };
                                list.Add(log);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Lỗi khi gọi Procedure Audit Trail: " + ex.Message);
                }
            }

            return list;
        }
    }
}

