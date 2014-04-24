using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace S3NAntTask
{
    public abstract class S3CoreFileTask : S3CoreTask
    {
        #region Task Attributes

        [TaskAttribute("file", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string FilePath { get; set; }

        [TaskAttribute("key", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string Key { get; set; }

        [TaskAttribute("overwrite", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public bool Overwrite { get; set; }

        /// <summary>Get the name of the file we're sending to S3</summary>
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(Key))
                    return Path.GetFileName(FilePath);
                else
                    return Key;
            }
        }

        #endregion

        protected override void ExecuteTask()
        { 
        
        }

    }
}
