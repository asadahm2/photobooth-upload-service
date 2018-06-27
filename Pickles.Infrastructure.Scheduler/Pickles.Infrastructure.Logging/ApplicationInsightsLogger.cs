using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Pickles.Infrastructure.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Logging
{
    public class ApplicationInsightsLogger : ILogger
    {
        private readonly TelemetryClient Telemetry = null;

        public ApplicationInsightsLogger()
        {
            Telemetry = new TelemetryClient();

            Telemetry.InstrumentationKey = ConfigurationManager.AppSettings["appInsightsInstrumentationKey"];

            Telemetry.Context.User.Id = String.Format("{0}\\{1}", CurrentEnvironment.MachineName, CurrentEnvironment.UserName);
            Telemetry.Context.Session.Id = Guid.NewGuid().ToString();
            Telemetry.Context.Device.OperatingSystem = CurrentEnvironment.OperatingSystem;
            Telemetry.Context.Location.Ip = CurrentEnvironment.IPAddress;
        }

        public void LogEvent(Dictionary<string, string> properties = null, [CallerMemberName] string eventName = null)
        {
            Telemetry.TrackEvent(eventName, GetProperties(properties));
        }

        public void LogCustomEvent(string eventName, Dictionary<string, string> properties = null)
        {
            Telemetry.TrackEvent(eventName, GetProperties(properties));
        }

        public void LogException(Exception ex, Dictionary<string, string> properties = null)
        {
            Telemetry.TrackException(ex, GetProperties(properties));
        }

        public void LogTrace(string description, LogLevel level, Dictionary<string, string> properties = null)
        {
            Telemetry.TrackTrace(description, (SeverityLevel)Enum.Parse(typeof(SeverityLevel), level.ToString(), true), GetProperties(properties));
        }
        
        private Dictionary<string, string> GetProperties(Dictionary<string, string> properties)
        {
            var props = CurrentEnvironment.EnvironmentInfo;

            if (properties != null)
            {
                foreach(var item in properties)
                {
                    props.Add(item.Key, item.Value);
                }
            }

            return props;
        }
    }
}
