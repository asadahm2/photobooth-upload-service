using Microsoft.HockeyApp;
using Microsoft.HockeyApp.DataContracts;
using Pickles.Infrastructure.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Pickles.Infrastructure.Logging
{
    public class HockeyAppLogger : ILogger
    {
        public HockeyAppLogger()
        {
            TelemetryConfiguration telemetryConfig = new TelemetryConfiguration();
        }
        public void LogEvent(Dictionary<string, string> properties = null, [CallerMemberName] string eventName = null)
        {
            throw new NotImplementedException();
        }

        public void LogCustomEvent(string eventName, Dictionary<string, string> properties = null)
        {
            HockeyApp.ConsoleCrashHandler c = new HockeyApp.ConsoleCrashHandler();
            
            HockeyClient.Current.TrackEvent(GetEvent(eventName));
            HockeyClient.Current.Flush();
        }
        
        public void LogTrace(string description, LogLevel level, Dictionary<string, string> properties = null)
        {
            HockeyClient.Current.TrackTrace(description, (SeverityLevel)Enum.Parse(typeof(SeverityLevel), level.ToString(), true));
        }

        public void LogException(Exception ex, Dictionary<string, string> properties = null)
        {
            HockeyClient.Current.TrackException(ex, CurrentEnvironment.EnvironmentInfo);
        }

        private EventTelemetry GetEvent(string eventName)
        {
            var eventData = new EventTelemetry(eventName);

            eventData.Context.InstrumentationKey = "871866e909244295827da3736a99620b"; //Get from config

            foreach(var kvp in CurrentEnvironment.EnvironmentInfo)
            {
                eventData.Properties.Add(kvp.Key, kvp.Value);
            }
            
            return eventData;
        }

        
    }
}
