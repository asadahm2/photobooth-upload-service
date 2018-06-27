using System;
using System.Collections.Generic;
using System.Configuration;

namespace Pickles.Infrastructure.UploadJob
{
    public class UploadFileConfiguration : ConfigurationSection
    {
        [ConfigurationProperty("numberOfThreads", IsRequired = true)]
        public int NumberOfThreads
        {
            get
            {
                return Convert.ToInt32(this["numberOfThreads"]);
            }
        }

        [ConfigurationProperty("uploadSource", IsRequired = true)]
        public string UploadSource
        {
            get
            {
                return Convert.ToString(this["uploadSource"]);
            }
        }

        [ConfigurationProperty("setup", IsRequired = true)]
        public SetupConfigurationCollection Setups
        {
            get
            {
                return this["setup"] as SetupConfigurationCollection;
            }
        }
    }

    public class SetupConfigurationCollection : ConfigurationElementCollection, IEnumerable<SetupConfigurationElement>
    {
        public new IEnumerator<SetupConfigurationElement> GetEnumerator()
        {
            foreach (var key in this.BaseGetAllKeys())
            {
                yield return (SetupConfigurationElement)BaseGet(key);
            }
        }

        public SetupConfigurationElement this [int index]
        {
            get
            {
                return BaseGet(index) as SetupConfigurationElement;
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SetupConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SetupConfigurationElement)element).Id;
        }
    }

    public class SetupConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("id", IsRequired = true)]
        public string Id { get { return this["id"] as string; } }


        [ConfigurationProperty("filePath", IsRequired = true)]
        public string FilePath { get { return this["filePath"] as string; } }


        [ConfigurationProperty("machineName", IsRequired = true)]
        public string MachineName { get { return this["machineName"] as string; } }


        [ConfigurationProperty("userName", IsRequired = false)]
        public string UserName { get { return this["userName"] as string; } }

        
        [ConfigurationProperty("password", IsRequired = false)]
        public string Password { get { return this["password"] as string; } }


        [ConfigurationProperty("fileExtension", IsRequired = true)]
        public string FileExtension { get { return this["fileExtension"] as string; } }


        [ConfigurationProperty("uploadEndpoint", IsRequired = true)]
        public string UploadEndpoint { get { return this["uploadEndpoint"] as string; } }


        [ConfigurationProperty("getEndpoint", IsRequired = true)]
        public string GetEndpoint { get { return this["getEndpoint"] as string; } }


        [ConfigurationProperty("subscriptionId", IsRequired = true)]
        public string SubscriptionId { get { return this["subscriptionId"] as string; } }


        [ConfigurationProperty("uploadTimeoutMinutes", IsRequired = true)]
        public int UploadTimeoutMinutes { get { return Convert.ToInt32(this["uploadTimeoutMinutes"]); } }


        [ConfigurationProperty("imagesPerCall", IsRequired = true)]
        public int ImagesPerCall { get { return Convert.ToInt32(this["imagesPerCall"]); } }


        [ConfigurationProperty("archiveFolder")]
        public string ArchiveFolder { get { return this["archiveFolder"] as string; } }
    }
}
