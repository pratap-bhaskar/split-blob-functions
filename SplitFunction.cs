using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VSMCFunctions.Models;

namespace VSMCFunctions
{
    public class SplitFunction
    {
        [FunctionName("SplitFunction")]
        public async Task Run([BlobTrigger("project44ftp/pro44/{name}", Connection = "FTPStorage")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            //deserialize the stream to VesselSchedules
            using (StreamReader reader = new StreamReader(myBlob))
            {
                var vesselSchedules = JsonConvert.DeserializeObject<VesselSchedules>(reader.ReadToEnd());
                log.LogInformation($"Found {vesselSchedules.Schedules.Count()} schedules");
                var blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=test19072022;AccountKey=<REDACTED>;BlobEndpoint=https://test19072022.blob.core.windows.net/;");
                var blobContainerClient = blobServiceClient.GetBlobContainerClient("schedules");

                //Write the schedule to the schedule storage account
                foreach (var schedule in vesselSchedules.Schedules)
                {
                    var fileName = schedule.Name;
                    var blobClient = blobContainerClient.GetBlobClient(fileName);
                    var content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(schedule));
                    using (var ms = new MemoryStream(content))
                    {
                        await blobClient.UploadAsync(ms, overwrite: true);
                    }   
                }
            }
        }
    }
}
