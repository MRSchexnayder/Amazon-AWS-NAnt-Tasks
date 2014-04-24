using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace S3NAntTask
{
    [TaskName("amazon-s3-deleteFile")]
    public class S3DeleteFileTask : S3CoreFileTask
    {

        /// <summary>Execute the NAnt task</summary>
        protected override void ExecuteTask()
        {
            LogHeader = MakeActionLabel("Delete");

            //// Ensure the configured bucket exists
            if (!BucketExists(BucketName))
            {
                Project.Log(Level.Error, "{0} ERROR! S3 Bucket '{1}' not found!", LogHeader, BucketName);
                return;
            }

            // Ensure the file exists
            if (!S3FileExists(FilePath))
            {
                Project.Log(Level.Error, "{0} ERROR! File not found {1}", LogHeader, FilePath);
                return;
            }
            else
            {
                // Delete the file from S3
                using (Client)
                {
                    try
                    {
                        Project.Log(Level.Info, "{0} File: {1}", LogHeader, FilePath);
                        DeleteObjectRequest request = new DeleteObjectRequest
                        {
                            Key = FilePath,
                            BucketName = BucketName
                        };

                        var response = Client.DeleteObject(request);

                        if (S3FileExists(FilePath))
                            Project.Log(Level.Error, "{0} ERROR! File delete FAILED!", LogHeader);
                        else
                            Project.Log(Level.Info, "{0} File deleted.", LogHeader);

                    }
                    catch (AmazonS3Exception ex)
                    {
                        ShowError(ex);
                    }
                }

            }
        }

    }
}
