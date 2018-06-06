using System;

namespace Logshark.ConnectionModel.TableauServer
{
    public interface ITableauServerConnectionInfo
    {
        string Site { get; }
        int PublishingTimeoutSeconds { get; }
        Uri Uri { get; }
    }
}