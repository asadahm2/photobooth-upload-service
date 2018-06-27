using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Logging
{
    public class FileLogger : ILogger
    {
        const string LOG_FILE = "picklesscheduler.txt";

        public void LogCustomEvent(string eventName, Dictionary<string, string> properties = null)
        {
            List<string> lines = new List<string>();
            lines.Add("Event Name: " + eventName);

            if (properties != null)
            {
                lines.AddRange(properties.Select(p => p.Key + ": " + p.Value));
            }

            LogToFile(lines.ToArray());
        }

        public void LogEvent(Dictionary<string, string> properties = null, [CallerMemberName] string eventName = null)
        {
            List<string> lines = new List<string>();
            lines.Add("Event Name: " + eventName);

            lines.AddRange(ToList(properties));

            LogToFile(lines.ToArray());
        }

        public void LogException(Exception ex, Dictionary<string, string> properties = null)
        {
            List<string> lines = new List<string>();
            lines.Add("Exception: " + ex.ToString());

            lines.AddRange(ToList(properties));

            LogToFile(lines.ToArray());
        }

        public void LogTrace(string description, LogLevel level, Dictionary<string, string> properties = null)
        {
            List<string> lines = new List<string>();
            lines.Add("Log Level: " + level.ToString());
            lines.Add("Description: " + description);

            lines.AddRange(ToList(properties));

            LogToFile(lines.ToArray());
        }

        private List<string> ToList(Dictionary<string, string> properties)
        {
            List<string> list = new List<string>();

            if (properties != null)
            {
                list.AddRange(properties.Select(p => p.Key + ": " + p.Value));
            }

            return list;
        }

        private void LogToFile(string[] lines)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, LOG_FILE);

            File.WriteAllLines(filePath, lines);
        }
    }
}
