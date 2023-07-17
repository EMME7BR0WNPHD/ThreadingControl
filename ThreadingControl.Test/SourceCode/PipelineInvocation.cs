using System;
using ThreadingControl;

namespace SourceCode.PipelineInvocation
{
    internal static class Threads
    {
        public const string MainThread = nameof(MainThread);
        public const string WorkerThread = nameof(WorkerThread);
    }

    internal class Class1
    {
        private Pipeline _pipeline;

        public Class1(Pipeline pipeline)
        {
            _pipeline = pipeline;
        }

        [ThreadControl(Threads.MainThread)]
        public void MainThreadMethod1(Class2 class2)
        {
            _pipeline.Enqueue(() => class2.WorkerThreadMethod());
            _pipeline.Enqueue(class2.WorkerThreadMethod);
            IntermediateMethod(class2);
        }

        private void IntermediateMethod(Class2 class2)
        {
            _pipeline.Enqueue(() => class2.WorkerThreadMethod());
            _pipeline.Enqueue(class2.WorkerThreadMethod);
        }
    }

    internal class Pipeline
    {
        [Pipeline(Threads.WorkerThread)]
        public void Enqueue(Action action)
        {
            action();
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
