using UserApp.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UserApp.Models;

namespace UserApp.Repositories
{
    public class ProductRepository
    {
        private readonly QL_PHONGGYMEntities _context;

        public ProductRepository(QL_PHONGGYMEntities context)
        {
            _context = context;
        }
        public List<CHUYENMON> GetChuyenMons()
        {
            var list = _context.CHUYENMONs.Take(5).ToList();

            return list;
        }
        public List<LOAISANPHAM> GetLoaiSanPhams()
        {
            return _context.LOAISANPHAMs.ToList();
        }

        public List<string> GetHangsByLoai()
        {
            return _context.SANPHAMs.Where(sp => sp.HANG != null).Select(sp => sp.HANG).Distinct().ToList();
        }

        public List<string> GetXuatSu()
        {
            return _context.SANPHAMs.Where(sp => sp.XUATXU != null).Select(sp => sp.XUATXU).Distinct().ToList();
        }

        public List<GOITAP> GetGoiTaps()
        {
            return _context.GOITAPs.Where(sp => sp.GIA == 399000.00m || sp.GIA == 10000000.00m).ToList();
        }

        public List<SanPhamViewModel> GetSanPhams()
        {
            var list = (from sp in _context.SANPHAMs
                        join ha in _context.HINHANHs on sp.MASP equals ha.MASP into haGroup
                        select new SanPhamViewModel
                        {
                            MaSP = (int)sp.MASP,
                            TenSP = sp.TENSP,
                            LoaiSP = sp.LOAISANPHAM.TENLOAISP,
                            DonGia = sp.DONGIA,
                            SoLuongTon = (int)sp.SOLUONGTON,
                            GiaKhuyenMai = (decimal)sp.GIAKHUYENMAI,
                            Hang = sp.HANG,
                            XuatXu = sp.XUATXU,
                            BaoHanh = sp.BAOHANH,
                            MoTaChung = sp.MOTACHUNG,
                            MaTaChiTiet = sp.MOTACHITIET,
                            UrlHinhAnhChinh = haGroup.FirstOrDefault(h => h.ISMAIN == true).URL,
                            UrlHinhAnhsPhu = haGroup.Where(h => h.ISMAIN == false)
                                                    .Select(h => h.URL)
                                                    .ToList()
                        }).ToList();

            return list;
        }
    }
}