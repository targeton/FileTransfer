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
        protected int _bufferSize = 0;
        #endregion

        #region 属性

        #endregion

        #region 方法
        public void Add(T item)
        {
            _queue.Enqueue(item);
            if (_consumerTask == null || _consumerTask.IsCompleted == true)
            {
                _consumerTask = Task.Factory.StartNew(() =>
                {
                    List<T> consumeQueue = new List<T>();
                    while (_queue.Count > 0)
                    {
                        T consumeItem = default(T);
                        if (!_queue.TryDequeue(out consumeItem))
                            continue;
                        if (consumeItem == null)
                            continue;
                        consumeQueue.Add(consumeItem);
                        if (consumeQueue.Count >= _bufferSize || _queue.Count <= 0)
                        {
                            Consume(consumeQueue);
                            consumeQueue = new List<T>();
                        }
                        
                    }
                });
            }
        }

        protected virtual void Consume(IEnumerable<T> items)
        { }

        #endregion
    }

}
