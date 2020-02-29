using LogShark.Containers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace LogShark.Common
{
    public enum ExitCode
    {
        [Description("The operation completed successfully.")]
        OK = 0,

        [Description("An error occurred.")]
        ERROR = 1,

        [Description("A transient error occurred.")]
        ERROR_TRANSIENT = 2,

        [Description("A unknown error occurred.")]
        ERROR_UNKNOWN = 3,
    }
}
