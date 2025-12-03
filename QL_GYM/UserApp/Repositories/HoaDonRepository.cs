using UserApp.Helpers;
using UserApp.Models;
using UserApp.ViewModel;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace UserApp.Repositories // Chú ý namespace
{
    public class HoaDonRepository
    {
        private readonly QL_PHONGGYMEntities _context;
        public HoaDonRepository(string connStr)
        {
            _context = new QL_PHONGGYMEntities(connStr);
        }

        public HoaDonRepository()
        {
            _context = new QL_PHONGGYMEntities(true); 
        }
        public InvoiceFullData GetInvoiceById(int id)
        {
            var listHoaDon = _context.CHITIETHOADONs
                                     .Include("HOADON")
                                     .Include("HOADON.KHACHHANG")
                                     .Include("SANPHAM")
                                     .Where(x => x.MAHD == id)
                                     .ToList();

            if (!listHoaDon.Any())
                return null;

            var first = listHoaDon.First();

            var result = new InvoiceFullData
            {
                Header = new InvoiceHeaderViewModel
                {
                    MAHD = Convert.ToInt32(first.MAHD),
                    NGAYLAP = first.HOADON.NGAYLAP,
                    TONGTIEN = first.HOADON.TONGTIEN ?? 0,
                    GIAMGIA = first.HOADON.GIAMGIA ?? 0,
                    THANHTIEN = first.HOADON.THANHTIEN ?? 0,
                    TENKH = first.HOADON.KHACHHANG?.TENKH,
                    SDT = GiaiMa.GiaiMaCong(first.HOADON.KHACHHANG?.SDT, 6)
                },

                Details = listHoaDon.Select(item => new InvoiceDetailViewModel
                {
                    TENSP = item.SANPHAM?.TENSP,
                    SOLUONG = (int)item.SOLUONG,
                    DONGIA = item.DONGIA,
                    THANHTIEN_SP = (item.DONGIA) * (item.SOLUONG ?? 0)
                }).ToList()
            };

            return result;
        }

        public HOADON GetHOADON(int mahd)
        {
            return _context.HOADONs.Find(mahd);
        }

        public List<InvoiceFullData> GetInvoice()
        {
            var listHoaDon = _context.CHITIETHOADONs
                                     .Include("HOADON")
                                     .Include("HOADON.KHACHHANG")
                                     .Include("SANPHAM")
                                     .ToList();

            var result = listHoaDon
                .GroupBy(item => item.MAHD)
                .Select(group =>
                {
                    var first = group.First();

                    return new InvoiceFullData
                    {
                        Header = new InvoiceHeaderViewModel
                        {
                            MAHD = (int)group.Key,
                            NGAYLAP = first.HOADON.NGAYLAP,
                            TONGTIEN = first.HOADON.TONGTIEN ?? 0,
                            GIAMGIA = first.HOADON.GIAMGIA ?? 0,
                            THANHTIEN = first.HOADON.THANHTIEN ?? 0,
                            TENKH = first.HOADON.KHACHHANG?.TENKH,
                            SDT = GiaiMa.GiaiMaCong(first.HOADON.KHACHHANG?.SDT, 6)
                        },

                        Details = group.Select(item => new InvoiceDetailViewModel
                        {
                            TENSP = item.SANPHAM?.TENSP,
                            SOLUONG = (int)item.SOLUONG,
                            DONGIA = item.DONGIA,
                            THANHTIEN_SP = (item.DONGIA) * (item.SOLUONG ?? 0)
                        }).ToList()
                    };
                })
                .ToList();

            return result;
        }

    }
}