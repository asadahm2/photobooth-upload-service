using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.UploadJob
{
    public static class Extensions
    {
        public static string ToLoadNumber(this string fileName)
        {
            string loadNumber = String.Empty;

            fileName = fileName.ToFileName();

            loadNumber = fileName.Split("_".ToCharArray())[0];

            return loadNumber;
        }

        public static string ToCameraPosition(this string fileName)
        {
            string position = String.Empty;

            fileName = fileName.ToFileName();

            position = fileName.Split("_".ToCharArray())[1];

            return position;
        }

        public static string ToSequenceNumber(this string fileName)
        {
            string sequence = String.Empty;
            
            fileName = fileName.ToFileName();

            sequence = fileName.Split("_".ToCharArray())[2];

            return sequence;
        }

        public static string ToFileName(this string fileName)
        {
            //e.g. C:\Temp1\181818_Top_01.jpg
            return Path.GetFileName(fileName);
        }

        public static string ToDestinationFolder(this string fileName, string archiveFolder)
        {
            //e.g. fileName: C:\Temp1\181818_Top_01.jpg or \\Apple\Shared\181818_Top_01.jpg
            //archiveFolder: C:\Archive or \\Apple\Archive

            //e.g. C:\Temp1\181818_Top_01.jpg to C:\Archive\05May2017\181818_Top_01.jpg
            //\\Apple\Shared\181818_Top_01.jpg to \\Apple\Archive\181818_Top_01.jpg
            fileName = fileName.ToFileName();

            return Path.Combine(archiveFolder, DateTime.Now.ToString("ddMMMyyyy") ,fileName);
        }

        public static string ToErrorFolder(this string fileName, string archiveFolder)
        {
            //e.g. fileName: C:\Temp1\181818_Top_01.jpg or \\Apple\Shared\181818_Top_01.jpg
            //archiveFolder: C:\Archive or \\Apple\Archive

            //e.g. C:\Temp1\181818_Top_01.jpg to C:\Archive\05May2017\181818_Top_01.jpg
            //\\Apple\Shared\181818_Top_01.jpg to \\Apple\Archive\181818_Top_01.jpg
            fileName = fileName.ToFileName();

            return Path.Combine(archiveFolder, DateTime.Now.ToString("ddMMMyyyy") + "error", fileName);
        }
    }
}
