using System;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace S3NAntTask 
{
    [TaskName("amazon-s3-putFile")]
    public class S3PutFileTask : S3CoreFileTask
    {

        /// <summary>Execute the NAnt task</summary>
        protected override void ExecuteTask() 
        {

            LogHeader = MakeActionLabel("Upload");

            //// Ensure the configured bucket exists
            if (!BucketExists(BucketName))
            {
                Project.Log(Level.Error, "{0} ERROR! S3 Bucket '{1}' not found!", LogHeader, BucketName);
                return;
            }

            // Ensure the specified file exists
            if (!File.Exists(FilePath)) 
            {
                Project.Log(Level.Error, "{0} ERROR! Local file '{1}' doesn't exist!", LogHeader, FilePath);
                return;
            }
            else
            {
                sourceFileMD5 = GetLocalFileMD5Sum(FilePath);
            }

            // Ensure that the file doesn't exist or that if it does, the overwrite flag is set to true
            if (!S3FileExists(Key) || (S3FileExists(Key) && Overwrite))
            {

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

                        targetFileMD5 = response.ETag.Replace("\"", "");

                        if (S3FileExists(FileName))
                        {
                            if (sourceFileMD5 == targetFileMD5)
                            {
                                Project.Log(Level.Info, "{0} Upload successful!", LogHeader);
                            }
                            else
                            {
                                Project.Log(Level.Error, "{0} Upload corrupted! MD5 Sum mismatch!", LogHeader);
                                Project.Log(Level.Info, "{0} Expected: {1}", LogHeader, sourceFileMD5);
                                Project.Log(Level.Info, "{0} Actual  : {1}", LogHeader, targetFileMD5);
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
            else
            {
                Project.Log(Level.Error, "{0} ERROR! Target file exists and overwrite attribute NOT set to TRUE!", LogHeader);
            }

        }

    }
}
