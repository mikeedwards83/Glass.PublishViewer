using System;

namespace Glass.PublishViewer
{
    public class PublishTerminatedException : Exception
    {
        public PublishTerminatedException(string message) : base(message)
        {

        }
    }
}
