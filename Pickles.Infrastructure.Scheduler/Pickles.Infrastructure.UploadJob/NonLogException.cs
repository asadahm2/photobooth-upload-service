using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.UploadJob
{
    public class NonLogException : Exception
    {
        public NonLogException(string message) : base(message)
        {

        }
    }
}
