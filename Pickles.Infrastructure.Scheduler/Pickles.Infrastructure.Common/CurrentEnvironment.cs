using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Pickles.Infrastructure.Common
{
    public class CurrentEnvironment
    {
        public static string IPAddress
        {
            get
            {
                string ipAddress = "";

                string hostName = Dns.GetHostName();

                IPHostEntry ipEntry = Dns.GetHostEntry(hostName);

                var ipNode = ipEntry.AddressList.Where(a => a.AddressFamily.ToString() == "InterNetwork").FirstOrDefault();

                if (ipNode != null)
                {
                    ipAddress = ipNode.ToString();
                }

                return ipAddress;
            }
        }

        public static string MachineName
        {
            get
            {
                return Environment.MachineName;
            }
        }

        public static string UserName
        {
            get
            {
                return Environment.UserName;
            }
        }

        public static string OperatingSystem
        {
            get
            {
                return Environment.OSVersion.ToString();
            }
        }

        public static Dictionary<string, string> EnvironmentInfo
        {
            get
            {
                var info = new Dictionary<string, string>();

                info.Add("User", String.Format("{0}\\{1}", MachineName, UserName));
                info.Add("OS", CurrentEnvironment.OperatingSystem);
                info.Add("MachineName", CurrentEnvironment.MachineName);
                info.Add("IP", CurrentEnvironment.IPAddress);
                info.Add("SysTimeUtc", DateTime.UtcNow.ToString());

                return info;
            }
        }
    }
}
