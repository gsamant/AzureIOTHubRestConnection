﻿using System;
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
        string IOTHubName = "Your IOT Hub Name";
        string IOTHubKey = "Your IOT Hub Key";

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


        [HttpGet("{deviceId}")]
        public DeviceData Get(String deviceId)
        {
            TimeSpan fromEpochStart = DateTime.UtcNow - new DateTime(1970, 1, 1);
            string expiry = Convert.ToString((int)fromEpochStart.TotalSeconds + 3600);

            string baseAddress = (IOTHubName +".azure-devices.net/devices/" + deviceId).ToLower();
            string stringToSign = WebUtility.UrlEncode(baseAddress).ToLower() + "\n" + expiry;

            byte[] data = Convert.FromBase64String(IOTHubKey);
            HMACSHA256 hmac = new HMACSHA256(data);
            string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            string token = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}",
                            WebUtility.UrlEncode(baseAddress).ToLower(), WebUtility.UrlEncode(signature), expiry);
            return new DeviceData { DeviceID = deviceId, Auth = WebUtility.UrlEncode(signature), Expires = expiry };

        }
    }
}
