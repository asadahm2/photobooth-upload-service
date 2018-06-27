using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Logging
{
    public class WindowsEventLogger : ILogger
    {
        public void LogCustomEvent(string eventName, Dictionary<string, string> properties = null)
        {
            throw new NotImplementedException();
        }

        public void LogEvent(Dictionary<string, string> properties = null, [CallerMemberName] string eventName = null)
        {
            throw new NotImplementedException();
        }

        public void LogException(Exception ex, Dictionary<string, string> properties = null)
        {
            throw new NotImplementedException();
        }

        public void LogTrace(string description, LogLevel level, Dictionary<string, string> properties = null)
        {
            throw new NotImplementedException();
        }

        private void Log()
        {
            string source = "PicklesScheduler";

            string log = "Application";

            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, log);
            }

            EventLog.WriteEntry(source, "This is a warning from the demo log", EventLogEntryType.Warning);
        }
    }
}
