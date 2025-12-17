using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UserApp.Repositories;
using UserApp.Models;

namespace UserApp.Controllers
{
    public class PhanCongPTController : Controller
    {
        private readonly KhachHangRepository _repository;

        public PhanCongPTController()
        {
            _repository = new KhachHangRepository();
        }

        public ActionResult Index()
        {
            var listKH = _repository.GetMyCustomers();
            ViewBag.MaKH = new SelectList(listKH, "MAKH", "TENKH");

            var listPT = _repository.GetDanhSachHLV();
            ViewBag.MaNV = new SelectList(listPT, "MANV", "TENNV");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int MaKH, int MaNV, int SoBuoi, decimal GiaMoiBuoi)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    bool ketQua = _repository.PhanCongPT(MaKH, MaNV, SoBuoi, GiaMoiBuoi);

                    if (ketQua)
                    {
                        TempData["Message"] = "Phân công PT thành công!";
                        TempData["Type"] = "success";
                    }
                    else
                    {
                        TempData["Message"] = "Không tìm thấy Khách hàng hoặc Nhân viên.";
                        TempData["Type"] = "danger";
                    }
                }
                catch (Exception ex)
                {
                    var fullErrorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        fullErrorMessage += " --> " + ex.InnerException.Message;
                        if (ex.InnerException.InnerException != null)
                        {
                            fullErrorMessage += " --> " + ex.InnerException.InnerException.Message;
                        }
                    }

                    // Hiển thị toàn bộ chuỗi lỗi để debug
                    TempData["Message"] = "Lỗi chi tiết: " + fullErrorMessage;
                    TempData["Type"] = "danger";
                }
            }
            else
            {
                TempData["Message"] = "Dữ liệu nhập vào không hợp lệ.";
                TempData["Type"] = "warning";
            }

            return RedirectToAction("Index");
        }
    }
}