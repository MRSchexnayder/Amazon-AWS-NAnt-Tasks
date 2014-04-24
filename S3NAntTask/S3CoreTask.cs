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

    public abstract class S3CoreTask : Task
    {
        #region Task Attributes

        /// <summary>Region to create the new bucket in. Default to US standard</summary>
        public S3Region _region = S3Region.US;
        public int timeout = 3600000;

        [TaskAttribute("accesskey", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string AWSAccessKey { get; set; }

        [TaskAttribute("secretkey", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string AWSSecretKey { get; set; }

        [TaskAttribute("bucket", Required = true)]
        [StringValidator(AllowEmpty = false)]
        public string BucketName { get; set; }

        [TaskAttribute("region", Required = false)]
        [StringValidator(AllowEmpty = false)]
        public string Region
        {
            get
            {
                return _region.ToString();
            }
            set
            {
                _region = (S3Region)Enum.Parse(typeof(S3Region), value);
                Project.Log(Level.Info, String.Format("Set Amazon region to: {0}", _region));
            }
        }

        #endregion

        public string LogHeader { get; set; }
        public string sourceFileMD5 { get; set; }
        public string targetFileMD5 { get; set; }

        /// <summary>Get an Amazon S3 client. Be sure to dispose of the client when done</summary>
        public AmazonS3 Client
        {
            get
            {
                return Amazon.AWSClientFactory.CreateAmazonS3Client(AWSAccessKey, AWSSecretKey);
            }
        }

        /// <summary>Determine if the specified bucket alredy exists</summary>
        /// <returns>True if the bucket exists</returns>
        public bool BucketExists(string bucketName)
        {
            using (Client)
            {
                try
                {
                    using (var response = Client.ListBuckets())
                    {
                        if (response.Buckets.Any(bucket => bucket.BucketName.Equals(bucketName)))
                        {
                            return true;
                        }

                    }
                }
                catch (AmazonS3Exception ex)
                {
                    ShowError(ex);
                }
            }
            return false;
        }

        /// <summary>Determine if our file already exists in the specified S3 bucket</summary>
        /// <returns>True if the file already exists in the specified bucket</returns>
        public bool S3FileExists(string fileKey)
        {
            bool retVal = false;

            using (Client)
            {
                try
                {
                    ListObjectsRequest request = new ListObjectsRequest
                    {
                        BucketName = BucketName
                    };

                    using (var response = Client.ListObjects(request))
                    {
                        foreach (var file in response.S3Objects)
                        {
                            //Project.Log(Level.Info, "File: " + file.Key);
                            if (file.Key.Equals(fileKey))
                            {
                                retVal = true;
                            }
                        }
                    }
                }
                catch (AmazonS3Exception ex)
                {
                    ShowError(ex);
                }
            }
            return retVal;
        }

        /// <summary>Get the MD5 sum from a local file (on disk)</summary>
        /// <returns>Returns the string representation of the MD5 sum</returns>
        public String GetLocalFileMD5Sum(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (Stream stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        /// <summary>Get the MD5 sum from file in an S3 bucket</summary>
        /// <returns>Returns the string representation of the MD5 sum</returns>
        public String GetS3FileMD5Sum(string fileKey)
        {
            string etag = "not found";
            using (Client)
            {
                try
                {
                    ListObjectsRequest request = new ListObjectsRequest
                    {
                        BucketName = BucketName
                    };

                    do
                    {
                        using (ListObjectsResponse response = Client.ListObjects(request))
                        {
                            foreach (S3Object entry in response.S3Objects)
                            {
                                if (entry.Key.Equals(fileKey))
                                {
                                    etag = entry.ETag.Replace("\"", "");
                                }
                            }
                            // If response is truncated, set the marker to get the next 
                            // set of keys.
                            if (response.IsTruncated)
                            {
                                request.Marker = response.NextMarker;
                            }
                            else
                            {
                                request = null;
                            }
                        }

                    } while (request != null);
                }

                catch (AmazonS3Exception ex)
                {
                    ShowError(ex);
                }

                return etag;
            }
        }

        /// <summary>Creating formatting for proper log messages in NAnt</summary>
        /// <returns>Returns the string that proceeds any messages from the task</returns>
        public string MakeActionLabel(string Action) 
        {
            return "     [S3 " + Action + "]";
        }
        
        /// <summary>Format and display an exception</summary>
        /// <param name="ex">Exception to display</param>
        public void ShowError(AmazonS3Exception ex)
        {
            if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") || ex.ErrorCode.Equals("InvalidSecurity")))
            {
                Project.Log(Level.Error, "Please check the provided AWS Credentials.");
            }
            else
            {
                Project.Log(Level.Error, "An Error, number {0}, occurred with the message '{1}'",
                    ex.ErrorCode, ex.Message);
            }
        }  
 
    }
}
