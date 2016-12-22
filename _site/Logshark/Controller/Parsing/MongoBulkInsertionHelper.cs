using log4net;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Logshark.Controller.Parsing
{
    internal static class MongoBulkInsertionHelper
    {
        public static Thread CreateInsertionThread(IMongoDatabase mongoDatabase, ICollection<BsonDocument> logDocuments, string collectionName, int maxRetries)
        {
            Thread insertionThread = new Thread(() => MongoBulkInsertionTask.DoInsertLogDocuments(mongoDatabase, logDocuments, collectionName, maxRetries));
            insertionThread.Start();
            return insertionThread;
        }
    }

    internal static class MongoBulkInsertionTask
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly InsertManyOptions InsertManyOptions = new InsertManyOptions { BypassDocumentValidation = true, IsOrdered = false };

        public static void DoInsertLogDocuments(IMongoDatabase mongoDatabase, ICollection<BsonDocument> logDocuments, string collectionName, int maxRetries)
        {
            if (logDocuments.Count == 0)
            {
                return;
            }

            bool success = false;
            int retries = 0;

            while (!success && retries <= maxRetries)
            {
                try
                {
                    IMongoCollection<BsonDocument> collection = mongoDatabase.GetCollection<BsonDocument>(collectionName);

                    if (retries >= 1)
                    {
                        Log.WarnFormat("Retrying insertion into {0} [Attempt {1} of {2}]", collectionName, retries, maxRetries);
                    }

                    collection.InsertMany(logDocuments, InsertManyOptions);
                    success = true;
                }
                catch (Exception ex)
                {
                    retries++;
                    Log.ErrorFormat("Error inserting into {0}: {1}", collectionName, ex.Message);
                }
            }
        }
    }
}