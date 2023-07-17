using System;

namespace ThreadingControl
{
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Property)]
    public class ThreadControlAttribute : Attribute
    {
        public ThreadControlAttribute(string threadName)
        {
            ThreadName = threadName;
        }

        public string ThreadName { get; }
    }
}
