using UserApp.Models;
using System.Collections.Generic;
using System.Linq;
using System;
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
    }
}
