using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadCaching
{
	public class CachedRepository<Value, Result>
	{
		private readonly Dictionary<Value, Result> _results;
		private readonly HashSet<Value> _requestedComputations;
		private readonly Func<Value, Result> _calcFunctor;
		private readonly Object _dictLock;
		private readonly Object _setLock;   //we could have avoided this using a ConcurrentSet

		public CachedRepository(Func<Value, Result> f)
		{
			_results = new Dictionary<Value, Result>();
			_requestedComputations = new HashSet<Value>();
			_calcFunctor = f;
			_dictLock = new Object();
			_setLock = new Object();
		}

		public Result GetResult(Value v)
		{
			Result toReturn = default(Result);
			bool resultFound = false;
			bool done = false;

			//in this lock we either:
			//  a) find a cached result
			//  b) release the lock and wait for a result already requested but not yet cached
			//  c) find that we need to compute the result ourselves (outside the lock) and cache it 
			lock (_dictLock)
			{
				while (!done)       //we want to loop because we're in a Monitor
				{
					resultFound = _results.TryGetValue(v, out toReturn);
					if (!resultFound)       //if result was not found
					{
						bool calcInProgress;

						//we want to check if we're already computing the result in another thread and if not compute ourselves
						//(we check and add atomically to spare one access)
						//we need to use an additional lock because another thread may be working on this Set outside the _dictLock  
						lock (_setLock)
						{
							calcInProgress = !_requestedComputations.Add(v);
						}

						if (calcInProgress)     //if the value was already there we wait for completion else we've already placed
						{
							//on wake up we'll find the result in the Dictionary in the next iteration
							Monitor.Wait(_dictLock);
						}
						else    //we want to exit the loop and the lock to compute the result ourselves... it's up to us !!
						{
							done = true;
						}
					}
					else    //we found the result and we can exit the loop and lock
					{
						done = true;
					}
				}
			}

			if (!resultFound)   //if we're here we need to compute it ourselves
			{
				toReturn = _calcFunctor(v);
				lock (_dictLock)
				{
					_results.Add(v, toReturn);

					//we need to tell the waiting threads that the result is now available.. only problem is that we wake up everybody
					//a more granular locking mechanism may be required
					Monitor.PulseAll(_dictLock);
				}

				lock (_setLock)
				{
					//we do this outside the "main" lock because the value is already cached here
					_requestedComputations.Remove(v);
				}
			}


			return toReturn;

		}

		// this is a naive implementation that can repeat work if pre-empted between locks
		public Result SimpleGetResult(Value v)
		{
			Result toReturn = default(Result);
			bool resultFound = false;

			lock (_dictLock)
			{
				resultFound = _results.TryGetValue(v, out toReturn);
			}

			if (!resultFound)
			{
				toReturn = _calcFunctor(v);
				lock (_dictLock)
				{
					_results.TryAdd(v, toReturn);
				}
			}

			return toReturn;
		}
	}
}
