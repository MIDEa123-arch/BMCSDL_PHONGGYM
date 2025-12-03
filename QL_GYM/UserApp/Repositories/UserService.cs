using Oracle.ManagedDataAccess.Client;
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
                                ObjectPrivileges = reader["OBJECT_PRIVILEGES"] != DBNull.Value
                                    ? reader["OBJECT_PRIVILEGES"].ToString()
                                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                        .ToList()
                                    : new List<string>(),
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
        public int Login(string username, string password)
        {
            var adminBuilder = new OracleConnectionStringBuilder(_adminRawConnection);
            string adminUser = adminBuilder.UserID;
            string adminPassword = adminBuilder.Password;

            if (username.Equals(adminUser, StringComparison.OrdinalIgnoreCase) && password == adminPassword)
            {
                return 2;
            }


            password = MaHoa.MaHoaNhan(MaHoa.MaHoaNhan(password, 7), 7);

            try
            {
                using (var connAdmin = new OracleConnection(_adminRawConnection))
                using (var cmd = new OracleCommand("SP_RESOLVE_LOGIN_LIMIT", connAdmin))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_username", OracleDbType.Varchar2).Value = username.ToUpper();
                    connAdmin.Open();
                    cmd.ExecuteNonQuery();
                }

                // 2. Tạo EF connection string cho user staff
                var entityBuilder = new EntityConnectionStringBuilder(_adminEfConnection);
                var oracleBuilder = new OracleConnectionStringBuilder(entityBuilder.ProviderConnectionString);

                oracleBuilder.UserID = username.ToUpper();
                oracleBuilder.Password = password;

                entityBuilder.ProviderConnectionString = oracleBuilder.ToString();
                _connectionStringUser = entityBuilder.ToString();

                // 3. Test login user bằng Oracle raw connection
                using (var connUser = new OracleConnection(oracleBuilder.ToString()))
                {
                    connUser.Open();
                }

                return 1;
            }
            catch (OracleException ex)
            {
                throw ex;
            }

        }

        // Check connection alive
        public bool CheckConnectionAlive(string efConnectionString)
        {
            if (string.IsNullOrEmpty(efConnectionString)) return false;

            try
            {
                var entityBuilder = new EntityConnectionStringBuilder(efConnectionString);
                string rawOracleConnectionString = entityBuilder.ProviderConnectionString;

                using (var conn = new OracleConnection(rawOracleConnectionString))
                using (var cmd = new OracleCommand("SP_TEST_CONNECTION", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("p_alive", OracleDbType.Int32).Direction = ParameterDirection.Output;
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
    }
}
