using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.Filestore.Helpers
{
    internal class MongoQueryFilestoreHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> FilestoreByFile(IMongoCollection<BsonDocument> collection)
        {
            var filenameQuery = Query.Regex("file", new BsonRegularExpression("filestore.*"));
            return filenameQuery;
        }

        public static ProjectionDefinition<BsonDocument> IgnoreUnusedFilestoreFieldsProjection()
        {
            // Include all fields except for the following.
            return Builders<BsonDocument>.Projection.Exclude("file_path");
        }
    }
}