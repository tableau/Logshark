using LogShark.Containers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogShark.Common
{
    public static class EnvironmentController
    {
        public static int SetExitCode(RunSummary runSummary, bool suppressNontransientErrors)
        {
            if (runSummary.IsSuccess)
            {
                return SetExitCode(ExitCode.OK);
            }
            
            switch (runSummary.IsTransient)
            {
                case true:
                    return SetExitCode(ExitCode.ERROR_TRANSIENT);
                case false:
                    return SetExitCode(suppressNontransientErrors ? ExitCode.OK : ExitCode.ERROR);
                case null:
                default:
                    return SetExitCode(ExitCode.ERROR_UNKNOWN);
            }
        }

        public static int SetExitCode(ExitCode exitCode)
        {

            Environment.ExitCode = (int)exitCode;
            return Environment.ExitCode;
        }
    }
}
