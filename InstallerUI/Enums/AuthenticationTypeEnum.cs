using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerUI.Enums
{
    public enum AuthenticationTypeEnum
    {
        [Description("Windows Authentication")]
        WindowsAuthentication = 0,
        [Description("SQL Server Authentication")]
        SQLServerAuthentication = 1
    }
}
