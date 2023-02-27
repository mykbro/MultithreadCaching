using System.Security.Cryptography;

namespace MultithreadCaching
{
    internal class Program
    {
        static int _count = 0;
		//static ICachedRepository<int, int> _myRep = new UnboundedCachedRepository<int, int>(BigCalculation);
		static ICachedRepository<int, int> _myRep = new LRUEvictedCachedRepository<int, int>(BigCalculation, 5);
		//static ICachedRepository<int, int> _myRep = new OldestEvictedCachedRepository<int, int>(BigCalculation, 5);
		static ManualResetEventSlim _canProceed = new ManualResetEventSlim();        

        static void Main(string[] args)
        {

            //TestMultithreadRepo();
            TestLRUCache();


		}

        static void TestMultithreadRepo()
        {
			List<Thread> threads = new List<Thread>();


			for (int i = 0; i < 100; i++)
			{
                int toPass = RandomNumberGenerator.GetInt32(20);
				Thread t = new Thread(() => GetAndPrint(toPass));
				threads.Add(t);
				t.Start();
			}

			_canProceed.Set();

			foreach (Thread t in threads)
			{
				t.Join();
			}

			Console.WriteLine("Count: " + _count);
		}

        static int BigCalculation(int x)
        {
            /*
             * we simulate a big calculation updating a static counter to keep track of how many time we have a cache miss
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

        static void TestLRUCache()
        {           
            bool exit = false;
            int input = 0;


			while (!exit)
            {
                Console.Write("> ");
                string inputString = Console.ReadLine();
                if (inputString.Equals("x", StringComparison.OrdinalIgnoreCase))
                {
                    exit = true;
                }
                else if(Int32.TryParse(inputString, out input))
                {
                    Console.WriteLine(_myRep.GetResult(input));
                }
            }

        }

    }
}