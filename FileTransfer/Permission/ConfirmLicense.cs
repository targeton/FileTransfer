using System;
using System.Security.Cryptography;
using System.Text;

namespace FileTransfer.Permission
{
    public class ConfirmLicense
    {
        private string _rsaPublicString = @"<RSAKeyValue><Modulus>km0iwTIN7hvhTkTlNOaV6oeOBzbeAYESdtd8SdiP8P1UgLrN8/mKEmVMfHg0A/qyrOYHG9jaXW3AN5B6KBBq+S2zosBrmUUCoTd+83kkPG9LGscmt05gj6Cwh/zhNeWxv14UJixYpv/7cG/4mWUyWHH4whoSj20FDmARSwLUtV8=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        public bool VerifyData(byte[] license)
        {
            var machineCode = new MachineCode();
            string machineCodeString = machineCode.GetMachineCode();
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(_rsaPublicString);
                    return rsa.VerifyData(Encoding.Unicode.GetBytes(machineCodeString), "SHA", license);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
