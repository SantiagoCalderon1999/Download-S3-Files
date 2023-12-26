using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System.Diagnostics;

namespace Download_S3_Files
{
    internal class Program
    {

        private const int PartSize = 16 * 1024 * 1024;
        private const string BucketName = "";
        private const string AccessKey = "";
        private const string SecretKey = "";
        private const string AwsRegion = "";
        static void Main(string[] args)
        {
            var fileKey = "";
            DownloadFileAsyncS(fileKey).Wait();
        }

        private static AmazonS3Client CreateS3Client()
        {
            Console.WriteLine("Creating AWS Credentials");


            AmazonS3Client client;

            var credentials = new BasicAWSCredentials(AccessKey, SecretKey);
            client = new AmazonS3Client(credentials, RegionEndpoint.GetBySystemName(AwsRegion));

            return client;
        }

        public static async Task<(MemoryStream, bool)> DownloadFileAsyncP(string key)
        {
            try
            {
                using (var client = CreateS3Client())
                {
                    var fileSize = await GetFileSize(key, client);

                    var downloadPartTasks = new List<Task<byte[]>>();
                    var partCount = (int)Math.Ceiling((double)fileSize / PartSize);
                    for (int i = 0; i < partCount; i++)
                    {
                        var startRange = i * PartSize;
                        var endRange = Math.Min((i + 1) * PartSize - 1, fileSize - 1);
                        var partRequest = new GetObjectRequest
                        {
                            BucketName = BucketName,
                            Key = key,
                            ByteRange = new ByteRange(startRange, endRange)
                        };
                        downloadPartTasks.Add(DownloadPartFileAsync(client, partRequest));
                    }
                    await Task.WhenAll(downloadPartTasks);

                    var stream = ReorderDownloadedParts(downloadPartTasks);
                    Console.WriteLine($"Successfully downloaded file of size {fileSize}");
                    return (stream, true);
                }
            }
            catch (AmazonS3Exception ex)
            {
                return (null, false);
            }
        }

        private static MemoryStream ReorderDownloadedParts(List<Task<byte[]>> downloadPartTasks)
        {
            var stream = new MemoryStream();
            var byteList = downloadPartTasks.Select(task => task.Result).ToList();
            foreach (var downloadPartResult in byteList)
            {
                stream.Write(downloadPartResult, 0, downloadPartResult.Length);
            }

            return stream;
        }

        private static async Task<long> GetFileSize(string key, AmazonS3Client client)
        {
            var getObjectMetadataRequest = new GetObjectMetadataRequest()
            {
                BucketName = BucketName,
                Key = key
            };
            var metadata = await client.GetObjectMetadataAsync(getObjectMetadataRequest);
            var fileSize = metadata.Headers.ContentLength;
            return fileSize;
        }

        public static async Task<byte[]> DownloadPartFileAsync(IAmazonS3 s3Client, GetObjectRequest request)
        {
            using (var response = await s3Client.GetObjectAsync(request))
            using (var memoryStream = new MemoryStream())
            {
                await response.ResponseStream.CopyToAsync(memoryStream);
                var numberOfThreads = Process.GetCurrentProcess().Threads.Count;
                return memoryStream.ToArray();
            }
        }

        public static async Task<(MemoryStream, bool)> DownloadFileAsyncS(string key)
        {
            try
            {
                using (var client = CreateS3Client())
                {
                    var request = new GetObjectRequest
                    {
                        BucketName = BucketName,
                        Key = key
                    };

                    using (var response = await client.GetObjectAsync(request))
                    {
                        var stream = new MemoryStream();
                        await response.ResponseStream.CopyToAsync(stream);
                        response.ResponseStream.Dispose();
                        stream.Seek(0, SeekOrigin.Begin);
                        Console.WriteLine("Successfully downloaded file from S3");
                        return (stream, true);
                    }
                }
            }
            catch (AmazonS3Exception ex)
            {
                return (null, false);
            }
        }
    }
}