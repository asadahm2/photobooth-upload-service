using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.UploadJob
{
    public class UploadSet
    {
        public UploadSet()
        {
            CanTrack = true;
        }

        public enum UploadStatus { NotStarted, Successful, Failed }

        public string LoadNumber { get; set; }

        public bool IsRetake { get; set; }

        public bool CanTrack { get; set; }

        public SetupConfigurationElement Setup { get; set; }

        public List<UploadInfo> UploadInfoList { get; set; }

        public Dictionary<string, string> ToProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties.Add("LoadNumber", LoadNumber);
            properties.Add("IsRetake", IsRetake.ToString());
            properties.Add("FilePath", Setup.FilePath);
            properties.Add("ImagesPerCall", Setup.ImagesPerCall.ToString());
            properties.Add("SourceMachineName", Setup.MachineName);
            properties.Add("UploadEndpoint", Setup.UploadEndpoint);

            if (UploadInfoList != null)
            {
                foreach (var item in UploadInfoList)
                {
                    properties.Add(item.Id, item.ToPropertyString());
                }
            }
            
            return properties;
        }
    }
}
