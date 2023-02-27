using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadCaching
{
	public class UnboundedCachedRepository<TValue, TResult> : ICachedRepository<TValue, TResult> where TValue : notnull
	{
		private readonly Dictionary<TValue, Task<TResult>> _results;
		private readonly Func<TValue, TResult> _calcFunctor;
		private readonly Object _globaLock;	

		

		public UnboundedCachedRepository(Func<TValue, TResult> f)		
		{
			_results = new Dictionary<TValue, Task<TResult>>();
			_calcFunctor = f;
			_globaLock = new Object();		
		}

	


		public TResult GetResult(TValue v)
		{
			Task<TResult>? foundTask = null;
			bool taskFound = false;

			lock (_globaLock)
			{
				taskFound = _results.TryGetValue(v, out foundTask);
				if (!taskFound)
				{
					Task<TResult> t = new Task<TResult>(() => _calcFunctor(v));   //maybe it's better to speculatively create the Task outside the Lock					
					_results.Add(v, t);					
					foundTask = t;
				}
			}

			if (!taskFound)   //we start the Task outside the lock
			{
				foundTask!.Start();
			}

			return foundTask!.Result;    //we block until a Result arrives
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
					_results.Add(v, t);							
					foundTask = t;
				}
			}

			if (!taskFound)   //we start the Task outside the lock
			{
				foundTask!.Start();
			}

			return foundTask!;   //we return the Task that should be awaited by the caller
		}

		public void Clear()
		{
			lock (_globaLock)
			{
				_results.Clear();
			}
		}
	}
}
