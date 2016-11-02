using System.Collections.Concurrent;
using System.Data;
using System.Threading;

namespace Logshark.PluginLib.Persistence
{
    internal abstract class BaseInsertionThread<T> : IInsertionThread<T> where T : new()
    {
        protected ConcurrentQueue<T> persistenceQueue;
        protected Thread insertionThread;

        public IDbConnection DbConnection { get; protected set; }
        public bool IsRunning { get; protected set; }
        public long ItemsPersisted { get; protected set; }

        public virtual int ItemsPendingInsertion
        {
            get { return persistenceQueue.Count; }
        }

        protected BaseInsertionThread()
        {
            persistenceQueue = new ConcurrentQueue<T>();
            IsRunning = true;
            insertionThread = new Thread(ProcessQueue);
            insertionThread.Start();
        }

        public void Shutdown()
        {
            IsRunning = false;
            insertionThread.Join();
            if (DbConnection != null && DbConnection.State != ConnectionState.Closed)
            {
                DbConnection.Close();
                DbConnection.Dispose();
            }
        }

        public void Enqueue(T item)
        {
            persistenceQueue.Enqueue(item);
        }

        public void ProcessQueue()
        {
            while (IsRunning || !persistenceQueue.IsEmpty)
            {
                T item;
                while (persistenceQueue.TryDequeue(out item))
                {
                    Insert(item);
                }
            }
        }

        public virtual void Destroy()
        {
            IsRunning = false;
            insertionThread.Abort();
            if (DbConnection != null && DbConnection.State != ConnectionState.Closed)
            {
                DbConnection.Close();
                DbConnection.Dispose();
            }
        }

        protected abstract void Insert(T item);
    }
}