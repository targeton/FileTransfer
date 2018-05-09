using System;
using System.Security.Cryptography;
using System.Text;

namespace LicenseGenerator
{
    public class Generator
    {
        private string _rsaPrivateString = @"<RSAKeyValue><Modulus>km0iwTIN7hvhTkTlNOaV6oeOBzbeAYESdtd8SdiP8P1UgLrN8/mKEmVMfHg0A/qyrOYHG9jaXW3AN5B6KBBq+S2zosBrmUUCoTd+83kkPG9LGscmt05gj6Cwh/zhNeWxv14UJixYpv/7cG/4mWUyWHH4whoSj20FDmARSwLUtV8=</Modulus><Exponent>AQAB</Exponent><P>ysk30X8ENWHi4RMlFv2xMfvIJc9QPYiByDAbuTNDPFLO4tpISJiR0+jr6bbqy3EE1tv5W2fgSuV5JK011t9Dlw==</P><Q>uNnHjUBWRhBIj2lFxyTztKVzRZidxULFnVvGDAjV6EmWNKZ8ShnMFO1P7GWX+8DJzzGyBc9c352Ghaie7By1eQ==</Q><DP>W8S3p+zNIMNNgwHA9SiVecMxjjrFWzNdWBS9VxBlyvTGf069C21QARAVQszucGaTBBDERaM6k2pJalmgVb7vvQ==</DP><DQ>j8fVj+kbMiQ4TFR2EhCL/1cx8lBjZ6woSd24jmPQ/n0+eHWG95xZQW3VXOso7Ilob+EXt60zcDv3Br/B3aX3AQ==</DQ><InverseQ>wKxVglZJC3rkgygKstwj893gpTifyjKPB9yANRckBI5YEqiE2P+eYFVdBoOXQrihQjAJaeYDW433ap7yv0fLeQ==</InverseQ><D>O/qCWvrC4/79mk70SGgPjqL6FZBu/dS+GHoKCkGwLHnIjHZ4eHCGEyT1YKLoQ50EZXhP/yMjx1N2ggwnTZP0DQDXkeUnZgywpN3g9s3vp/+t/bUaAGB6hLzdNUzj0FRzB6iTJE6I47WcYuCHW6ha9hbWdFdCV36wwY/FexmKERE=</D></RSAKeyValue>";
        public byte[] CreateLicense(string machineCodeString)
        {
            byte[] result = null;
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(_rsaPrivateString);
                    byte[] dataToSign = Encoding.Unicode.GetBytes(machineCodeString);
                    result = rsa.SignData(dataToSign, "SHA");
                }
            }
            catch (Exception)
            {

            }
            return result;
        }
    }
}
