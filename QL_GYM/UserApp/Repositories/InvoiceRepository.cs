using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using UserApp.Helpers;
using UserApp.Models;
using UserApp.ViewModel;

namespace UserApp.Repositories
{
    public class InvoiceRepository
    {
        private readonly QL_PHONGGYMEntities _db;

        public InvoiceRepository(QL_PHONGGYMEntities db)
        {
            _db = db;
        }

        public InvoiceFullData GetInvoiceById(int id)
        {
            var result = new InvoiceFullData { Details = new List<InvoiceDetailViewModel>() };
            var conn = _db.Database.Connection as OracleConnection;

            if (conn.State != ConnectionState.Open)
                conn.Open();

            using (var cmd = new OracleCommand("ADMINGYM.sp_LayChiTietHoaDon", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("p_MaHD", OracleDbType.Int32).Value = id;

                var pHeader = new OracleParameter("out_Header", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                var pDetail = new OracleParameter("out_Detail", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pHeader);
                cmd.Parameters.Add(pDetail);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result.Header = new InvoiceHeaderViewModel
                        {
                            MAHD = Convert.ToInt32(reader["MAHD"]),
                            NGAYLAP = Convert.ToDateTime(reader["NGAYLAP"]),
                            TONGTIEN = Convert.ToDecimal(reader["TONGTIEN"]),
                            GIAMGIA = reader["GIAMGIA"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["GIAMGIA"]),
                            THANHTIEN = Convert.ToDecimal(reader["THANHTIEN"]),
                            TENKH = reader["TENKH"].ToString(),
                            SDT = GiaiMa.GiaiMaCong(reader["SDT"].ToString(), 6)
                        };
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            result.Details.Add(new InvoiceDetailViewModel
                            {
                                TENSP = reader["TENHANG"].ToString(),
                                SOLUONG = Convert.ToInt32(reader["SOLUONG"]),
                                DONGIA = Convert.ToDecimal(reader["DONGIA"]),
                                THANHTIEN_SP = Convert.ToDecimal(reader["THANHTIEN_SP"])
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}