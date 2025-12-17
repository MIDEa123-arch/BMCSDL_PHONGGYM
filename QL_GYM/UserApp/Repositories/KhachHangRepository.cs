using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Helpers;
using UserApp.Models;
using UserApp.ViewModel;

namespace UserApp.Repositories
{
    public class KhachHangRepository
    {
        private readonly QL_PHONGGYMEntities _context;

        public KhachHangRepository(QL_PHONGGYMEntities context)
        {
            _context = context;
        }
        public KhachHangRepository(string connStr)
        {
            _context = new QL_PHONGGYMEntities(connStr);
        }

        public KhachHangRepository()
        {
            _context = new QL_PHONGGYMEntities(true);
        }

        public List<HocVienViewModel> GetHocVien()
        {
            var list = (from kh in _context.KHACHHANGs
                        join dk in _context.DANGKYPTs on kh.MAKH equals dk.MAKH
                        join nv in _context.NHANVIENs on dk.MANV equals nv.MANV                       
                        select new HocVienViewModel
                        {
                            MaKH = (int)kh.MAKH,
                            TenKH = kh.TENKH,
                            GioiTinh = kh.GIOITINH,
                            SDT = kh.SDT,
                            Email = kh.EMAIL,
                            SoBuoi = (int)(dk.SOBUOI),
                            TrangThai = dk.TRANGTHAI
                        }).ToList();
            
            foreach (var item in list)
            {
                item.SDT = GiaiMa.GiaiMaCong(item.SDT, 6);
                item.Email = GiaiMa.GiaiMaCong(item.Email, 6);
            }

            return list;
        }
        public List<KHACHHANG> GetMyCustomers()
        {
            var list = _context.KHACHHANGs.ToList();
            
            foreach (var item in list)
            {
                try
                {
                    item.SDT = GiaiMa.GiaiMaCong(item.SDT, 6);
                    item.EMAIL = GiaiMa.GiaiMaCong(item.EMAIL, 6);
                }
                catch
                {                    
                }
            }

            return list;
        }
        public KHACHHANG ThongTinKH(int makh)
        {
            var kh = _context.KHACHHANGs.FirstOrDefault(k => k.MAKH == makh);
            if (kh != null)
            {     
                if (kh.EMAIL != null) kh.EMAIL = GiaiMa.GiaiMaCong(kh.EMAIL, 6);
                if (kh.SDT != null) kh.SDT = GiaiMa.GiaiMaCong(kh.SDT, 6);
            }
            return kh;

        }
        public LOAIKHACHHANG LoaiKh(int maloai)
        {
            return _context.LOAIKHACHHANGs.FirstOrDefault(kh => kh.MALOAIKH == maloai);

        }

        public DIACHI GetDiaChi(int makh)
        {
            var diaChiList = _context.DIACHIs.Where(dc => dc.MAKH == makh).OrderByDescending(dc => dc.NGAYTHEM).ToList();

            if (!diaChiList.Any())
                return null;

            var diaChi = diaChiList.FirstOrDefault(dc => dc.LADIACHIMACDINH == true);

            return diaChi ?? diaChiList.First();
        }


        public void ThemDiaChi(int makh, FormCollection form)
        {
            string tinh = form["province"];
            string huyen = form["district"];
            string xa = form["ward"];
            string diaChiCuThe = form["address"];

            if (string.IsNullOrEmpty(tinh) || string.IsNullOrEmpty(huyen) || string.IsNullOrEmpty(xa)) return;

            var diaChiTonTai = _context.DIACHIs
                .FirstOrDefault(dc =>
                    dc.MAKH == makh &&
                    dc.TINHTHANHPHO == tinh &&
                    dc.QUANHUYEN == huyen &&
                    dc.PHUONGXA == xa &&
                    dc.DIACHICUTHE == diaChiCuThe);

            if (diaChiTonTai == null)
            {
                var diaChiMoi = new DIACHI
                {
                    MAKH = makh,
                    TINHTHANHPHO = tinh,
                    QUANHUYEN = huyen,
                    PHUONGXA = xa,
                    DIACHICUTHE = diaChiCuThe,
                    LADIACHIMACDINH = false,                    
                };

                _context.DIACHIs.Add(diaChiMoi);               
            }
            else
            {
                diaChiTonTai.NGAYTHEM = DateTime.Now;
                _context.Entry(diaChiTonTai).State = EntityState.Modified;
            }
            _context.SaveChanges();
        }
    }
}