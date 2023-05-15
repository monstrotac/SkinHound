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
        private const string USD_TO_EUR_PATH = $"https://www.freeforexapi.com/api/live?pairs=USDEUR";
        private static HttpClient client = new HttpClient();
        public static float usdToCnyRate;
        public static float usdToCadRate;
        public static float usdToEurRate;
        public float USDToCHYRate { get; set; }
        public float USDToCADRate { get; set; }
        public float USDToEURRate { get; set; }
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
            string conversionUsdCad;
            HttpResponseMessage responseCad = await client.GetAsync(USD_TO_CAD_PATH);
            if (responseCad.IsSuccessStatusCode)
            {
                conversionUsdCad = await responseCad.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(conversionUsdCad);
                usdToCadRate = (float)obj["rates"]["USDCAD"]["rate"];
            }
            string conversionUsdCny;
            HttpResponseMessage responseCny = await client.GetAsync(USD_TO_CNY_PATH);
            if (responseCny.IsSuccessStatusCode)
            {
                conversionUsdCny = await responseCny.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(conversionUsdCny);
                usdToCnyRate = (float)obj["rates"]["USDCNY"]["rate"];
            }
            string conversionUsdEur;
            HttpResponseMessage responseEur = await client.GetAsync(USD_TO_EUR_PATH);
            if (responseEur.IsSuccessStatusCode)
            {
                conversionUsdEur = await responseEur.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(conversionUsdEur);
                usdToEurRate = (float)obj["rates"]["USDEUR"]["rate"];
            }
        }
        public static float GetCurrencyRateFromUSD(string currency)
        {
            switch (currency)
            {
                case "CAD":
                    return usdToCadRate;
                    break;
                case "EUR":
                    return usdToEurRate;
                    break;
                case "USD":
                    return 1.0f;
                    break;
                default:
                    return 0.0f;
                    break;
            }
        }
    }
}
