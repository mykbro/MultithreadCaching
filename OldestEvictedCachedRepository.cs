using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadCaching
{
	public class OldestEvictedCachedRepository<TValue, TResult> : ICachedRepository<TValue, TResult> where TValue : notnull
	{
		private readonly Queue<TValue> _keysQueue;			//we also need to keep a queue of the requests for eviction
		private readonly Dictionary<TValue, Task<TResult>> _results;
		private readonly Func<TValue, TResult> _calcFunctor;
		private readonly Object _globaLock;
		private readonly int _maxSize;

		private bool MaxSizeReached => _results.Count >= _maxSize;     // called with no locks, for private use


        /// <summary>
        /// An 'oldest eviction' cache initialized with a function f:TValue -> TResult and a max size. 
		/// After reaching max size the oldest entry is evicted from the cache.
        /// </summary> 
        public OldestEvictedCachedRepository(Func<TValue, TResult> f, int maxSize)		
		{
			if(maxSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxSize));

			_results = new Dictionary<TValue, Task<TResult>>();		
			_keysQueue = new Queue<TValue>();
			_calcFunctor = f;
			_globaLock = new Object();
			_maxSize = maxSize;
		}			

		public Task<TResult> GetResultAsync(TValue v)
		{
			Task<TResult>? foundTask = null;
			bool taskFound = false;

			lock (_globaLock)
			{
				taskFound = _results.TryGetValue(v, out foundTask);
				if (!taskFound)
				{
					Task<TResult> t = new Task<TResult>(() => _calcFunctor(v));   //maybe it's better to speculatively create the Task outside the Lock
					if (MaxSizeReached)
					{
						this.RemoveOldestElement();
					}
					_results.Add(v, t);
					_keysQueue.Enqueue(v);
					foundTask = t;
				}
			}

			if (!taskFound)   //we start the Task outside the lock
			{
				foundTask!.Start();
			}

			return foundTask!;   //we return the Task that should be awaited by the caller
		}

        public TResult GetResult(TValue v)
        {            
            return GetResultAsync(v).Result;    //we block until a Result arrives
        }

        public void Clear()
		{
			lock (_globaLock)
			{
				_results.Clear();
				_keysQueue.Clear();
			}
		}

		private bool RemoveOldestElement()
		{
			TValue? v = default(TValue);
			bool elementFound = _keysQueue.TryDequeue(out v);

			if (elementFound)
			{
				_results.Remove(v!);	
			}

			return elementFound;
		}
	}
}
