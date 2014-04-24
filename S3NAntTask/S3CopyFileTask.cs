using System;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace S3NAntTask
{
    [TaskName("amazon-s3-copyFile")]
    class S3CopyFileTask : S3CoreTask
    {
        #region Task Attributes

        [TaskAttribute("sourceFile", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string sourceKey { get; set; }

        [TaskAttribute("targetFile", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string targetKey { get; set; }

        [TaskAttribute("targetBucket", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public string TargetBucket { get; set; }

        [TaskAttribute("overwrite", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public bool Overwrite { get; set; }

        #endregion

        public string GetTargetBucket
        {
            get
            {
                if (string.IsNullOrEmpty(TargetBucket))
                    return BucketName;
                else
                    return TargetBucket;
            }
        }

        /// <summary>Execute the NAnt task</summary>
        protected override void ExecuteTask()
        {
            LogHeader = MakeActionLabel("Copy");

            // Ensure the configured bucket exists
            if (!BucketExists(BucketName))
            {
                Project.Log(Level.Error, "{0} ERROR! S3 Bucket '{1}' not found!", LogHeader, BucketName);
                return;
            }

            if (!BucketExists(GetTargetBucket))
            {
                Project.Log(Level.Error, "{0} ERROR! S3 Bucket '{1}' not found!", LogHeader, GetTargetBucket);
                return;
            }

            try
            {
                Project.Log(Level.Info,  
                    LogHeader + " From: " + BucketName + ": " + sourceKey + "\r\n" +
                    LogHeader + " to:   " + GetTargetBucket + ": " + targetKey);

                CopyObjectRequest request = new CopyObjectRequest
                {
                    SourceBucket = BucketName,
                    SourceKey = sourceKey,
                    DestinationBucket = GetTargetBucket,
                    DestinationKey = targetKey
                };
                CopyObjectResponse response = Client.CopyObject(request);

                targetFileMD5 = response.ETag.Replace("\"", "");
                sourceFileMD5 = GetS3FileMD5Sum(sourceKey);

                if (!S3FileExists(targetKey))
                {
                    Project.Log(Level.Error, "{0} ERROR! Copy FAILED!", LogHeader);
                }
                else
                {
                    if (sourceFileMD5 == targetFileMD5)
                    {
                        Project.Log(Level.Info, "{0} Copy successful!", LogHeader);
                    }
                    else
                    {
                        Project.Log(Level.Error, "{0} Copy corrupted! MD5 Sum mismatch!", LogHeader);
                        Project.Log(Level.Info, "{0} Expected: {1}", LogHeader, sourceFileMD5);
                        Project.Log(Level.Info, "{0} Actual  : {1}", LogHeader, targetFileMD5);
                    }
                
                }
            }
            catch (AmazonS3Exception ex)
            {
                Project.Log(Level.Error, "{0} ERROR! {1}: {2} \r\n" + LogHeader + "{3}", LogHeader, ex.StatusCode, ex.Message, ex.InnerException);
                return;
            }
        
        }

    }
}
