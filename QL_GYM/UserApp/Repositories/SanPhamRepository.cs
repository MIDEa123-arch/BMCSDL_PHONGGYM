using UserApp.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Oracle.ManagedDataAccess.Client;
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
        public List<SANPHAM> GetAll()
        {
            return _context.SANPHAMs.ToList();
        }

        public SANPHAM GetById(int id)
        {
            return _context.SANPHAMs.Find(id);
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
        // Trong SanPhamRepository.cs
        public bool UpdateProduct(SANPHAM sp, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                var dbEntry = _context.SANPHAMs.Find(sp.MASP);
                if (dbEntry == null)
                {
                    errorMessage = "Sản phẩm không tồn tại.";
                    return false;
                }

                // Cập nhật từng trường dữ liệu
                dbEntry.TENSP = sp.TENSP;
                dbEntry.MALOAISP = sp.MALOAISP;
                dbEntry.DONGIA = sp.DONGIA;
                dbEntry.SOLUONGTON = sp.SOLUONGTON;
                dbEntry.GIAKHUYENMAI = sp.GIAKHUYENMAI;
                dbEntry.HANG = sp.HANG;
                dbEntry.XUATXU = sp.XUATXU;
                dbEntry.BAOHANH = sp.BAOHANH;
                dbEntry.MOTACHUNG = sp.MOTACHUNG;
                dbEntry.MOTACHITIET = sp.MOTACHITIET;

                // Lưu ý: Không cập nhật hình ảnh ở hàm này, hình ảnh xử lý riêng
                _context.SaveChanges();
                return true;
            }
            catch (OracleException ex)
            {
                throw ex;

            }
        }

        // Hàm lấy danh sách hình ảnh theo MASP
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
    }
}
