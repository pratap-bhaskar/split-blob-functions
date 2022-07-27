using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VSMCFunctions.Models;

namespace VSMCFunctions
{
    public class ScheduleFunction
    {
        [FunctionName("ScheduleFunction")]
        public async Task Run([BlobTrigger("schedules/{name}", Connection = "ScheduleStorage")] Stream myBlob,
            string name,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            using (StreamReader reader = new StreamReader(myBlob))
            {
                var currentSchedule = JsonConvert.DeserializeObject<VesselSchedule>(reader.ReadToEnd());
                log.LogInformation($"Current value is {currentSchedule}");
                //Check if there is a previous version
                var blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=test19072022;AccountKey=<REDACTED>;BlobEndpoint=https://test19072022.blob.core.windows.net/;");
                var blobContainerClient = blobServiceClient.GetBlobContainerClient("schedules");

                var previousVersion = await GetPreviousVersionContent(blobContainerClient, name, log);
                if(!string.IsNullOrEmpty(previousVersion))
                {
                    var previousSchedule = JsonConvert.DeserializeObject<VesselSchedule>(previousVersion);
                    if(previousSchedule == currentSchedule)
                    {
                        log.LogInformation("No difference found between previous and current versions");
                    }
                    else
                    {
                        log.LogInformation($"Difference found!!!! Previous value is {previousSchedule} and current value is {currentSchedule}");
                    }
                    
                }
            }
        }

        private async Task<string?> GetPreviousVersionContent(BlobContainerClient containerClient, string blobName,
            ILogger log)
        {
            // Call the listing operation, specifying that blob versions are returned.
            // Use the blob name as the prefix. 
            var blobVersions = containerClient.GetBlobs
                (BlobTraits.None, BlobStates.Version, prefix: blobName)
                .Where(x => x.Name == blobName)
                .OrderByDescending(version => version.VersionId);
            
            if(blobVersions.Count() > 1)
            {
                var previousVersion = blobVersions.Skip(1).FirstOrDefault();
                log.LogInformation($"Previous version found {previousVersion.VersionId}");
                //last on would be index 1
                var blobClient = containerClient.GetBlobClient(blobName).WithVersion(previousVersion.VersionId);
                var ms = new MemoryStream();
                await blobClient.DownloadToAsync(ms);
                
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            log.LogInformation($"Previous version not found");
            return null;
        }
    }
}
