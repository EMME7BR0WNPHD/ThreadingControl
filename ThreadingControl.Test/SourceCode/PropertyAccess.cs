using System.Collections.ObjectModel;
using ThreadingControl;

namespace SourceCode.PropertyAccess
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
            foreach (var i in class2.WorkerThreadCollection)
            {
                //todo:
            }
        }
    }

    internal class Class2
    {
        public Class2(ObservableCollection<int> workerThreadCollection)
        {
            WorkerThreadCollection = workerThreadCollection;
        }

        [ThreadControl(Threads.WorkerThread)]
        public ObservableCollection<int> WorkerThreadCollection { get; }
    }
}
