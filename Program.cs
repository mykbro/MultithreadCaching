using System.Security.Cryptography;

namespace MultithreadCaching
{
    internal class Program
    {
        static int _count = 0;
        static CachedRepository<int, int> _myRep = new CachedRepository<int, int>(BigCalculation);
        static ManualResetEventSlim _canProceed = new ManualResetEventSlim();        

        static void Main(string[] args)
        {
            
            List<Thread> threads = new List<Thread>();


            for(int i = 0; i < 100; i++)
            {
                int toPass = RandomNumberGenerator.GetInt32(10);
                Thread t = new Thread(() => GetAndPrint(toPass));
                threads.Add(t);
                t.Start();
            }

            _canProceed.Set();

            foreach(Thread t in threads)
            {
                t.Join();
            }

            Console.WriteLine("Count: " + _count);
        }

        static int BigCalculation(int x)
        {
            /*
             * we simulate a big calculation updating a static counter to keep track of how many time we do this
             * 
             */

            Interlocked.Increment(ref _count);

            Thread.Sleep(2000);

            return x + 1;
        }

        static void GetAndPrint(int v)
        {
            //we wait 


            _canProceed.Wait();

            Thread.Sleep(RandomNumberGenerator.GetInt32(5000));
            int result = _myRep.GetResult(v);           
            Console.WriteLine(result);
        }

    }

    internal class CachedRepository<Value, Result>
    {
        private readonly Dictionary<Value, Task<Result>> _results;    
        private readonly Func<Value, Result> _calcFunctor;
        private readonly Object _globaLock;      

        public CachedRepository(Func<Value, Result> f) 
        {            
            _results = new Dictionary<Value, Task<Result>>();         
            _calcFunctor = f;
            _globaLock = new Object();            
        }

        public Result GetResult(Value v) 
        {
            Task<Result>? foundTask = null;
            bool taskFound = false;               

            lock (_globaLock)
            {                
                taskFound = _results.TryGetValue(v, out foundTask);
                if (!taskFound)       
                {
                    Task<Result> t = new Task<Result>(() => _calcFunctor(v));   //maybe it's better to speculatively create the Task outside the Lock
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
    }
}