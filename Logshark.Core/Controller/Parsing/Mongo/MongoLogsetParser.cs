using LogParsers.Base;
using Logshark.ConnectionModel.Helpers;
using Logshark.ConnectionModel.Mongo;
using Logshark.Core.Controller.Parsing.Mongo.Metadata;
using Logshark.RequestModel.Config;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Core.Controller.Parsing.Mongo
{
    internal class MongoLogsetParser : LogsetParser
    {
        protected readonly MongoConnectionInfo mongoConnectionInfo;
        protected readonly MongoLogProcessingMetadataWriter metadataWriter;

        public MongoLogsetParser(MongoConnectionInfo mongoConnectionInfo, LogsharkTuningOptions tuningOptions)
            : base(tuningOptions)
        {
            this.mongoConnectionInfo = mongoConnectionInfo;
            metadataWriter = new MongoLogProcessingMetadataWriter(mongoConnectionInfo);
        }

        protected override void Initialize(LogsetParsingRequest request)
        {
            IMongoDatabase database = mongoConnectionInfo.GetDatabase(request.LogsetHash);
            IParserFactory parserFactory = request.ArtifactProcessor.GetParserFactory(request.Target);

            var metadata = new LogProcessingMetadata(request);
            metadataWriter.Write(metadata, request.LogsetHash);

            CreateMongoDbCollections(request.CollectionsToParse, database, parserFactory);
        }

        protected override IDocumentWriter GetDocumentWriter(LogFileContext file, string collectionName, string logsetHash)
        {
            return new MongoDocumentBufferedWriter(mongoConnectionInfo.GetDatabase(logsetHash), collectionName);
        }

        protected override IDisposable GetProcessingWrapper(LogsetParsingRequest request)
        {
            return new MongoProcessingHeartbeatTimer(metadataWriter, request.LogsetHash);
        }

        protected override IParsedLogsetValidator GetValidator()
        {
            return new MongoParsedLogsetValidator(mongoConnectionInfo);
        }

        protected override void Finalize(LogsetParsingRequest request, LogsetParsingResult result)
        {
            var metadata = new LogProcessingMetadata(request)
            {
                ProcessedSuccessfully = true,
                ProcessedSize = result.ParsedDataVolumeBytes,
                FailedFileParses = result.FailedFileParses
            };

            metadataWriter.Write(metadata, request.LogsetHash);
            metadataWriter.WriteMasterMetadataRecord(metadata);
        }

        /// <summary>
        /// Creates all of the required MongoDB collections that this logset requires.
        /// </summary>
        protected ISet<string> CreateMongoDbCollections(ISet<string> requestedCollections, IMongoDatabase database, IParserFactory parserFactory)
        {
            IDictionary<string, ISet<string>> collectionIndexMap = BuildCollectionIndexMap(requestedCollections, parserFactory);

            // Create collections & indexes using the dictionary.
            ISet<string> collectionsCreated = new SortedSet<string>();
            foreach (var collection in collectionIndexMap)
            {
                var collectionName = collection.Key;
                ISet<string> indexes = collection.Value;

                IMongoCollection<BsonDocument> dbCollection = database.GetCollection<BsonDocument>(collectionName);
                collectionsCreated.Add(collectionName);

                foreach (var index in indexes)
                {
                    var indexKeysBuilder = new IndexKeysDefinitionBuilder<BsonDocument>();
                    CreateIndexOptions indexOptions = new CreateIndexOptions { Sparse = false };
                    dbCollection.Indexes.CreateOne(indexKeysBuilder.Ascending(index), indexOptions);
                }

                // If we are working against a sharded Mongo cluster, we need to explicitly shard each collection.
                if (mongoConnectionInfo.ConnectionType == MongoConnectionType.ShardedCluster)
                {
                    MongoAdminHelper.EnableShardingOnCollectionIfNotEnabled(mongoConnectionInfo.GetClient(), database.DatabaseNamespace.DatabaseName, collectionName);
                }
            }

            return collectionsCreated;
        }

        protected IDictionary<string, ISet<string>> BuildCollectionIndexMap(ISet<string> requestedCollections, IParserFactory parserFactory)
        {
            // Maps collection names to the set of defined indexed fields for that collection
            var collections = new Dictionary<string, ISet<string>>();

            // Stuff collection names & indexes into the dictionary, deduping in the process.
            foreach (var parser in parserFactory.GetAllParsers())
            {
                var collectionName = parser.CollectionSchema.CollectionName.ToLowerInvariant();

                if (!collections.ContainsKey(collectionName) && requestedCollections.Contains(collectionName))
                {
                    collections.Add(collectionName, new HashSet<string>());
                }

                // Add indexes.
                if (collections.ContainsKey(collectionName))
                {
                    foreach (var index in parser.CollectionSchema.Indexes)
                    {
                        collections[collectionName].Add(index);
                    }
                }
            }

            return collections;
        }
    }
}