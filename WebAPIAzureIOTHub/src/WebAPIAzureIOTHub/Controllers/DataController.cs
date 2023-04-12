using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Data.SqlClient;
using System.Configuration;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using System.Net;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIAzureIOTHub.Controllers
{

    
    [DataContract]
    public class DeviceData
    {
        [DataMember]
        public string DeviceID { get; set; }
        [DataMember]
        public string Auth { get; set; }
        [DataMember]
        public string Expires { get; set; }
    }

    [DataContract]
    public class SensorData
    {
        [DataMember]
        public string HTime { get; set; }
        [DataMember]
        public string Temperature { get; set; }
        [DataMember]
        public string Pressure { get; set; }
        
    }

    [DataContract]
    public class DataModel
    {
        [DataMember]
        public DeviceData DeviceData { get; set; }
        [DataMember]
        public SensorData SensorData { get; set; }
    }


    [Route("api/[controller]")]
    public class DataController : Controller
    {
        string IOTHubName = "myiothub";
        string IOTHubKey = "7IPsTfzLKePSrRNok4rK6NF92FXSPIH9aXTgZsETvyA=";
        string storageconnection ="BlobEndpoint=https://gamtestrg.blob.core.windows.net/;QueueEndpoint=https://gamtestrg.queue.core.windows.net/;FileEndpoint=https://gamtestrg.file.core.windows.net/;TableEndpoint=https://gamtestrg.table.core.windows.net/;SharedAccessSignature=sv=2021-12-02&ss=bfqt&srt=s&sp=rwdlacupiyx&se=2023-04-12T16:52:28Z&st=2023-04-12T08:52:28Z&spr=https&sig=W5m%2FRpnAHxX3%2Bog7TVaW17Rd1QIdc%2FSvWnIxlos3Gl8%3D";

        // POST api/data
        [HttpPost]
        public String Post([FromBody] object request)
        {
            var data = JsonConvert.DeserializeObject<DataModel>(request.ToString());
            if (data != null && data.DeviceData != null && data.SensorData != null)
            {
                data.SensorData.Pressure = Math.Round((Convert.ToDouble(data.SensorData.Pressure) * 0.145), 1).ToString();
                TimeZoneInfo ISTZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

                data.SensorData.HTime = (TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ISTZone)).ToString();
                var client = new HttpClient();
                client.BaseAddress = new Uri("https://"+ IOTHubName + ".azure-devices.net/devices/" + data.DeviceData.DeviceID + "/messages/events?api-version=2016-02-03");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "SharedAccessSignature sr=demo-myiothub.azure-devices.net%2fdevices%2f" + data.DeviceData.DeviceID + "&sig=" + data.DeviceData.Auth + "&se=" + data.DeviceData.Expires);
                HttpResponseMessage response = client.PostAsJsonAsync("", data.SensorData).Result;
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return new HttpResponseMessage(HttpStatusCode.OK).ToString();
                else
                    return response.ToString();
            }
            else return "Empty data";
        }


        [HttpGet()]
        public string Get(string deviceId, string deviceKey, string ttl)
        {
            TimeSpan fromEpochStart = DateTime.UtcNow - new DateTime(1970,1,1,0,0,0, DateTimeKind.Utc);
            
            int timetolive = Convert.ToInt32(ttl) * 24 * 3600;
            string expiry = Convert.ToString((int)fromEpochStart.TotalSeconds + timetolive);

            string baseAddress = IOTHubName + ".azure-devices.net/devices/" + deviceId;
            string stringToSign = WebUtility.UrlEncode(baseAddress) + "\n" + expiry;

            byte[] data = Convert.FromBase64String(WebUtility.UrlDecode(deviceKey));
            HMACSHA256 hmac = new HMACSHA256(data);
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            string token = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}",
                            WebUtility.UrlEncode(baseAddress).ToLower(), WebUtility.UrlEncode(signature), expiry);
            return token;

        }

    }
}
