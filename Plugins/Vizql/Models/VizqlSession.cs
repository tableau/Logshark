using Logshark.Plugins.Vizql.Models.Events.Caching;
using Logshark.Plugins.Vizql.Models.Events.Compute;
using Logshark.Plugins.Vizql.Models.Events.Connection;
using Logshark.Plugins.Vizql.Models.Events.Error;
using Logshark.Plugins.Vizql.Models.Events.Etc;
using Logshark.Plugins.Vizql.Models.Events.Performance;
using Logshark.Plugins.Vizql.Models.Events.Query;
using Logshark.Plugins.Vizql.Models.Events.Render;
using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Vizql.Models
{
    public abstract class VizqlSession
    {
        [AutoIncrement]
        [PrimaryKey]
        public int Id { get; set; }

        [Index(Unique = true)]
        public string VizqlSessionId { get; set; }

        public Guid LogsetHash { get; protected set; }

        //Errors
        [Ignore]
        public IList<VizqlErrorEvent> ErrorEvents { get; private set; }

        //Performance
        [Ignore]
        public IList<VizqlPerformanceEvent> PerformanceEvents { get; private set; }

        //Connections
        [Ignore]
        public IList<VizqlConstructProtocol> ConstructProtocolEvents { get; private set; }

        [Ignore]
        public IList<VizqlConstructProtocolGroup> ConstructProtocolGroupEvents { get; private set; }

        [Ignore]
        public IList<VizqlDsConnect> DsConnectEvents { get; private set; }

        //Caching
        [Ignore]
        public IList<VizqlEcDrop> EcDropEvents { get; private set; }

        [Ignore]
        public IList<VizqlEcStore> EcStoreEvents { get; private set; }

        [Ignore]
        public IList<VizqlEcLoad> EcLoadEvents { get; private set; }

        [Ignore]
        public IList<VizqlEqcStore> EqcStoreEvents { get; private set; }

        [Ignore]
        public IList<VizqlEqcLoad> EqcLoadEvents { get; private set; }

        //Query
        [Ignore]
        public IList<VizqlDsInterpretMetadata> DsInterpretMetadataEvents { get; private set; }

        [Ignore]
        public IList<VizqlQpBatchSummary> QpBatchSummaryEvents { get; private set; }

        [Ignore]
        public IList<VizqlEndQuery> EndQueryEvents { get; private set; }

        [Ignore]
        public IList<VizqlQpQueryEnd> QpQueryEndEvents { get; private set; }

        [Ignore]
        public IList<VizqlEndSqlTempTableTuplesCreate> EndSqlTempTableTuplesCreateEvents { get; private set; }

        [Ignore]
        public IList<VizqlEndPrepareQuickFilterQueries> EndPrepareQuickFilterQueriesEvents { get; private set; }

        [Ignore]
        public IList<VizqlSetCollation> SetCollationEvents { get; private set; }

        [Ignore]
        public IList<VizqlProcessQuery> ProcessQueryEvents { get; private set; }

        //Compute
        [Ignore]
        public IList<VizqlEndComputeQuickFilterState> EndComputeQuickFilterStateEvents { get; private set; }

        //Render
        [Ignore]
        public IList<VizqlEndUpdateSheet> EndUpdateSheetEvents { get; private set; }

        //Message
        [Ignore]
        public IList<VizqlMessage> MessageEvents { get; private set; }

        //Etc
        [Ignore]
        public IList<VizqlEtc> EtcEvents { get; private set; }

        protected void CreateEventCollections()
        {
            //Errors
            ErrorEvents = new List<VizqlErrorEvent>();

            //Performance
            PerformanceEvents = new List<VizqlPerformanceEvent>();

            //Query
            DsInterpretMetadataEvents = new List<VizqlDsInterpretMetadata>();
            QpBatchSummaryEvents = new List<VizqlQpBatchSummary>();
            EndQueryEvents = new List<VizqlEndQuery>();
            QpQueryEndEvents = new List<VizqlQpQueryEnd>();
            EndSqlTempTableTuplesCreateEvents = new List<VizqlEndSqlTempTableTuplesCreate>();
            EndPrepareQuickFilterQueriesEvents = new List<VizqlEndPrepareQuickFilterQueries>();
            SetCollationEvents = new List<VizqlSetCollation>();
            ProcessQueryEvents = new List<VizqlProcessQuery>();

            //Connection
            ConstructProtocolEvents = new List<VizqlConstructProtocol>();
            ConstructProtocolGroupEvents = new List<VizqlConstructProtocolGroup>();
            DsConnectEvents = new List<VizqlDsConnect>();

            //Caching
            EcDropEvents = new List<VizqlEcDrop>();
            EcStoreEvents = new List<VizqlEcStore>();
            EcLoadEvents = new List<VizqlEcLoad>();
            EqcStoreEvents = new List<VizqlEqcStore>();
            EqcLoadEvents = new List<VizqlEqcLoad>();

            //Compute
            EndComputeQuickFilterStateEvents = new List<VizqlEndComputeQuickFilterState>();

            //Render
            EndUpdateSheetEvents = new List<VizqlEndUpdateSheet>();

            //Message
            MessageEvents = new List<VizqlMessage>();

            //Etc
            EtcEvents = new List<VizqlEtc>();
        }

        public void AppendEvent(VizqlEvent vizqlEvent)
        {
            if (String.IsNullOrEmpty(vizqlEvent.VizqlSessionId))
            {
                vizqlEvent.VizqlSessionId = VizqlSessionId;
            }

            //Performance
            if (vizqlEvent.GetElapsedTimeInSeconds().HasValue)
            {
                PerformanceEvents.Add(new VizqlPerformanceEvent(vizqlEvent));
            }

            //Error
            if (vizqlEvent is VizqlErrorEvent)
            {
                ErrorEvents.Add(vizqlEvent as VizqlErrorEvent);
            }

            //Connections
            else if (vizqlEvent is VizqlConstructProtocol)
            {
                ConstructProtocolEvents.Add(vizqlEvent as VizqlConstructProtocol);
            }
            else if (vizqlEvent is VizqlConstructProtocolGroup)
            {
                ConstructProtocolGroupEvents.Add(vizqlEvent as VizqlConstructProtocolGroup);
            }
            else if (vizqlEvent is VizqlDsConnect)
            {
                DsConnectEvents.Add(vizqlEvent as VizqlDsConnect);
            }

            //Caching
            else if (vizqlEvent is VizqlEcDrop)
            {
                EcDropEvents.Add(vizqlEvent as VizqlEcDrop);
            }
            else if (vizqlEvent is VizqlEcLoad)
            {
                EcLoadEvents.Add(vizqlEvent as VizqlEcLoad);
            }
            else if (vizqlEvent is VizqlEcStore)
            {
                EcStoreEvents.Add(vizqlEvent as VizqlEcStore);
            }
            else if (vizqlEvent is VizqlEqcLoad)
            {
                EqcLoadEvents.Add(vizqlEvent as VizqlEqcLoad);
            }
            else if (vizqlEvent is VizqlEqcStore)
            {
                EqcStoreEvents.Add(vizqlEvent as VizqlEqcStore);
            }

            //Query
            else if (vizqlEvent is VizqlDsInterpretMetadata)
            {
                DsInterpretMetadataEvents.Add(vizqlEvent as VizqlDsInterpretMetadata);
            }
            else if (vizqlEvent is VizqlEndQuery)
            {
                EndQueryEvents.Add(vizqlEvent as VizqlEndQuery);
            }
            else if (vizqlEvent is VizqlQpBatchSummary)
            {
                QpBatchSummaryEvents.Add(vizqlEvent as VizqlQpBatchSummary);
            }
            else if (vizqlEvent is VizqlQpQueryEnd)
            {
                QpQueryEndEvents.Add(vizqlEvent as VizqlQpQueryEnd);
            }
            else if (vizqlEvent is VizqlEndPrepareQuickFilterQueries)
            {
                EndPrepareQuickFilterQueriesEvents.Add(vizqlEvent as VizqlEndPrepareQuickFilterQueries);
            }
            else if (vizqlEvent is VizqlEndSqlTempTableTuplesCreate)
            {
                EndSqlTempTableTuplesCreateEvents.Add(vizqlEvent as VizqlEndSqlTempTableTuplesCreate);
            }
            else if (vizqlEvent is VizqlSetCollation)
            {
                SetCollationEvents.Add(vizqlEvent as VizqlSetCollation);
            }
            else if (vizqlEvent is VizqlProcessQuery)
            {
                ProcessQueryEvents.Add(vizqlEvent as VizqlProcessQuery);
            }

            //Compute
            else if (vizqlEvent is VizqlEndComputeQuickFilterState)
            {
                EndComputeQuickFilterStateEvents.Add(vizqlEvent as VizqlEndComputeQuickFilterState);
            }

            //Render
            else if (vizqlEvent is VizqlEndUpdateSheet)
            {
                EndUpdateSheetEvents.Add(vizqlEvent as VizqlEndUpdateSheet);
            }

            //Message
            else if (vizqlEvent is VizqlMessage)
            {
                MessageEvents.Add(vizqlEvent as VizqlMessage);
            }

            //Etc
            else if (vizqlEvent is VizqlEtc)
            {
                EtcEvents.Add(vizqlEvent as VizqlEtc);
            }
        }

        public bool ContainsEvents()
        {
            return (ErrorEvents.Count > 0 ||
                    PerformanceEvents.Count > 0);
        }
    }
}