using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FileTransfer.ValidationRules
{
    public class IPAdressCheckRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var input = value.ToString();
            if (!Regex.IsMatch(input, @"^(((\d{1,2})|(1[0-9][0-9])|(2[0-4][0-9])|(25[0-5]))\.){3}((\d{1,2})|(1[0-9][0-9])|(2[0-4][0-9])|(25[0-5]))$"))
                return new ValidationResult(false, "输入不满足IP地址要求！");
            else
                return ValidationResult.ValidResult;
        }
    }
}
