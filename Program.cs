using System.Security.Cryptography;

namespace MultithreadCaching
{
    internal class Program
    {
        static int _count = 0;
       

        static void Main(string[] args)
        {

			CachedRepository<int, int> myRep = new CachedRepository<int, int>(BigCalculation);
			ManualResetEventSlim rendevouz = new ManualResetEventSlim();
			List<Thread> threads = new List<Thread>();


            for(int i = 0; i < 100; i++)
            {
                int toPass = RandomNumberGenerator.GetInt32(10);
                Thread t = new Thread(() => GetAndPrint(toPass, rendevouz, myRep));
                threads.Add(t);
                t.Start();
            }

            rendevouz.Set();

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

        static void GetAndPrint(int v, ManualResetEventSlim rendevouz, CachedRepository<int, int> myRep)
        {
            //we wait
            rendevouz.Wait();

            Thread.Sleep(RandomNumberGenerator.GetInt32(5000));
            //int result = myRep.GetResult(v);
            int result = myRep.SimpleGetResult(v);
            Console.WriteLine(result);
        }

    }
}