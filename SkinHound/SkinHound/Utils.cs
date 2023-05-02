using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Media.Protection.PlayReady;

namespace SkinHound
{
    internal class Utils
    {
        private const string USD_TO_CAD_PATH = $"https://www.freeforexapi.com/api/live?pairs=USDCAD";
        private const string USD_TO_CNY_PATH = $"https://www.freeforexapi.com/api/live?pairs=USDCNY";
        private static HttpClient client = new HttpClient();
        public static float usdToCnyRate;
        public static float usdToCadRate;
        public float USDToCHYRate { get; set; }
        public float USDToCADRate { get; set; }
        //Base64 utilities.
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static async void UpdateRates()
        {
            //This is used to handle null values.
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            //The list that we will eventually return.
            string usdToCad;
            HttpResponseMessage responseCad = await client.GetAsync(USD_TO_CAD_PATH);
            if (responseCad.IsSuccessStatusCode)
            {
                usdToCad = await responseCad.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(usdToCad);
                usdToCadRate = (float)obj["rates"]["USDCAD"]["rate"];
            }
            string usdToCny;
            HttpResponseMessage responseCny = await client.GetAsync(USD_TO_CNY_PATH);
            if (responseCny.IsSuccessStatusCode)
            {
                usdToCad = await responseCny.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(usdToCad);
                usdToCnyRate = (float)obj["rates"]["USDCNY"]["rate"];
            }
        }
    }
}
