﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Core.Objects;
    using System.Linq;
    
    public partial class imageuploaderEntities : DbContext
    {
        public imageuploaderEntities()
            : base("name=imageuploaderEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<UploadTracking> UploadTrackings { get; set; }
    
        public virtual ObjectResult<GetUploadedFiles_Result> GetUploadedFiles(string sourceMachineName, string filePath)
        {
            var sourceMachineNameParameter = sourceMachineName != null ?
                new ObjectParameter("sourceMachineName", sourceMachineName) :
                new ObjectParameter("sourceMachineName", typeof(string));
    
            var filePathParameter = filePath != null ?
                new ObjectParameter("filePath", filePath) :
                new ObjectParameter("filePath", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<GetUploadedFiles_Result>("GetUploadedFiles", sourceMachineNameParameter, filePathParameter);
        }
    }
}
