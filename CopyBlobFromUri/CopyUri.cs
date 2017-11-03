using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CopyBlobFromUri
{
    public static class CopyUri
    {
        [FunctionName("CopyUri")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req,
            TraceWriter log)
        {
            log.Info("CopyUri function triggered");

            // Check the request for the appropriate payload and return a bad
            // request response if it's incomplete.
            var request = await req.Content.ReadAsAsync<CopyUriRequest>();
            if (request == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Missing request details.");
            }

            // Retrieve the connection string for the storage account and initialize the
            // container and blob to write to. 
            var connectionString = Environment.GetEnvironmentVariable("StorageConnectionString");
            var account = CloudStorageAccount.Parse(connectionString);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(request.ContainerName);
            container.CreateIfNotExistsAsync().Wait();
            var blob = container.GetBlockBlobReference(request.BlobName);

            // Download the Uri into a byte array and then upload it as a 
            // stream into the blob. There are other ways to accomplish
            // this - this is a brute-force way. 
            byte[] data = new System.Net.WebClient().DownloadData(request.Uri);
            using (var stream = new MemoryStream(data, writable: false))
            {
                blob.UploadFromStream(stream);
            }

            // Celebrate
            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
