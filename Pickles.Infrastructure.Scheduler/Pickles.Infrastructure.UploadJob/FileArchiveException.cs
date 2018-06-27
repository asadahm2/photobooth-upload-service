using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.UploadJob
{
    public class FileArchiveException : Exception
    {
        public FileArchiveException(string message) : base(message)
        {

        }
    }
}
