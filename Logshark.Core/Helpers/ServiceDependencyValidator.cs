using log4net;
using Logshark.Core.Exceptions;
using Logshark.RequestModel.Config;
using ServiceStack.OrmLite;
using System;
using System.Reflection;

namespace Logshark.Core.Helpers
{
    public class ServiceDependencyValidator
    {
        protected readonly LogsharkConfiguration configuration;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ServiceDependencyValidator(LogsharkConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ValidateAllDependencies()
        {
            Log.Debug("Validating all configured service dependencies are available..");

            ValidatePostgresIsAvailable();
            ValidateMongoIsAvailable();
        }

        public void ValidatePostgresIsAvailable()
        {
            Log.DebugFormat("Validating that PostgreSQL database '{0}' is available..", configuration.PostgresConnectionInfo);

            try
            {
                IDbConnectionFactory connectionFactory = configuration.PostgresConnectionInfo.GetConnectionFactory(configuration.PostgresConnectionInfo.DefaultDatabase);
                using (connectionFactory.OpenDbConnection())
                {
                    Log.DebugFormat("Successfully opened a connection to PostgreSQL database '{0}'!", configuration.PostgresConnectionInfo);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Failed to open connection to PostgreSQL database '{0}': {1}", configuration.PostgresConnectionInfo, ex.Message);
                Log.Error(errorMessage);
                throw new ServiceDependencyUnavailableException("PostgreSQL", errorMessage, ex);
            }
        }

        public void ValidateMongoIsAvailable()
        {
            Log.DebugFormat("Validating that MongoDB database '{0}' is available..", configuration.MongoConnectionInfo);

            try
            {
                configuration.MongoConnectionInfo.GetClient().ListDatabases();
                Log.DebugFormat("Successfully opened a connection to MongoDB database '{0}'!", configuration.MongoConnectionInfo);
            }
            catch (Exception ex)
            {
                string errorMessage = String.Format("Failed to open connection to MongoDB database '{0}': {1}", string.Join(",", configuration.MongoConnectionInfo.Servers), ex.Message);
                Log.Error(errorMessage);
                throw new ServiceDependencyUnavailableException("MongoDB", errorMessage, ex);
            }
        }
    }
}