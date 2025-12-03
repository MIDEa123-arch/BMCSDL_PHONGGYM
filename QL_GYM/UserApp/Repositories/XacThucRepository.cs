using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UserApp.Models;

namespace UserApp.Repositories
{
    public class XacThucRepository
    {
        private readonly QL_PHONGGYMEntities _context;      

        public XacThucRepository(bool admin)
        {
            _context = new QL_PHONGGYMEntities(admin);
        }

        public bool Verify(int logId, string otpInput)
        {
            int key = 12345678; 
            string encryptedInput = MaHoa.MahoaDes(otpInput, key);

            var logRecord = _context.LOGINHISTORies.FirstOrDefault(x => x.ID == logId);
            if (logRecord != null && logRecord.OTPCODE == encryptedInput && logRecord.OTPEXPIRE > DateTime.Now)
            {
                logRecord.STATUS = 1;
                _context.SaveChanges();

                return true;
            }
            return false;
        }

        public string CheckIpSecurity(int userId, string currentIp, out int logId)
        {
            logId = 0;
            var lastLogin = _context.LOGINHISTORies
                               .Where(x => x.USERID == userId && x.STATUS == 1)
                               .OrderByDescending(x => x.LOGINTIME)
                               .FirstOrDefault();

            if (lastLogin == null || lastLogin.IPADDRESS == currentIp)
            {
                var safeLog = new LOGINHISTORY
                {
                    USERID = userId,
                    IPADDRESS = currentIp,
                    LOGINTIME = DateTime.Now,
                    STATUS = 1
                };
                _context.LOGINHISTORies.Add(safeLog);
                _context.SaveChanges();

                return null;
            }

            string otp = new Random().Next(100000, 999999).ToString();
            int key = 12345678;
            string encryptedOtp = MaHoa.MahoaDes(otp, key);

            var pendingLog = new LOGINHISTORY
            {
                USERID = userId,
                IPADDRESS = currentIp,
                LOGINTIME = DateTime.Now,
                STATUS = 0,
                OTPCODE = encryptedOtp,
                OTPEXPIRE = DateTime.Now.AddMinutes(5)
            };
            _context.LOGINHISTORies.Add(pendingLog);
            _context.SaveChanges();

            logId = (int)pendingLog.ID; 
            return otp; 
        }
    }
}