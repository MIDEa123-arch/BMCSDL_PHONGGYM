using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.IO;

using UserApp.Models;
using UserApp.ViewModel;
namespace UserApp.Repositories
{
    public class SanPhamRepository
    {
        private readonly QL_PHONGGYMEntities _context;
        public SanPhamRepository(string connStr)
        {
            _context = new QL_PHONGGYMEntities(connStr);
        }

        public SanPhamRepository()
        {
            _context = new QL_PHONGGYMEntities(true);
        }

        public SANPHAM GetSanPhamById(int id)
        {
            return _context.SANPHAMs.Find(id);
        }



        public List<SANPHAM> GetAll()
        {
            return _context.SANPHAMs.ToList();
        }

        public List<LOAISANPHAM> GetLoai()
        {
            return _context.LOAISANPHAMs.ToList();
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

        public void Add(SANPHAM sp)
        {
            _context.SANPHAMs.Add(sp);
            _context.SaveChanges();
        }

        public void Update(SANPHAM sp)
        {
            _context.Entry(sp).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
        }

        public void Delete(int id)
        {
            var sp = _context.SANPHAMs.Find(id);
            if (sp != null)
            {
                _context.SANPHAMs.Remove(sp);
                _context.SaveChanges();
            }
        }
        public void AddWithImages(SANPHAM sp, List<HINHANH> danhSachHinh)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.SANPHAMs.Add(sp);
                    _context.SaveChanges();
                    if (danhSachHinh != null && danhSachHinh.Count > 0)
                    {
                        foreach (var hinh in danhSachHinh)
                        {
                            hinh.MASP = sp.MASP;
                            _context.HINHANHs.Add(hinh);
                        }
                        _context.SaveChanges();
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();                    
                    throw;
                }
            }
        }

        public List<HINHANH> GetImagesByProductId(int maSP)
        {
            return _context.HINHANHs.Where(x => x.MASP == maSP).ToList();
        }

        // Hàm xóa hình ảnh (Dùng khi người dùng thay thế ảnh mới)
        public void DeleteImage(int maHinh)
        {
            var img = _context.HINHANHs.Find(maHinh);
            if (img != null)
            {
                _context.HINHANHs.Remove(img);
                _context.SaveChanges();
            }
        }

        // Hàm thêm hình (Dùng lại logic cũ)
        public void AddImage(HINHANH img)
        {
            try
            {
                _context.HINHANHs.Add(img);
                _context.SaveChanges();
            }
            catch (OracleException ex)
            {
                throw ex;
            }
        }

        public bool UpdateProduct(SANPHAM model, List<string> newImageNames, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                var dbItem = _context.SANPHAMs.Find(model.MASP);
                if (dbItem == null)
                {
                    errorMessage = "Sản phẩm không tồn tại.";
                    return false;
                }
                
                dbItem.TENSP = model.TENSP;
                dbItem.MALOAISP = model.MALOAISP;
                dbItem.DONGIA = model.DONGIA;
                dbItem.GIAKHUYENMAI = model.GIAKHUYENMAI;
                dbItem.SOLUONGTON = model.SOLUONGTON;
                dbItem.HANG = model.HANG;
                dbItem.XUATXU = model.XUATXU;
                dbItem.BAOHANH = model.BAOHANH;
                dbItem.MOTACHUNG = model.MOTACHUNG;
                dbItem.MOTACHITIET = model.MOTACHITIET;
                
                if (newImageNames != null && newImageNames.Count > 0)
                {
                    foreach (var imageName in newImageNames)
                    {
                        var hinhAnh = new HINHANH
                        {
                            MASP = dbItem.MASP,
                            URL = imageName,
                            ISMAIN = false
                        };
                        _context.HINHANHs.Add(hinhAnh);
                    }
                }

                _context.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {                
                var inner = ex.InnerException;
                while (inner != null)
                {
                    if (inner is OracleException oracleEx)
                    {                        
                        if (oracleEx.Number == 1031 || oracleEx.Number == 6550)
                        {
                            errorMessage = "Bạn không có quyền thực hiện thao tác này.";
                            return false;
                        }
                        
                        errorMessage = "Lỗi Database: " + oracleEx.Message;
                        return false;
                    }
                    inner = inner.InnerException;
                }
                
                errorMessage = "Lỗi cập nhật dữ liệu: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "Lỗi hệ thống: " + ex.Message;
                return false;
            }
        }

        // Helper to check for duplicate names (excluding current ID)
        public bool IsDuplicateName(string tenSp, int maSp)
        {
            return _context.SANPHAMs.Any(x =>
                x.TENSP.ToLower() == tenSp.Trim().ToLower() &&
                x.MASP != maSp
            );
        }
    }
}
