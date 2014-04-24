using System;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace S3NAntTask
{
    [TaskName("amazon-s3")]
    class S3PutFile_DEP_Task : S3CoreFileTask
    {
        /// <summary>DEPRECATED: Execute the NAnt task</summary>
        /// This task exists ONLY to satisfy compatibilty with older versions of the task and script that rely on it
        protected override void ExecuteTask()
        {

            LogHeader = MakeActionLabel("Upload");

            // Ensure the configured bucket exists
            if (!BucketExists(BucketName))
            {
                //Project.Log(Level.Error, "[ERROR] S3 Bucket '{0}' not found!", BucketName);
                S3CreateBucketTask cb = new S3CreateBucketTask();
                try
                {
                    Project.Log(Level.Info, "{0} Creating S3 bucket: {1}", LogHeader, BucketName);
                    using (Client)
                    {
                        try
                        {
                            var request = new PutBucketRequest
                            {
                                BucketName = BucketName,
                                BucketRegion = _region
                            };
                            Client.PutBucket(request);
                        }
                        catch (AmazonS3Exception ex)
                        {
                            ShowError(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Project.Log(Level.Error, "[ERROR] Error creating bucket. Msg: \r\n" + ex);
                }
                return;
            }

            // Ensure the specified file exists
            if (!File.Exists(FilePath))
            {
                Project.Log(Level.Error, "[ERROR] Local file '{0}' doesn't exist!", FilePath);
                return;
            }
            else
            {
                sourceFileMD5 = GetLocalFileMD5Sum(FilePath);
                //Project.Log(Level.Info, "Filename : " + FilePath);
                //Project.Log(Level.Info, "MD5 Sum  : " + FileMD5Sum);
            }

            // Ensure the overwrite is false and the file doesn't already exist in the specified bucket
            if (!Overwrite && S3FileExists(FileName))
                return;

            // Send the file to S3
            using (Client)
            {
                try
                {
                    Project.Log(Level.Info, "{0} File: {1}", LogHeader, FileName);
                    PutObjectRequest request = new PutObjectRequest
                    {
                        Key = FileName,
                        BucketName = BucketName,
                        FilePath = FilePath,
                        Timeout = timeout
                    };

                    PutObjectResponse response = Client.PutObject(request);

                    string targetFileMD5 = response.ETag.Replace("\"", "");

                    //Project.Log(Level.Info, "{0} ETag data: {1}", LogHeader, etag);

                    if (S3FileExists(FileName))
                    {
                        if (sourceFileMD5 == targetFileMD5)
                        {
                            Project.Log(Level.Info, "{0} Upload successful!", LogHeader);
                            //Project.Log(Level.Info, "{0} Expected: {1}", LogHeader, sourceFileMD5);
                            //Project.Log(Level.Info, "{0} Actual  : {1}", LogHeader, targetFileMD5);
                        }
                        else
                        {
                            Project.Log(Level.Error, "{0} Upload corrupted! MD5 Sum mismatch!", LogHeader);
                            //Project.Log(Level.Info, "{0} Expected: {1}", LogHeader, sourceFileMD5);
                            //Project.Log(Level.Info, "{0} Actual  : {1}", LogHeader, targetFileMD5);
                        }
                    }
                    else
                    {
                        Project.Log(Level.Error, "{0} Upload FAILED!", LogHeader);
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
