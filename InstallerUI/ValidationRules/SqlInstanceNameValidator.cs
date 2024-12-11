using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace InstallerUI.ValidationRules
{
    public class InstanceNameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var instanceName = value as string;
            if (string.IsNullOrEmpty(instanceName))
                return new ValidationResult(false, "Instance name is required.");

            // Check length
            if (instanceName.Length > 16)
                return new ValidationResult(false, "Instance name must be 16 characters or fewer.");

            // Check if the first character is a letter
            if (!char.IsLetter(instanceName[0]))
                return new ValidationResult(false, "The first character must be a letter.");

            // Check for valid subsequent characters (letters, numbers, $ or _)
            for (var i = 1; i < instanceName.Length; i++)
            {
                var ch = instanceName[i];
                if (!char.IsLetterOrDigit(ch) && ch != '$' && ch != '_')
                    return new ValidationResult(false, "Instance name contains invalid characters.");
            }

            // Check for leading or trailing underscore
            if (instanceName.StartsWith("_") || instanceName.EndsWith("_"))
                return new ValidationResult(false, "Instance name can't start or end with an underscore.");

            // Reserved keyword validation
            string[] reservedKeywords = { "Default", "MSSQLSERVER" };
            if (reservedKeywords.Any(
                    keyword => string.Equals(instanceName, keyword, StringComparison.OrdinalIgnoreCase)))
                return new ValidationResult(false, $"Instance name can't be {instanceName}.");

            // Check for disallowed special characters
            char[] disallowedChars = { '\\', ',', ':', ';', '\'', '&', '-', '@', '$' };
            if (instanceName.Any(ch => disallowedChars.Contains(ch)))
                return new ValidationResult(false, "Instance name contains disallowed special characters.");

            return ValidationResult.ValidResult;
        }
    }
}