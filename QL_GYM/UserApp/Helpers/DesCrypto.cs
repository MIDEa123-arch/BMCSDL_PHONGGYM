using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UserApp.Helpers
{
    static class DesCrypto
    {
        public static readonly byte[] IV = { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };

        public static void EncryptFile(string inputFile, string outputFile, byte[] key)
        {
            byte[] fileBytes = File.ReadAllBytes(inputFile); // đọc toàn bộ file
            using (DES des = DES.Create())
            using (ICryptoTransform encryptor = des.CreateEncryptor(key, IV))
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(fileBytes, 0, fileBytes.Length);
                cs.FlushFinalBlock();
                File.WriteAllBytes(outputFile, ms.ToArray()); // lưu file đã mã hóa
            }
        }

        public static void DecryptFile(string inputFile, string outputFile, byte[] key)
        {
            byte[] cipherBytes = File.ReadAllBytes(inputFile); // đọc file đã mã hóa
            using (DES des = DES.Create())
            using (ICryptoTransform decryptor = des.CreateDecryptor(key, IV))
            using (MemoryStream ms = new MemoryStream(cipherBytes))
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
            {
                cs.CopyTo(fsOut); // ghi ra file giải mã
            }
        }

    }
}
