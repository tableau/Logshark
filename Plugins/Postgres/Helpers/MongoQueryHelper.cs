using MongoDB.Bson;
using MongoDB.Driver;

namespace Logshark.Plugins.Postgres.Helpers
{
    internal class MongoQueryHelper
    {
        private static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        public static FilterDefinition<BsonDocument> LogLinesByFile(IMongoCollection<BsonDocument> collection)
        {
            var filenameQuery = Query.Regex("file", new BsonRegularExpression("postgresql-*"));
            return filenameQuery;
        }

        public static ProjectionDefinition<BsonDocument> IgnoreUnusedFieldsProjection()
        {
            return Builders<BsonDocument>.Projection.Exclude("ts_offset")
                                                    .Exclude("file_path");
        }
    }
}
