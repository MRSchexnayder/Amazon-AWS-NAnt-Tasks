using System;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace S3NAntTask
{
    [TaskName("amazon-s3-CreateBucket")]
    public class S3CreateBucketTask : S3CoreBucketTask
    {
        protected override void ExecuteTask() 
        {
            LogHeader = MakeActionLabel("Create Bucket");
            if (!BucketExists(BucketName))
            {
                Project.Log(Level.Info, "{0} {1}", LogHeader, BucketName);
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
            else
            {
                Project.Log(Level.Error, "{0} Bucket: {1}, already exists!", LogHeader, BucketName);
            }
        }
 
    }
}
