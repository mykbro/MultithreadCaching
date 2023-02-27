using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultithreadCaching
{
	public interface ICachedRepository<TValue, TResult> where TValue : notnull
	{
		public TResult GetResult(TValue value);
		public Task<TResult> GetResultAsync(TValue value);

	}
}
