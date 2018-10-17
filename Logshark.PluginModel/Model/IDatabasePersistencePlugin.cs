using ServiceStack.OrmLite;

namespace Logshark.PluginModel.Model
{
    public interface IDatabasePersistencePlugin : IPlugin
    {
        IDbConnectionFactory OutputDatabaseConnectionFactory { get; set; }

        bool IsDatabaseRequired { get; }
    }
}