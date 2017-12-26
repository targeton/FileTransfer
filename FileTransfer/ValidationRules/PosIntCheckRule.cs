using System.Windows.Controls;

namespace FileTransfer.ValidationRules
{
    public class PosIntCheckRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var input = value.ToString();
            int result = -1;
            if (int.TryParse(input, out result) && (result >= 0 && result <= 65535))
                return ValidationResult.ValidResult;
            else
                return new ValidationResult(false, "输入应为端口范围（0-65535）！");
        }
    }
}
