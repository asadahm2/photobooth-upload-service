using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Data
{
    public interface IData
    {
        void Update(List<UploadTracking> infoList);

        List<UploadTracking> GetUploadedFiles(string machineName, string filePath);
    }
}
