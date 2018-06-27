using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Data
{
    public class SqlDB : IData
    {
        public List<T> Get<T>(string tableName, string queryColumn = null, string columnMatchValue = null)
        {
            throw new NotImplementedException();
        }

        public void Upsert<T>(T data, string tableName, string indexColumnName)
        {
            throw new NotImplementedException();
        }

        public void Update(List<UploadTracking> infoList)
        {
            using (var dbContext = new imageuploaderEntities())
            {
                foreach(var info in infoList)
                {
                    var existing = dbContext.UploadTrackings.Where(i => i.Id == info.Id).FirstOrDefault();

                    if (existing == null)
                    {
                        dbContext.UploadTrackings.Add(new UploadTracking
                        {
                            Id = info.Id,
                            EndTimeUtc = info.EndTimeUtc,
                            ErrorMessage = info.ErrorMessage,
                            FileName = info.FileName,
                            MachineName = info.MachineName,
                            SourceMachineName = info.SourceMachineName,
                            StartTimeUtc = info.StartTimeUtc,
                            Status = info.Status,
                            UploadEndPoint = info.UploadEndPoint,
                            FileCreationTimeUtc = info.FileCreationTimeUtc,
                            IsRetake = info.IsRetake
                        });
                    }
                    else
                    {
                        existing.EndTimeUtc = info.EndTimeUtc;
                        existing.ErrorMessage = info.ErrorMessage;
                        existing.FileName = info.FileName;
                        existing.SourceMachineName = info.SourceMachineName;
                        existing.MachineName = info.MachineName;
                        existing.StartTimeUtc = info.StartTimeUtc;
                        existing.Status = info.Status;
                        existing.UploadEndPoint = info.UploadEndPoint;
                        existing.FileCreationTimeUtc = info.FileCreationTimeUtc;
                        existing.IsRetake = info.IsRetake;
                    }
                }

                dbContext.SaveChanges();
            }
        }

        public List<UploadTracking> GetUploadedFiles(string machineName, string filePath)
        {
            List<UploadTracking> data = new List<UploadTracking>();

            using (var dbContext = new imageuploaderEntities())
            {
                data = dbContext.GetUploadedFiles(machineName, filePath).Select
                    (
                        d => new UploadTracking
                        {
                         EndTimeUtc = d.EndTimeUtc,
                         ErrorMessage = d.ErrorMessage,
                         FileName = d.FileName,
                         Id = d.Id,
                         MachineName = d.MachineName,
                         SourceMachineName = d.SourceMachineName,
                         StartTimeUtc = d.StartTimeUtc,
                         Status = d.Status,
                         UploadEndPoint = d.UploadEndPoint,
                         FileCreationTimeUtc = d.FileCreationTimeUtc
                        }
                    ).ToList();
            }

            return data;
        }
    }
}
