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

        public List<HINHANH> GetImagesByProductId(int maSP)
        {
            return _context.HINHANHs.Where(x => x.MASP == maSP).ToList();
        }

        public bool IsDuplicateName(string tenSp, int maSp)
        {
            return _context.SANPHAMs.Any(x =>
                x.TENSP.ToLower() == tenSp.Trim().ToLower() &&
                x.MASP != maSp
            );
        }

        public void XoaSanPham(int id)
        {            
            _context.Database.ExecuteSqlCommand("SET ROLE ALL");

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var sp = _context.SANPHAMs.FirstOrDefault(x => x.MASP == id);
                    if (sp == null)
                    {
                        throw new Exception("Sản phẩm không tồn tại hoặc đã bị xóa.");
                    }

                    var dsHinh = _context.HINHANHs.Where(h => h.MASP == id).ToList();
                    if (dsHinh.Count > 0)
                    {
                        _context.HINHANHs.RemoveRange(dsHinh);
                    }

                    _context.SANPHAMs.Remove(sp);

                    _context.SaveChanges();
                    transaction.Commit();
                }
                catch (DbUpdateException dbEx)
                {
                    transaction.Rollback();
                    HandleOracleException(dbEx);
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void AddWithImages(SANPHAM sp, List<HINHANH> danhSachHinh)
        {            
            _context.Database.ExecuteSqlCommand("SET ROLE ALL");

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
                catch (DbUpdateException dbEx)
                {
                    transaction.Rollback();
                    HandleOracleException(dbEx);
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public bool UpdateProduct(SANPHAM model, List<string> newImageNames, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                _context.Database.ExecuteSqlCommand("SET ROLE ALL");

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
            catch (DbUpdateException dbEx)
            {
                try
                {
                    HandleOracleException(dbEx);
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "Lỗi hệ thống: " + ex.Message;
                return false;
            }
        }

        public void DeleteImage(int maHinh)
        {
            try
            {
                _context.Database.ExecuteSqlCommand("SET ROLE ALL"); // Refresh quyền

                var img = _context.HINHANHs.Find(maHinh);
                if (img != null)
                {
                    _context.HINHANHs.Remove(img);
                    _context.SaveChanges();
                }
            }
            catch (DbUpdateException dbEx) { HandleOracleException(dbEx); }
            catch (Exception) { throw; }
        }

        public void AddImage(HINHANH img)
        {
            try
            {
                _context.Database.ExecuteSqlCommand("SET ROLE ALL"); // Refresh quyền

                _context.HINHANHs.Add(img);
                _context.SaveChanges();
            }
            catch (DbUpdateException dbEx) { HandleOracleException(dbEx); }
            catch (Exception) { throw; }
        }

        private void HandleOracleException(DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException;
            while (inner != null)
            {
                if (inner is OracleException oracleEx)
                {
                    switch (oracleEx.Number)
                    {
                        case 1031:
                            throw new Exception("Bạn không có quyền thực hiện thao tác này.");

                        case 2292:
                            throw new Exception("Không thể xóa: Dữ liệu này đang được sử dụng ở nơi khác (Hóa đơn/Phiếu nhập...).");

                        case 00001:
                            throw new Exception("Dữ liệu bị trùng lặp (Mã hoặc Tên đã tồn tại).");

                        case 6550:
                            throw new Exception("Lỗi Database PL/SQL: Có thể do thiếu quyền trên Trigger/Sequence.");

                        default:
                            throw new Exception($"Lỗi Oracle (Code: {oracleEx.Number}): {oracleEx.Message}");
                    }
                }
                inner = inner.InnerException;
            }

            if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("ORA-01031"))
            {
                throw new Exception("Bạn không có quyền thực hiện thao tác này.");
            }

            throw new Exception("Lỗi cập nhật dữ liệu: " + dbEx.Message);
        }
    }
}