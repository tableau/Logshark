using MongoDB.Driver;

namespace Logshark.PluginLib.Processors
{
    public class QueryDefinition<TDocument>
    {
        public FilterDefinition<TDocument> Query { get; private set; }

        public SortDefinition<TDocument> Sort { get; private set; }

        public int? Limit { get; private set; }

        public QueryDefinition(FilterDefinition<TDocument> query)
        {
            Query = query;
        }

        public QueryDefinition(FilterDefinition<TDocument> query, SortDefinition<TDocument> sort, int? limit)
        {
            Query = query;
            Sort = sort;
            Limit = limit;
        }

        public IFindFluent<TDocument, TDocument> BuildQuery(IMongoCollection<TDocument> collection)
        {
            var fluent = collection.Find(Query);

            if (Sort != null)
            {
                fluent = fluent.Sort(Sort);
            }

            if (Limit.HasValue && Limit.Value >= 0)
            {
                fluent = fluent.Limit(Limit);
            }

            return fluent;
        }
    }
}