using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Web.Hosting;

namespace UserApp.Helpers
{
    public class DigitalSignService
    {
        public string SignFile(string inputFilePath, string pfxAbsolutePath, string password, string expectedSubject = "CN=GymAdmin")
        {
            // Nếu file PFX chưa tồn tại, tự tạo
            if (!File.Exists(pfxAbsolutePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pfxAbsolutePath)); // đảm bảo thư mục tồn tại
                CreateSelfSignedPfx(pfxAbsolutePath, password, expectedSubject);
            }

            // Kiểm tra file gốc
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException($"Không tìm thấy file gốc: {inputFilePath}");

            using (var cert = new X509Certificate2(pfxAbsolutePath, password, X509KeyStorageFlags.Exportable))
            {
                if (!string.IsNullOrEmpty(expectedSubject) && !cert.Subject.Contains(expectedSubject))
                    throw new Exception($"File PFX không hợp lệ! Mong muốn: {expectedSubject}, thực tế: {cert.Subject}");

                byte[] data = File.ReadAllBytes(inputFilePath);
                var contentInfo = new ContentInfo(data);
                var signedCms = new SignedCms(contentInfo, true);
                var cmsSigner = new CmsSigner(cert);
                signedCms.ComputeSignature(cmsSigner);

                byte[] signedBytes = signedCms.Encode();
                string outputSignaturePath = inputFilePath + ".sig";
                File.WriteAllBytes(outputSignaturePath, signedBytes);

                return outputSignaturePath;
            }
        }


        private void CreateSelfSignedPfx(string pfxPath, string password, string subjectName)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                var req = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);    
                var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));

                File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pfx, password));
            }
        }
       
        public bool VerifyFile(string originalFilePath, string signatureFilePath, string pfxPath, string pfxPassword, out string message)
        {
            message = "";
            try
            {
                // 1. Kiểm tra file đầu vào
                if (!File.Exists(originalFilePath))
                {
                    message = "Không tìm thấy file gốc trên server.";
                    return false;
                }
                if (!File.Exists(signatureFilePath))
                {
                    message = "Không tìm thấy file chữ ký (.sig).";
                    return false;
                }

                // 2. Load file PFX để lấy thông tin chủ nhân (để so sánh)
                X509Certificate2 certFromPfx;
                try
                {
                    certFromPfx = new X509Certificate2(pfxPath, pfxPassword);
                }
                catch
                {
                    message = "Mật khẩu file PFX không đúng hoặc file PFX lỗi.";
                    return false;
                }

                // 3. Đọc dữ liệu để verify
                byte[] originalData = File.ReadAllBytes(originalFilePath);
                byte[] signatureData = File.ReadAllBytes(signatureFilePath);

                ContentInfo contentInfo = new ContentInfo(originalData);
                // true = Detached (Quan trọng: Phải khớp với lúc Sign)
                SignedCms signedCms = new SignedCms(contentInfo, true);

                // 4. Giải mã và Kiểm tra toán học
                signedCms.Decode(signatureData);

                try
                {
                    // true = verifySignatureOnly (Bỏ qua check CA Trust vì bạn dùng Self-Signed)
                    signedCms.CheckSignature(true);
                }
                catch (CryptographicException)
                {
                    message = "CẢNH BÁO: Dữ liệu file gốc đã bị thay đổi hoặc chữ ký không đúng!";
                    return false;
                }

                // 5. Kiểm tra xem người ký có đúng là người sở hữu file PFX này không?
                foreach (var signer in signedCms.SignerInfos)
                {
                    if (signer.Certificate != null &&
                        signer.Certificate.Thumbprint == certFromPfx.Thumbprint)
                    {
                        message = "Hợp lệ: File toàn vẹn và được ký bởi đúng PFX này.";
                        return true;
                    }
                }

                message = "File toàn vẹn, NHƯNG được ký bởi một người khác (không khớp file PFX này).";
                return false;
            }
            catch (Exception ex)
            {
                message = "Lỗi hệ thống: " + ex.Message;
                return false;
            }
        }

    }
}
