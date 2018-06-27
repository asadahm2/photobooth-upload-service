using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Logging
{
    public interface ILogger
    {
        void LogEvent(Dictionary<string, string> properties = null, [CallerMemberName] string eventName = null);
        
        void LogCustomEvent(string eventName, Dictionary<string, string> properties = null);

        void LogTrace(string description, LogLevel level, Dictionary<string, string> properties = null);

        void LogException(Exception ex, Dictionary<string, string> properties = null);
    }
}
