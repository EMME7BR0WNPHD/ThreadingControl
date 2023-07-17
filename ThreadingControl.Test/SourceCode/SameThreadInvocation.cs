using ThreadingControl;

namespace SourceCode.SameThreadInvocation
{
    internal static class Threads
    {
        public const string MainThread = nameof(MainThread);
        public const string WorkerThread = nameof(WorkerThread);
    }

    internal class Class1
    {
        [ThreadControl(Threads.WorkerThread)]
        public void WorkerThreadMethod(Class2 class2)
        {
            class2.WorkerThreadMethod();
        }
    }

    internal class Class2
    {
        [ThreadControl(Threads.WorkerThread)]
        public void WorkerThreadMethod()
        {

        }
    }
}
