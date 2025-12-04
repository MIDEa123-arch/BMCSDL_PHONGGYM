using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.IO;
using System.Net.Mail;
using System.Web.Mvc;
using UserApp.Helpers;
using UserApp.Models;
using UserApp.Repositories;
using UserApp.ViewModel;

namespace UserApp.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly InvoiceRepository _invoiceRepo;
        private readonly QL_PHONGGYMEntities _db;

        public InvoiceController()
        {
            _db = new QL_PHONGGYMEntities();
            _invoiceRepo = new InvoiceRepository(_db);
        }

        [HttpGet]
        public ActionResult Sign(int id)
        {
            var invoiceData = _invoiceRepo.GetInvoiceById(id);
            if (invoiceData == null || invoiceData.Header == null)
                return Content($"Không tìm thấy hóa đơn số {id}!");

            var hoaDon = _db.HOADONs.Find(id);

            // Lấy root của project web/app
            string projectRoot = AppDomain.CurrentDomain.BaseDirectory;

            // Tạo folder "DanhSachHoaDon" bên trong project nếu chưa tồn tại
            string sharedFolder = Path.Combine(projectRoot, "DanhSachHoaDon");
            if (!Directory.Exists(sharedFolder))
            {
                Directory.CreateDirectory(sharedFolder);
            }

            string fileName = $"Invoice_{id}_{DateTime.Now.Ticks}.pdf";
            string pdfPath = Path.Combine(sharedFolder, fileName);

            if (hoaDon.TRANGTHAI == "Đã thanh toán")
            {
                CreatePdfInvoice(pdfPath, invoiceData);

                // Lưu PFX trong App_Data
                string appDataPath = Path.Combine(projectRoot, "App_Data");
                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                string pfxFileName = "GymAdmin.pfx";
                string pfxPath = Path.Combine(appDataPath, pfxFileName);

                var signer = new DigitalSignService();
                string sigPath = signer.SignFile(pdfPath, pfxPath, "123456", "CN=GymAdmin");

                // Lưu đường dẫn tương đối vào database
                string relativeFolder = "/DanhSachHoaDon";
                hoaDon.FILE_PATH = relativeFolder + "/" + fileName;
                hoaDon.SIGNATURE_PATH = relativeFolder + "/" + Path.GetFileName(sigPath);
                hoaDon.PUBLIC_KEY_USED = "GymAdmin";
                hoaDon.SIGNATURE_VERIFIED = false;
                hoaDon.CREATED_AT = DateTime.Now;

                _db.SaveChanges();

                SendInvoiceMail(
                    hoaDon.KHACHHANG.TENKH,
                    hoaDon.KHACHHANG.EMAIL,
                    pdfPath,
                    sigPath
                );
            }

            return RedirectToAction("ThanhToanThanhCong", "CartCheckout");
        }


        public void CreatePdfInvoice(string pdfPath, InvoiceFullData data)
        {
            var header = data.Header;

            using (FileStream fs = new FileStream(pdfPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Document doc = new Document(PageSize.A4, 25, 25, 25, 25);
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // Font tiếng Việt: đặt trong App_Data/Fonts
                string fontPath = Server.MapPath("~/App_Data/arial.ttf");
                if (!System.IO.File.Exists(fontPath))
                    throw new FileNotFoundException("Font arial.ttf chưa tồn tại trong App_Data");

                BaseFont bf = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                Font font = new Font(bf, 12);

                // Header
                doc.Add(new Paragraph("HỆ THỐNG GYM MASTER", font));
                doc.Add(new Paragraph($"Hóa đơn số: {header.MAHD}", font));
                doc.Add(new Paragraph($"Ngày: {header.NGAYLAP:dd/MM/yyyy HH:mm}", font));
                doc.Add(new Paragraph($"Khách: {header.TENKH}", font));
                doc.Add(new Paragraph($"SĐT: {header.SDT}", font));
                doc.Add(new Paragraph("-----------------------------------------------------", font));

                // Table chi tiết
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 4f, 1f, 2f, 2f });
                table.AddCell(new PdfPCell(new Phrase("Tên DV/SP", font)));
                table.AddCell(new PdfPCell(new Phrase("SL", font)));
                table.AddCell(new PdfPCell(new Phrase("Đơn giá", font)));
                table.AddCell(new PdfPCell(new Phrase("Thành tiền", font)));

                foreach (var item in data.Details)
                {
                    table.AddCell(new PdfPCell(new Phrase(item.TENSP, font)));
                    table.AddCell(new PdfPCell(new Phrase(item.SOLUONG.ToString(), font)));
                    table.AddCell(new PdfPCell(new Phrase(item.DONGIA.ToString("N0"), font)));
                    table.AddCell(new PdfPCell(new Phrase(item.THANHTIEN_SP.ToString("N0"), font)));
                }

                doc.Add(table);

                // Tổng tiền
                doc.Add(new Paragraph($"Tổng cộng: {header.TONGTIEN:N0}", font));
                doc.Add(new Paragraph($"Giảm giá: {header.GIAMGIA:N0}", font));
                doc.Add(new Paragraph($"Thanh toán: {header.THANHTIEN:N0}", font));
                doc.Add(new Paragraph("-----------------------------------------------------", font));
                doc.Add(new Paragraph("(Ký điện tử: CN=GymAdmin)", font));

                doc.Close();
            }
        }

        private void SendInvoiceMail(string customerName, string customerEmail, string pdfPath, string sigPath)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("thangdien0169@gmail.com", "The Gym");
            mail.To.Add(GiaiMa.GiaiMaCong(customerEmail, 6));
            mail.Subject = "Hóa đơn của bạn từ The Gym";
            mail.Body = $"Chào {customerName},\n\nXin vui lòng xem hóa đơn đính kèm.\n\nTrân trọng!";
            mail.Attachments.Add(new Attachment(pdfPath));
            mail.Attachments.Add(new Attachment(sigPath));
            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587); //Cổng của gmail
            smtp.Credentials = new System.Net.NetworkCredential("thangdien0169@gmail.com", "wfjrxxlksiwzvifm");
            smtp.EnableSsl = true; //Chế độ mã hóa
            smtp.Send(mail);
        }
    }
}
