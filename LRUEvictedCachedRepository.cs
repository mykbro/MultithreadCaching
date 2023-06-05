using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadCaching
{
	public class LRUEvictedCachedRepository<TValue, TResult> : ICachedRepository<TValue, TResult> where TValue : notnull
	{
		/*
		 * We need to keep a dictionary of 'Key -> ListNode' in order to update the list when requesting a result for a specific key.
		 * This can be done by using 2 dictionaries (1 for Key -> Task and 1 for Key -> ListNode) or by using only a dictionary Key -> (Task, ListNode).
		 * 
		 * The list nodes will contain the keys, for dictionary cleanup on eviction.	 
		 * 
		 */
		
		
		private readonly LinkedList<TValue> _keyListByMostRecent = new LinkedList<TValue>();	//we need to manage a LinkedList of the entries that we'll continously update
		private readonly Dictionary<TValue, LRUCacheEntry<TValue, TResult>> _results;		//we now use a Dictionary of TValue -> { Task<TResult>, LinkedListNode<TValue> }
		private readonly Func<TValue, TResult> _calcFunctor;
		private readonly Object _globaLock;
		private readonly int _maxSize;

		private bool MaxSizeReached => _results.Count >= _maxSize;     // called with no locks, for private use

        /// <summary>
        /// A 'least recently used eviction' cache initialized with a function f:TValue -> TResult and a max size. 
        /// After reaching max size the least recently used entry is evicted from the cache.
        /// </summary> 
        public LRUEvictedCachedRepository(Func<TValue, TResult> f, int maxSize)		
		{
			if(maxSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxSize));

			_results = new Dictionary<TValue, LRUCacheEntry<TValue, TResult>>();
			_keyListByMostRecent = new LinkedList<TValue>();
			_calcFunctor = f;
			_globaLock = new Object();
			_maxSize = maxSize;
		}		
		
		public Task<TResult> GetResultAsync(TValue v)
		{
			LRUCacheEntry<TValue, TResult> foundCacheEntry = default(LRUCacheEntry<TValue, TResult>);

			Task<TResult>? resultTask = null;
			bool cacheEntryFound = false;

			lock (_globaLock)
			{
				cacheEntryFound = _results.TryGetValue(v, out foundCacheEntry);
				if (!cacheEntryFound)
				{
					Task<TResult> t = new Task<TResult>(() => _calcFunctor(v));   //maybe it's better to speculatively create the Task outside the Lock
					if (MaxSizeReached)
					{
						this.RemoveLeastRecentlyUsedElement();
					}

					LinkedListNode<TValue> addedNode = _keyListByMostRecent.AddFirst(v);    //we put it at the beginning of the list... it's the most recent !

					_results.Add(v, new LRUCacheEntry<TValue, TResult>(t, addedNode));

					resultTask = t;
				}
				else
				{
					// we retrieve the task
					resultTask = foundCacheEntry.ResultTask;

					// we need to update the LRU list !
					LinkedListNode<TValue> node = foundCacheEntry.ListNode;
					
					// we check if the node is not already the first (ref equality)
					if(node != _keyListByMostRecent.First)
					{
						_keyListByMostRecent.Remove(node);  // we remove it from its position (it's O(1), it uses node.Next and .Prev )
						_keyListByMostRecent.AddFirst(node); // we place it back at the beginning	
					}
									
				}
			}

			if (!cacheEntryFound)   //we start the Task outside the lock
			{
				resultTask!.Start();
			}

			return resultTask;    //we block until a Result arrives
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
				_keyListByMostRecent.Clear();
			}
		}

		private void RemoveLeastRecentlyUsedElement()
		{
			LinkedListNode<TValue>? lastNode = _keyListByMostRecent.Last;

			//when calling this method there should always be at least one node
			if(lastNode != null)
			{
				_results.Remove(lastNode.Value);
				_keyListByMostRecent.RemoveLast();
			}			
		}
	}

	internal struct LRUCacheEntry<TValue, TResult>
	{
		public Task<TResult> ResultTask;
		public LinkedListNode<TValue> ListNode;

		public LRUCacheEntry(Task<TResult> resultTask, LinkedListNode<TValue> listNode)
		{
			ResultTask = resultTask; 
			ListNode = listNode;
		}
	}
}
