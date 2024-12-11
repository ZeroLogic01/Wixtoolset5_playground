using System.Globalization;
using System.Windows.Controls;

namespace InstallerUI.ValidationRules
{

    public class ConditionalNotEmptyValidationRule : ValidationRule
    {
        public PasswordBox PasswordBox { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (PasswordBox != null && PasswordBox.IsEnabled && string.IsNullOrWhiteSpace((value ?? "").ToString()))
            {
                return new ValidationResult(false, "Field cannot be empty.");
            }
            return ValidationResult.ValidResult;
        }
    }

}
