using System;

namespace CfdiSAT.Events
{
    public class HttpRequestFinishedEventArgs : EventArgs
    {
        public HttpRequestFinishedEventArgs(long duration)
        {
            Duration = duration;
        }

        public long Duration { get; }
    }
}
