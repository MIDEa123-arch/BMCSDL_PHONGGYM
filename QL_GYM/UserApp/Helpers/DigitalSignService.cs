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

    }
}
