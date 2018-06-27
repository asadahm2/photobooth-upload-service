using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pickles.Infrastructure.Data
{
    public class LiteDB 
    {
        const string DATABASE_NAME = "picklesscheduler.db";
        
        public void Upsert<T>(T data, string tableName, string indexColumnName)
        {
            lock(this)
            {
                // Open database (or create if doesn't exist)
                using (var db = new LiteDatabase(DATABASE_NAME))
                {
                    using (var trans = db.BeginTrans())
                    {
                        // Get customer collection
                        var col = db.GetCollection<T>(tableName);

                        // Create unique index in Name field
                        col.EnsureIndex(indexColumnName, true);

                        // Insert / update
                        col.Upsert(data);

                        trans.Commit();
                    }
                }
            }
            
        }

        public List<T> Get<T>(string tableName, string queryColumn = null, string columnMatchValue = null)
        {   

            lock(this)
            {
                List<T> data = new List<T>();

                using (var db = new LiteDatabase(DATABASE_NAME))
                {
                    Query query = null;
                    var col = db.GetCollection<T>(tableName);

                   // col.Delete(Query.EQ("FileName", @"C:\Temp1\Mazda-61.jpg"));

                    //col.Delete(Query.All());

                    if (!String.IsNullOrEmpty(queryColumn) && !String.IsNullOrEmpty(columnMatchValue))
                    {
                        query = Query.EQ(queryColumn, columnMatchValue);
                    }
                    else
                    {
                        query = Query.All();
                    }

                    data = col.Find(query).ToList<T>();
                }

                return data;
            }
            
        }
        
    }
}
