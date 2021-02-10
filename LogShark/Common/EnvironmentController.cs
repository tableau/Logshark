using LogShark.Containers;
using LogShark.Shared.Common;

namespace LogShark.Common
{
    public static class EnvironmentController
    {
        public static int SetExitCode(RunSummary runSummary, bool suppressNonTransientErrors)
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
                    return SetExitCode(suppressNonTransientErrors ? ExitCode.OK : ExitCode.ERROR);
                default:
                    return SetExitCode(ExitCode.ERROR_UNKNOWN);
            }
        }

        public static int SetExitCode(ExitCode exitCode)
        {
            return EnvironmentControllerBase.SetExitCode(exitCode);
        }
    }
}