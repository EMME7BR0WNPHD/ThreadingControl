using ThreadingControl;

namespace SourceCode.NestedInvocation
{
    internal static class Threads
    {
        public const string MainThread = nameof(MainThread);
        public const string WorkerThread = nameof(WorkerThread);
    }

    internal class Class1
    {
        [ThreadControl(Threads.MainThread)]
        public void MainThreadMethod2(Class2 class2)
        {
            IntermediateMethod(class2);
        }

        private void IntermediateMethod(Class2 class2)
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
