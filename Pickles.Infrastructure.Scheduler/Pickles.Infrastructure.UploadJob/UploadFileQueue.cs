using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.UploadJob
{
    //http://csharpindepth.com/Articles/General/Singleton.aspx#nested-cctor
    internal sealed class UploadFileQueue
    {
        private UploadFileQueue()
        {
        }

        public static UploadFileQueue Instance { get { return Nested.instance; } }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
                instance.Queue = new ConcurrentQueue<UploadSet>();
                instance.InProgress = new ConcurrentBag<string>();
            }

            internal static readonly UploadFileQueue instance = new UploadFileQueue();
        }
                
                
        public ConcurrentQueue<UploadSet> Queue { get; set; }

        public ConcurrentBag<string> InProgress { get; set; }
    }
}
