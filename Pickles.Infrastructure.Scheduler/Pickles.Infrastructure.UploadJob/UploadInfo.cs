using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.UploadJob
{   
    public class UploadInfo
    {
        public string Id { get; set; }

        public string FileName { get; set; }

        public string UploadEndPoint { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public DateTime EndTimeUtc { get; set; }

        public DateTime FileCreationTimeUtc { get; set; }

        public bool IsRetake { get; set; }

        public UploadSet.UploadStatus Status { get; set; }

        public string ErrorMessage { get; set; }

        public string SourceMachineName { get; set; }

        public string ToPropertyString()
        {
            var props = JsonConvert.SerializeObject(ToProperties());

            return props;
        }

        public Dictionary<string, string> ToProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties.Add("Id", Id);
            properties.Add("FileName", FileName);
            properties.Add("UploadEndPoint", UploadEndPoint);
            properties.Add("StartTimeUtc", StartTimeUtc.ToString());
            properties.Add("EndTimeUtc", EndTimeUtc.ToString());
            properties.Add("FileCreationTimeUtc", FileCreationTimeUtc.ToString());
            properties.Add("Status", Status.ToString());
            properties.Add("IsRetake", IsRetake.ToString());
            properties.Add("SourceMachineName", SourceMachineName);
            properties.Add("ErrorMessage", ErrorMessage);

            return properties;
        }
    }
}
