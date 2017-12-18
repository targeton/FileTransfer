using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Utils
{
    public class ProducerConsumerLite<T>
    {
        #region 变量
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private Task _consumerTask = null;
        #endregion

        #region 方法
        public void Add(T item)
        {
            _queue.Enqueue(item);
            if (_consumerTask == null || _consumerTask.IsCompleted == true)
            {
                while (_queue.Count > 0)
                {
                    T consumeItem = default(T);
                    if (!_queue.TryDequeue(out consumeItem))
                        continue;
                    if (consumeItem == null)
                        continue;
                    Consume(consumeItem);
                }
                BeforeTaskEnd();
            }
        }

        protected virtual void Consume(T item)
        { }

        protected virtual void BeforeTaskEnd()
        { }
        #endregion
    }

}
