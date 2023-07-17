using System;

namespace ThreadingControl
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PipelineAttribute : Attribute
    {
        public PipelineAttribute(string threadName)
        {
            ThreadName = threadName;
        }

        public string ThreadName { get; }
    }
}
