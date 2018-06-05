using log4net;
using Logshark.ConnectionModel.Mongo;
using Logshark.Core.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Logshark.Core.Controller.Parsing.Mongo
{
    internal class MongoParsedLogsetValidator : IParsedLogsetValidator
    {
        protected readonly MongoConnectionInfo mongoConnectionInfo;

        // These collections are automatically generated and will always contain data; we would like to ignore them.
        protected static readonly ISet<string> DefaultCollections = new HashSet<string>
        {
            "metadata"
        };

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MongoParsedLogsetValidator(MongoConnectionInfo mongoConnectionInfo)
        {
            this.mongoConnectionInfo = mongoConnectionInfo;
        }

        /// <summary>
        /// Validates that a given MongoDB database contains at least one document.
        /// </summary>
        /// <param name="mongoDatabaseName">The name of the MongoDB database to check.</param>
        public void ValidateDataExists(string mongoDatabaseName)
        {
            if (!ContainsData(mongoDatabaseName))
            {
                throw new ProcessingException(String.Format("MongoDB database {0} contains no valid log data!", mongoDatabaseName));
            }
        }

        /// <summary>
        /// Indicates whether the given MongoDB database contains at least one document.
        /// </summary>
        /// <param name="mongoDatabaseName">The name of the MongoDB database to check.</param>
        /// <returns>True if at least one document exists in the specified database.</returns>
        protected bool ContainsData(string mongoDatabaseName)
        {
            try
            {
                IMongoDatabase database = mongoConnectionInfo.GetDatabase(mongoDatabaseName);

                foreach (var collectionDocument in database.ListCollections().ToList())
                {
                    string collectionName = collectionDocument.GetValue("name").AsString;
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);

                    if (ContainsData(collection))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Encountered exception while validating contents of MongoDB database {0}: {1}", mongoDatabaseName, ex.Message);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether the given collection contains at least one document.
        /// </summary>
        protected bool ContainsData(IMongoCollection<BsonDocument> collection)
        {
            bool isDefaultCollection = DefaultCollections.Contains(collection.CollectionNamespace.CollectionName, StringComparer.InvariantCultureIgnoreCase);
            bool hasDocuments = collection.Find(Builders<BsonDocument>.Filter.Empty).Limit(1).FirstOrDefault() != null;

            return !isDefaultCollection && hasDocuments;
        }
    }
}