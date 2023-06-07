# MultithreadCaching

## Disclaimer
This codebase is made for self-teaching and educational purposes only.
Many features like input validation, object disposed checks, some exception handling, etc... are mostly missing.
As such this codebase cannot be considered production ready.


## What's this ?
This console app provides a series of cached repositories and a driving Program to test them.

A 'cached repository' is an object from which we can ask a value associated with a key. This value can be viewed as the result of a function where the argument is the key.
After performing the calculation the repository will cache the result for subsequent requests for the same key. This is expecially useful for calculations that requires a great amount of time/memory.

In multithreaded scenarios we should also care to not lock our repositories for too long (the whole duration of the calculation) but also to not repeat a calculation needlessy.
A calculation could be repeated if we use a 'double lock' strategy where we first lock to check if the result is already cached and if not we release the lock, make the calculation and then lock again to cache the result.

As the first lock is not held two threads can both not find the cached result and proceed to make the calculation twice.
In order to prevent this issue and not hold the lock for the whole calculation there are 2 solutions:

1. #### Use a placeholder strategy		
	Where we immediately place a known value like 'null' associated to a key in order to inform any other thread that a calculation is ongoing. A thread will wait for a result through the usage of a Monitor.

2. #### Use a Task based strategy
	Where we don't associate a value TResult to a key of type TValue but a Task<TResult>. 
Any requester will then call myTask.Result (or await it), this will also take care of putting the thread in the wait status. 
This strategy is the one chosen in an example in "Java concurreny in practice".			
	
The 'master' branch uses the Task based strategy while the first strategy is implemented (for the basic repo only) in the 'placeholder' branch.


## How does it work ?
Each repository implements the ICachedRepository interface.
There are 3 repositories:

1. #### Unbounded
	A basic repository that will keep growing indefinetely

2. #### Oldest entry evicted first
	A repository with a maximum size that will delete entries starting from the oldest one

3. #### Least recently used entry evicted first
	A repository with a maximum size that will delete entries starting from the least recently used first

For more details on each repository implementation check the class's source code.


## How should I use this ?
In the Program class choose and comment out the rest between:

	static ICachedRepository<int, int> _myRep = new UnboundedCachedRepository<int, int>(BigCalculation);
	static ICachedRepository<int, int> _myRep = new LRUEvictedCachedRepository<int, int>(BigCalculation, 5);
	static ICachedRepository<int, int> _myRep = new OldestEvictedCachedRepository<int, int>(BigCalculation, 5);

Then in Main(...) choose and comment out the rest between:

	static void Main(string[] args)
    {
        TestMultithreadRepo();
        TestLRUCache();
	}

The first method is a multithreaded simulation while the second one is interactive. These should be used in conjunction with the debugger to inspect the status of the repos during execution. 




