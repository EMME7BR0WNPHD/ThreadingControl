using ThreadingControl;

namespace SourceCode.ChainedInvocation
{
    internal static class Threads
    {
        public const string MainThread = nameof(MainThread);
        public const string WorkerThread = nameof(WorkerThread);
    }

    internal class Class1
    {
        [ThreadControl(Threads.MainThread)]
        public void MainThreadMethod1(Class2 class2)
        {
            if (true)
            {
                class2.WorkerThreadMethod();
            }
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
