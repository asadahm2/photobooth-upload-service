//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Pickles.Infrastructure.Data
{
    using System;
    
    public partial class GetUploadedFiles_Result
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string UploadEndPoint { get; set; }
        public System.DateTime StartTimeUtc { get; set; }
        public System.DateTime EndTimeUtc { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string MachineName { get; set; }
        public string SourceMachineName { get; set; }
        public System.DateTime FileCreationTimeUtc { get; set; }
    }
}