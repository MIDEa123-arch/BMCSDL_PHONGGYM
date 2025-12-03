using UserApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UserApp.Models;
using System.Web.Mvc;

namespace UserApp.Repositories
{
    public class CartRepository
    {
        private readonly QL_PHONGGYMEntities _context;

        public CartRepository(QL_PHONGGYMEntities context)
        {
            _context = context;
        }

        public int TaoHoaDon(FormCollection form, int makh, List<GioHangViewModel> cart)
        {
            HOADON hoaDon = new HOADON
            {
                MAKH = makh,
                TONGTIEN = (decimal)Convert.ToDecimal(form["tongTien"]),
                THANHTIEN = (decimal)Convert.ToDecimal(form["thanhTien"]),
                TRANGTHAI = form["paymentMethod"] == "BANK" ? "Đã thanh toán" : "Chưa thanh toán",
                GIAMGIA = (decimal)Convert.ToDecimal(form["giamGia"]),
                NGAYLAP = DateTime.Now,
            };

            _context.HOADONs.Add(hoaDon);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                if (item.MaSP != null)
                {
                    var DonGia = item.GiaKhuyenMaiSP ?? item.DonGia;
                    CHITIETHOADON ct = new CHITIETHOADON
                    {
                        MAHD = hoaDon.MAHD,
                        MASP = (int)item.MaSP,
                        MADKGT = null,
                        MADKLOP = null,
                        MADKPT = null,
                        SOLUONG = item.SoLuong ?? 1,
                        DONGIA = DonGia
                    };
                    _context.CHITIETHOADONs.Add(ct);
                    XoaDon(item.MaSP.Value, makh);
                }
            }
            _context.SaveChanges();
            return (int)hoaDon.MAHD;
        }

        public void Xoa(int id)
        {
            var item = _context.CHITIETGIOHANGs.FirstOrDefault(sp => sp.MASP == id);
            item.SOLUONG = item.SOLUONG - 1;

            if (item.SOLUONG <= 0)
                _context.CHITIETGIOHANGs.Remove(item);

            _context.SaveChanges();
        }

        public void Them(int id)
        {
            var item = _context.CHITIETGIOHANGs.FirstOrDefault(sp => sp.MASP == id);
            var product = _context.SANPHAMs.FirstOrDefault(sp => sp.MASP == id);
            if (item.SOLUONG >= product.SOLUONGTON)
            {
                item.SOLUONG = product.SOLUONGTON;
            }
            item.SOLUONG = item.SOLUONG + 1;
            _context.SaveChanges();
        }

        public void XoaDon(int id, int makh)
        {
            var item = _context.CHITIETGIOHANGs.FirstOrDefault(c => c.MAKH == makh &&
                                                            ((c.MASP != null && c.MASP == id) ||
                                                             (c.MAGOITAP != null && c.MAGOITAP == id) ||
                                                             (c.MALOP != null && c.MALOP == id)));
            if (item != null)
            {
                _context.CHITIETGIOHANGs.Remove(item);
                _context.SaveChanges();
            }
        }

        public void XoaDaChon(FormCollection form, int makh)
        {
            string[] selected = form.GetValues("selectedItems");
            List<int> selectedIds = selected.Select(int.Parse).ToList();
            foreach (var id in selectedIds)
            {
                var item = _context.CHITIETGIOHANGs.FirstOrDefault(c => c.MASP == makh && c.MASP == id);
                if (item == null)
                {
                    item = _context.CHITIETGIOHANGs.FirstOrDefault(c => c.MAGOITAP == makh && c.MAGOITAP == id);
                }
                if (item == null)
                {
                    item = _context.CHITIETGIOHANGs.FirstOrDefault(c => c.MALOP == makh && c.MALOP == id);
                }
                if (item != null)
                    _context.CHITIETGIOHANGs.Remove(item);
            }

            _context.SaveChanges();
        }

        public List<GioHangViewModel> ChonSanPham(FormCollection form, int makh)
        {
            string[] selected = form.GetValues("selectedItems");
            List<int> selectedIds = selected.Select(int.Parse).ToList();
            List<GioHangViewModel> list = new List<GioHangViewModel>();
            var cart = GetCart(makh);

            foreach (var id in selectedIds)
            {
                var item = cart.FirstOrDefault(c => c.MaKH == makh && c.MaSP == id);
                if (item == null)
                {
                    item = cart.FirstOrDefault(c => c.MaKH == makh && c.MaGoiTap == id);
                }
                if (item == null)
                {
                    item = cart.FirstOrDefault(c => c.MaKH == makh && c.MaLop == id);
                }
                if (item != null)
                    list.Add(item);
            }

            return list;
        }

        public bool AddToCart(int maKH, int? maSP, int? maGoiTap, int? maLop, int soLuong)
        {
            try
            {
                var existingItem = _context.CHITIETGIOHANGs.FirstOrDefault(c =>
                    c.MAKH == maKH &&
                    ((maSP != null && c.MASP == maSP) ||
                     (maGoiTap != null && c.MAGOITAP == maGoiTap) ||
                     (maLop != null && c.MALOP == maLop))
                );

                if (existingItem != null)
                {
                    existingItem.SOLUONG += soLuong;

                    if (maSP != null)
                    {
                        var product = _context.SANPHAMs.FirstOrDefault(p => p.MASP == maSP);
                        if (product != null && existingItem.SOLUONG > product.SOLUONGTON)
                        {
                            existingItem.SOLUONG = product.SOLUONGTON;
                        }
                    }
                }
                else
                {
                    decimal donGia = 0;

                    if (maSP != null)
                    {
                        var product = _context.SANPHAMs.FirstOrDefault(p => p.MASP == maSP);
                        donGia = product != null ? product.DONGIA : 0;
                    }
                    var newItem = new CHITIETGIOHANG
                    {
                        MAKH = maKH,
                        MASP = maSP,
                        MAGOITAP = maGoiTap,
                        MALOP = maLop,
                        SOLUONG = soLuong,
                        DONGIA = donGia,
                        NGAYTHEM = DateTime.Now
                    };

                    _context.CHITIETGIOHANGs.Add(newItem);
                }

                _context.SaveChanges();
                return true;
            }
            catch
            {
                throw;
            }
        }


        public List<GioHangViewModel> GetCart(int maKH)
        {
            try
            {
                var cart = _context.CHITIETGIOHANGs
                    .Where(c => c.MAKH == maKH)
                    .Select(c => new GioHangViewModel
                    {
                        MaChiTietGH = (int)c.MACHITIETGH,
                        MaKH = (int)c.MAKH,
                        MaSP = (int?)c.MASP ?? 0,
                        MaGoiTap = (int?)c.MAGOITAP ?? 0,
                        MaLop = (int?)c.MALOP ?? 0,
                        SoLuong = (int)c.SOLUONG,
                        DonGia =  c.DONGIA,
                        NgayThem = c.NGAYTHEM,
                        GiaKhuyenMaiSP = c.MASP != null ? c.SANPHAM.GIAKHUYENMAI : null,
                        TenMonHang = c.MASP != null ? c.SANPHAM.TENSP
                                     : c.MAGOITAP != null ? c.GOITAP.TENGOI
                                     : c.LOPHOC.TENLOP,
                        AnhDaiDienSP = c.MASP != null
                            ? c.SANPHAM.HINHANHs.FirstOrDefault(a => a.ISMAIN.HasValue && a.ISMAIN.Value).URL
                            : null,
                        SoLuongTon = c.MASP != null ? (int)c.SANPHAM.SOLUONGTON : 0
                    })
                    .ToList();

                return cart;
            }
            catch
            {
                throw;
            }
        }
    }
}