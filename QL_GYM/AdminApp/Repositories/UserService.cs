using UserApp.Helpers;
using UserApp.ViewModel;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;

public class UserService
{
    private readonly string _connectionString;

    public UserService(string connectionStringName)
    {
        _connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
    }

    public bool DeleteUser(string username)
    {
        try
        {
            using (var conn = new OracleConnection(_connectionString))
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
            using (var conn = new OracleConnection(_connectionString))
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
            using (var conn = new OracleConnection(_connectionString))
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
            using (var conn = new OracleConnection(_connectionString))
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

        using (var conn = new OracleConnection(_connectionString))
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
                                ? (DateTime?)null:((DateTimeOffset)reader["LAST_LOGIN"]).DateTime,
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
}
