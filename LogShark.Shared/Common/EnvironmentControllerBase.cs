using System;

namespace LogShark.Shared.Common
{
    public static class EnvironmentControllerBase
    {
        public static int SetExitCode(ExitCode exitCode)
        {
            Environment.ExitCode = (int)exitCode;
            return Environment.ExitCode;
        }
    }
}
