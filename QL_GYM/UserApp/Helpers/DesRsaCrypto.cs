using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UserApp.Helpers
{
    public class DesRsaCrypto
    {
        public static void GenerateKeys(out string publicKeyXml, out string privateKeyXml)
        {
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                publicKeyXml = rsa.ToXmlString(false);
                privateKeyXml = rsa.ToXmlString(true);
            }
        }
        public void EncryptFile(string inputFilePath, string outputFilePath, string rsaPublicKeyXml)
        {
            //Tạo khóa DES ngẫu nhiên
            using (var des = new DESCryptoServiceProvider())
            {
                des.GenerateKey(); // Sinh Key ngẫu nhiên (8 bytes)
                des.GenerateIV();  // Sinh IV ngẫu nhiên (8 bytes)

                // 2. Mã hóa khóa DES bằng RSA
                byte[] encryptedDesKey;
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(rsaPublicKeyXml);
                    // Mã hóa Key của DES
                    encryptedDesKey = rsa.Encrypt(des.Key, false);
                }

                // Cấu trúc: [Độ dài Key RSA (4 byte)] + [Key DES đã mã hóa] + [IV (8 byte)] + [Dữ liệu file đã mã hóa]
                using (var fsOut = new FileStream(outputFilePath, FileMode.Create))
                {
                    // Ghi độ dài của Key đã mã hóa
                    byte[] lenBytes = BitConverter.GetBytes(encryptedDesKey.Length);
                    fsOut.Write(lenBytes, 0, 4);

                    // Ghi Key DES đã mã hóa
                    fsOut.Write(encryptedDesKey, 0, encryptedDesKey.Length);

                    // Ghi IV
                    fsOut.Write(des.IV, 0, des.IV.Length);

                    // Ghi nội dung file đã được mã hóa bằng DES
                    using (var cryptoStream = new CryptoStream(fsOut, des.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (var fsIn = new FileStream(inputFilePath, FileMode.Open))
                        {
                            fsIn.CopyTo(cryptoStream);
                        }
                    }
                }
            }
        }

        public void DecryptFile(string inputFilePath, string outputFilePath, string rsaPrivateKeyXml)
        {
            using (var fsIn = new FileStream(inputFilePath, FileMode.Open))
            {
                // 1. Đọc độ dài của Key DES đã mã hóa
                byte[] lenBytes = new byte[4];
                fsIn.Read(lenBytes, 0, 4);
                int lenKey = BitConverter.ToInt32(lenBytes, 0);

                // 2. Đọc Key DES đã mã hóa
                byte[] encryptedDesKey = new byte[lenKey];
                fsIn.Read(encryptedDesKey, 0, lenKey);

                // 3. Đọc IV (8 bytes mặc định của DES)
                byte[] iv = new byte[8];
                fsIn.Read(iv, 0, 8);

                // 4. Giải mã Key DES bằng RSA Private Key
                byte[] desKey;
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(rsaPrivateKeyXml);
                    desKey = rsa.Decrypt(encryptedDesKey, false);
                }

                // 5. Giải mã nội dung file bằng DES
                using (var des = new DESCryptoServiceProvider())
                {
                    des.Key = desKey;
                    des.IV = iv;

                    using (var cryptoStream = new CryptoStream(fsIn, des.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var fsOut = new FileStream(outputFilePath, FileMode.Create))
                        {
                            cryptoStream.CopyTo(fsOut);
                        }
                    }
                }
            }
        }
    }
}