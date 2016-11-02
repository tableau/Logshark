using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Logshark.Controller.Parsing.Validation
{
    internal class LogsetValidator
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // These collections are automatically generated and will always contain data, we would like to ignore them.
        private static readonly ISet<string> DefaultCollections = new HashSet<string>
        {
            "metadata"
        };

        public static bool MongoDatabaseContainsRecords(LogsharkRequest request)
        {
            IMongoDatabase database = request.Configuration.MongoConnectionInfo.GetDatabase(request.RunContext.MongoDatabaseName);

            try
            {
                foreach (var collectionDocument in database.ListCollections().ToList())
                {
                    string collectionName = collectionDocument.GetValue("name").AsString;
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);

                    if (MongoCollectionContainsRecords(collection))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Encountered exception while validating contents of Mongo database {0}: {1}", request.RunContext.MongoDatabaseName, ex.Message);
                return false;
            }

            return false;
        }

        public static bool MongoCollectionContainsRecords(IMongoCollection<BsonDocument> collection)
        {
            bool isDefaultCollection = DefaultCollections.Contains(collection.CollectionNamespace.CollectionName, StringComparer.InvariantCultureIgnoreCase);
            bool hasElements = collection.Find(Builders<BsonDocument>.Filter.Empty).Limit(1).FirstOrDefault() != null;

            return !isDefaultCollection && hasElements;
        }
    }
}