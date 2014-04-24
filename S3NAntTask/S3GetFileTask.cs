using System;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace S3NAntTask
{
    [TaskName("amazon-s3-getFile")]
    public class S3GetFileTask : S3CoreFileTask
    {
        #region Task Attributes

        private string outputfile;

        [TaskAttribute("outputfile", Required = false)]
        [StringValidator(AllowEmpty = true)]
        public string Outputfile 
        {
            get
            {
                return outputfile;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                    outputfile = Path.GetFileName(Key);
                else
                    outputfile = value;
            }
        
        }

        #endregion

        protected override void ExecuteTask() 
        {

            LogHeader = MakeActionLabel("Download");

            // Ensure the configured bucket exists
            if (!BucketExists(BucketName))
            {
                Project.Log(Level.Error, "{0} ERROR! S3 Bucket: {1}, not found!", LogHeader, BucketName);
                return;
            }

            // Ensure the file exists
            if (!S3FileExists(FilePath))
            {
                Project.Log(Level.Error, "{0} ERROR! File not found: {1}", LogHeader, FilePath);
                return;
            }
            else 
            {
                sourceFileMD5 = GetS3FileMD5Sum(FilePath);
            }

            // Get the file from S3
            using (Client)
            {
                try
                {
                    Project.Log(Level.Info, LogHeader + " File: {1}\r\n"  + LogHeader + "   as: {2}", LogHeader, FilePath, Outputfile);
                    GetObjectRequest request = new GetObjectRequest
                    {
                        Key = FilePath,
                        BucketName = BucketName,
                        Timeout = timeout
                    };

                    using (var response = Client.GetObject(request))
                    {
                        response.WriteResponseStreamToFile(Outputfile);

                        targetFileMD5 = response.ETag.Replace("\"", "");

                        // verify that the file actually downloaded
                        if (File.Exists(Outputfile))
                        {
                            if (sourceFileMD5 == targetFileMD5)
                            {
                                Project.Log(Level.Info, "{0} Download successful.", LogHeader, Outputfile);
                            }
                            else
                            {
                                Project.Log(Level.Error, "{0} ERROR! File download FAILED!", LogHeader);
                            }
                        }
                        else
                        {
                            Project.Log(Level.Info, "{0} ERROR! Download FAILED!", LogHeader, Outputfile);
                        }
                            
                    }

                }
                catch (AmazonS3Exception ex)
                {
                    ShowError(ex);
                }
            }
            
            
        }
    }
}
