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
    [TaskName("amazon-s3-deleteAllFiles")]
    public class S3DeleteMultipleFilesTask : S3CoreTask
    {
        #region Task Attributes

        [TaskAttribute("searchstring", Required = true)]
        [StringValidator(AllowEmpty = true)]
        public string SearchString { get; set; }

        #endregion

        DeleteObjectsRequest deleteRequest = new DeleteObjectsRequest();
        int numKeys = 0;

        /// <summary>Execute the NAnt task</summary>
        protected override void ExecuteTask()
        {
            LogHeader = MakeActionLabel("Delete");

            // Ensure the configured bucket exists
            if (!BucketExists(BucketName))
            {
                Project.Log(Level.Error, "{0} ERROR! S3 Bucket: {1}, not found!", LogHeader, BucketName);
                return;
            }

            deleteRequest.BucketName = BucketName;

            FindKeys(BucketName, deleteRequest, SearchString, Client, LogHeader);

            // Delete the file from S3
            if (numKeys > 0)
            {
                if (String.IsNullOrEmpty(SearchString))
                    SearchString = "NONE (all files will be deleted!)";

                using (Client)
                {
                    try
                    {
                        DeleteObjectsResponse response = Client.DeleteObjects(deleteRequest);
                        Project.Log(Level.Info, "{0} Successfully deleted {1} files", LogHeader, response.DeletedObjects.Count);

                    }
                    catch (DeleteObjectsException ex)
                    {
                        ShowError(ex);
                    }
                    catch (AmazonS3Exception ex)
                    {
                        ShowError(ex);
                    }
                    catch (Exception ex)
                    {
                        Project.Log(Level.Error, "{0} ERROR: {1}", LogHeader, ex.Message);
                    }
                }
            }
            else
            {
                Project.Log(Level.Info, "{0} Bucket contains no files with the specified search string: {1}", LogHeader, SearchString);
            }

        }

        private void FindKeys(string BucketName, DeleteObjectsRequest deleteRequest, string SearchString, AmazonS3 Client, string LogHeader)
        {
            ListObjectsRequest request = new ListObjectsRequest
            {
                BucketName = BucketName
            };

            using (Client)
            {
                do
                {

                    ListObjectsResponse response = Client.ListObjects(request);
                    foreach (S3Object entry in response.S3Objects)
                    {
                        if (entry.Key.Contains(SearchString))
                        {
                            Project.Log(Level.Info, "{0} Deleting file: {1}", LogHeader, entry.Key);
                            deleteRequest.AddKey(entry.Key, null);
                            numKeys++;
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

                } while (request != null);
            }
        }

    }
}

