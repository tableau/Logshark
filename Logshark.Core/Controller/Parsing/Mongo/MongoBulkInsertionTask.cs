using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Logshark.Core.Controller.Parsing.Mongo
{
    internal static class MongoBulkInsertionTask
    {
        private static readonly InsertManyOptions InsertManyOptions = new InsertManyOptions { BypassDocumentValidation = true, IsOrdered = false };

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static bool InsertDocuments(ICollection<BsonDocument> documents, IMongoCollection<BsonDocument> collection, int maxRetries)
        {
            if (documents.Count == 0)
            {
                return false;
            }

            bool success = false;
            int retries = 0;

            while (!success && retries <= maxRetries)
            {
                try
                {
                    if (retries >= 1)
                    {
                        Log.DebugFormat("Retrying insertion into {0} [Attempt {1} of {2}]", collection.CollectionNamespace.CollectionName, retries, maxRetries);
                    }

                    collection.InsertMany(documents, InsertManyOptions);
                    success = true;
                }
                catch (Exception ex)
                {
                    retries++;
                    Log.ErrorFormat("Error inserting into {0}: {1}", collection.CollectionNamespace.CollectionName, ex.Message);
                }
            }

            return success;
        }
    }
}