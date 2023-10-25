﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Windows.Media.Protection.PlayReady;

namespace SkinHound
{
    internal class Utils
    {
        //For documentation on the API used for the exchange rate between currencies use the following link: https://www.exchangerate-api.com/docs/free
        private const string USD_CONVERSION_RATE_PATH = "https://open.er-api.com/v6/latest/USD";
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
            //New Revamped Version of Acquiring Rate due to the old API disappearing off the map.
            string usdRateResponse;
            HttpResponseMessage usdRateRequest = await client.GetAsync(USD_CONVERSION_RATE_PATH);
            if (usdRateRequest.IsSuccessStatusCode)
            {
                usdRateResponse = await usdRateRequest.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(usdRateResponse);
                usdToCadRate = (float)obj["rates"]["CAD"];
                usdToCnyRate = (float)obj["rates"]["CNY"];
                usdToEurRate = (float)obj["rates"]["EUR"];
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
        public static string GetCurrencySymbol(string currency)
        {
            switch (currency)
            {
                case "CAD":
                    return "$";
                    break;
                case "EUR":
                    return "€";
                    break;
                case "USD":
                    return "$";
                    break;
                default:
                    return "$";
                    break;
            }
        }
        public static async Task<BitmapImage> ConvertBinaryToImage(string image)
        {
            byte[] imageBytes = Convert.FromBase64String(image);

            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                // Use the bitmapImage as needed
                return bitmapImage;
            }
        }
        //Allows us to verify if a weapon is desired
        public async static Task<bool> VerifyIfDesired(string itemName, int i = 0)
        {
            if (SkinHoundConfiguration.Desired_Weapons.Count == 0)
                return false;
            if (itemName.ToLower().Contains($"{SkinHoundConfiguration.Desired_Weapons.ElementAt(i).ToLower()}"))
                return true;
            else if (SkinHoundConfiguration.Desired_Weapons.Count - 1 > i)
                return VerifyIfDesired(itemName, i + 1).Result;
            return false;
        }
    }
}
