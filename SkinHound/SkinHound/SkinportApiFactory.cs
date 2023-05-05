/*
 *  SKINPORTANALYZER.EXE
 *  CREATED BY SIMON-OLIVIER "BOBCALVERY" LACHANCE-GAGNÉ
 *  THIS SOFTWARE WAS CREATED UNDER THE USAGE OF THE MIT LICENSE
 *  QC,CANADA NOVEMBER 2022
*/
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace SkinHound
{
    public class SkinportApiFactory
    {
        //The notification manager takes care of recording the history of what we have already been notified of and sends the notifications to his NotificationCreator
        private static NotificationManager notificationManager = new NotificationManager();
        //Our configuration info which we acquire from config.json
        public static SkinHoundConfiguration userConfiguration;
        //This setting is created publicly to avoid repetition, it is used to avoid null values.
        private static JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        //The following const is a path to make our life easier.
        public const string SKINPORT_MARKET_HISTORY_PATH = "/v1/sales/history?currency=CAD&app_id=730";
        //For safety measures, we store our token info in the environement variables of the system, in order for the software to work for you, you'll have to create variables with the same name with your tokens as their value.
        public const string SKINPORT_TOKEN_CLIENT_ENV_VAR = "skinport_tk_id";
        public const string SKINPORT_TOKEN_SECRET_ENV_VAR = "skinport_tk_secret";
        //Here we declare the client which allows us to make requests to the API.
        private static HttpClient client = new HttpClient();
        //We keep a copy of the current product list in memory for price checking.
        private static List<Product> productListInMemory = new List<Product>();
        private static List<ProductMarketHistory> marketHistoryInMemory;

        //Main
        public SkinportApiFactory(SkinHoundConfiguration skinHoundConfiguration)
        {
            //We start by setting our configuration
            userConfiguration = skinHoundConfiguration;
            // Listen to notification activation
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                //The default link.
                string link = "https://skinport.com/";
                // Obtain the arguments from the notification
                ToastArguments.Parse(toastArgs.Argument).TryGetValue("Link", out link);
                //We attempt to start a browser to item.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    link = link.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
                }
            };
            // Update port # in the following line.
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string authHeader = Utils.Base64Encode($"{Environment.GetEnvironmentVariable(SKINPORT_TOKEN_CLIENT_ENV_VAR)}:{Environment.GetEnvironmentVariable(SKINPORT_TOKEN_SECRET_ENV_VAR)}");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + authHeader);
            client.BaseAddress = new Uri("https://api.skinport.com/v1/");
        }

        public async Task<List<string>> GetItemsNameList()
        {
            List<string> itemsNameList = new List<string>();
            foreach (Product product in productListInMemory)
            {
                itemsNameList.Add(product.Market_Hash_Name);
            }
            return itemsNameList;
        }
        //Async task runner where everything is acquired, this method is called by the front end in order to acquire the data to show.
        public async Task<List<Product>> AcquireProductList()
        {
            try
            {
                //This is used to handle null values.
                JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                //We query for a Global product list.
                List<Product> globalProductList = await GetGlobalProductList("/v1/items?currency=CAD");
                //We sort the list in order to have the most expensive outputs at the end.
                globalProductList.Sort((x, y) => x.Suggested_Price.CompareTo(y.Suggested_Price));
                //We send the product list into the memory var for price checking
                if(productListInMemory.Count == 0)
                    productListInMemory = globalProductList;
                List<Product> filteredList = new List<Product>();
                foreach (Product product in globalProductList)
                {
                    //Here we verify that item in the list isn't null because our function returns null when the item in question is not desired instead of wasting time.
                    Product tempProduct = await FilterProduct(product);
                    if (tempProduct != null)
                        filteredList.Add(tempProduct);
                }
                return filteredList;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        //This function is used to find a certain market hashname in the MarketHistoryInMemory.
        private static async Task<ProductMarketHistory> GetProductMarketHistory(string marketHashName)
        {
            if (marketHistoryInMemory == null)
                return null;
            //This is used to handle null values.
            foreach (var history in marketHistoryInMemory)
            {
                if (marketHashName == history.Market_Hash_Name)
                    return history;
            }
            return null;
        }
        //Updates the content of the products and filters the list with the help of the users settings.
        private static async Task<Product> FilterProduct(Product product)
        {
            //These variables are used to determine what type of notification we will be sending.
            bool VerificationsPassed = false, notificationIsFromDesired = false;
            NotificationType notificationType = NotificationType.REGULAR;
            //Before anything, we check if the current Suggested_Price corresponds to what we're looking for.
            if (product.Suggested_Price <= userConfiguration.Minimum_Worth_Value)
                return null;
            //We force the product to update its percentage off to make sure it is up to date and already calculated.
            product.UpdatePercentageOff();

            //At this point we discover what we're dealing with and if we should share it with the user or not.
            if (product.Percentage_Off == 100 || product.Market_Hash_Name.Contains("Case Hardened") || product.Market_Hash_Name.Contains("Doppler") || product.Market_Hash_Name.Contains("Marble Fade"))
                return null;
            if (marketHistoryInMemory == null)
                await AcquireMarketHistory();
            double recommendedDiscount = 0;
            //Before anything happens, we start by verfying if it's a desired weapon.
            if (VerifyIfDesired(product.Market_Hash_Name) && product.Percentage_Off > userConfiguration.Desired_Weapons_Min_Discount_Threshold)
            {
                ProductMarketHistory productMarketHistory = await GetProductMarketHistory(product.Market_Hash_Name);
                product.productMarketHistory = productMarketHistory;
                recommendedDiscount = await productMarketHistory.GetImmediateResellDiscount(product);
                //Now that it is relevant, we acquire details about the last sales of the products.
                switch (product.Percentage_Off)
                {
                    case var _ when product.Percentage_Off >= userConfiguration.Outstanding_Discount_Threshold:
                        notificationIsFromDesired = true; VerificationsPassed = true;
                        notificationType = NotificationType.INCREDIBLE;
                        product.imagePath = "resources/images/IncredibleDesiredNotification.png";
                        break;
                    case var _ when product.Percentage_Off >= userConfiguration.Great_Discount_Threshold:
                        notificationIsFromDesired = true; VerificationsPassed = true;
                        notificationType = NotificationType.GOLDEN;
                        product.imagePath = "resources/images/GoldenDesiredNotification.png";
                        break;
                    case var _ when product.Percentage_Off >= userConfiguration.Good_Discount_Threshold:
                        notificationIsFromDesired = true; VerificationsPassed = true;
                        notificationType = NotificationType.REGULAR;
                        product.imagePath = "resources/images/RegularDesiredNotification.png";
                        break;
                    default:
                        if (userConfiguration.Notify_On_All_Desired_Weapons)
                        {
                            VerificationsPassed = true;
                            notificationType = NotificationType.DEFAULT;
                            product.imagePath = "resources/images/DefaultDesiredNotification.png";
                        }
                        break;
                }
            }
            else if (product.Percentage_Off >= userConfiguration.Good_Discount_Threshold)
            {
                //Now that it is relevant, we acquire details about the last sales of the products.
                ProductMarketHistory productMarketHistory = await GetProductMarketHistory(product.Market_Hash_Name);
                product.productMarketHistory = productMarketHistory;
                recommendedDiscount = await productMarketHistory.GetImmediateResellDiscount(product);
                switch (product.Percentage_Off)
                {
                    case var _ when product.Percentage_Off >= userConfiguration.Outstanding_Discount_Threshold:
                        VerificationsPassed = true;
                        notificationType = NotificationType.INCREDIBLE;
                        product.imagePath = "resources/images/IncredibleNotification.png";
                        break;
                    case var _ when product.Percentage_Off >= userConfiguration.Great_Discount_Threshold:
                        VerificationsPassed = true;
                        notificationType = NotificationType.GOLDEN;
                        product.imagePath = "resources/images/GoldenNotification.png";
                        break;
                    default:
                        //To receive notifications for all the products listed in the application, including regular deals uncomment the lines bellow.
                        VerificationsPassed = true;
                        notificationType = NotificationType.REGULAR;
                        product.imagePath = "resources/images/RegularNotification.png";
                        break;
                }
            }
            //We finish off by sending a notification and by returning the deal if it was determined good enough.
            if (VerificationsPassed)
            {
                //We assign everything in the product
                product.recommendedDiscount = $"{recommendedDiscount}%";
                product.recommendedResellPrice = $"{((1 - recommendedDiscount / 100) * (double)product.Suggested_Price).ToString("0.00")}$";
                product.profitPercentageOnResellPrice = $"{Math.Round((((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price) / (double)product.Min_Price * 100), 2)}%";
                product.profitMoneyOnResellPrice = $"{((1 - (double)recommendedDiscount / 100) * (double)product.Suggested_Price * GetSkinPortCut(product) - (double)product.Min_Price).ToString("0.00")}$";
                //We notify our notification manager to take care of the notification process.
                if (userConfiguration.Notifications_Enabled)
                    await notificationManager.SendNotification(product, notificationType, notificationIsFromDesired);
                return product;
            }

            //If we get here, the deal wasn't good enough.
            return null;
        }
        //This function takes care of acquiring the data regarding the history of the Market, it should not be used too often since it doesn't need to be.
        private static async Task AcquireMarketHistory()
        {
            //This is used to handle null values.
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            //The list that we will eventually return.
            string marketHistory;
            HttpResponseMessage response = await client.GetAsync($"{SKINPORT_MARKET_HISTORY_PATH}");
            if (response.IsSuccessStatusCode)
            {
                marketHistory = await response.Content.ReadAsStringAsync();
                marketHistoryInMemory = JsonConvert.DeserializeObject<List<ProductMarketHistory>>(marketHistory, settings);
            }
            return;
        }

        //Returns the multiplier needed to know how much skinport will take.
        private static double GetSkinPortCut(Product product)
        {
            if (product.Min_Price > 1000)
                return 0.94;
            else return 0.88;
        }

        //This method is used to verify if the item is desired by looping through the elements inside of the list of desired weapons, if it isn't, it returns false.
        private static bool VerifyIfDesired(string itemName, int i = 0)
        {
            if (userConfiguration.Desired_Weapons.Count == 0)
                return false;
            if (itemName.Contains($"{userConfiguration.Desired_Weapons.ElementAt(i)}"))
                return true;
            else if (userConfiguration.Desired_Weapons.Count - 1 > i)
                return VerifyIfDesired(itemName, i + 1);
            return false;
        }
        //Gets the global product list which has little details to it.
        public static async Task<List<Product>> GetGlobalProductList(string path)
        {
            //The list that we will eventually return.
            List<Product> productList = new List<Product>();
            string products;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                products = await response.Content.ReadAsStringAsync();
                productList = JsonConvert.DeserializeObject<List<Product>>(products, settings);
            }
            return productList;
        }
        //Handler of the good shit takes care of repeating the process every X minutes.
        private static void handleProcess(object state)
        {
            //RunAsync().GetAwaiter().GetResult();
        }

        //This method sets the current config with the help of JSON
        public async void SetConfig(SkinHoundConfiguration skinHoundConfig)
        {
            userConfiguration = skinHoundConfig;
        }

        //This method is used to run manual price checks on items, it is incredibly useful to sell items fast at a decent price.
        /*private async static void RunPriceChecker()
        {
            string skinName = Console.ReadLine();
            if (productListInMemory.Count > 0)
            {
                bool productFound = false;
                foreach (Product product in productListInMemory)
                {
                    productFound = await PriceCheck(product, skinName, productFound);
                }
                if (!productFound)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine($"No skin with the name \"{skinName}\" found.");
                }
            }
            else
            {
                Console.WriteLine("Product list is empty please try again");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("Enter a skin name to price check it: ");
        }*/

        public async Task<Queue<Product>> PriceCheck(string skinName)
        {
            //We create a list of products that we will eventually return
            //this list can be of a maximum size of 15 for reasons of optimization at the moment.
            Queue<Product> foundProducts = new Queue<Product>();
            //We begin looking for the product in the memory.
            foreach(Product productInMemory in productListInMemory)
            {
                if (productInMemory.Market_Hash_Name.Contains(skinName))
                {
                    //We create a new product object and begin assigning values to it, so that it is ready to be used.
                    Product tempProduct = productInMemory;
                    tempProduct.productMarketHistory = await GetProductMarketHistory(productInMemory.Market_Hash_Name);
                    if (tempProduct.productMarketHistory == null)
                        return null;
                    double recommendedDiscount = await tempProduct.productMarketHistory.GetInstantResellDiscount(tempProduct);
                    tempProduct.recommendedDiscount = $"{recommendedDiscount}%";
                    tempProduct.recommendedResellPrice = $"{((1 - recommendedDiscount / 100) * (double)tempProduct.Suggested_Price).ToString("0.00")}$";
                    tempProduct.profitPercentageOnResellPrice = $"{Math.Round((((1 - (double)recommendedDiscount / 100) * (double)tempProduct.Suggested_Price * GetSkinPortCut(tempProduct) - (double)tempProduct.Min_Price) / (double)tempProduct.Min_Price * 100), 2)}%";
                    tempProduct.profitMoneyOnResellPrice = $"{((1 - (double)recommendedDiscount / 100) * (double)tempProduct.Suggested_Price * GetSkinPortCut(tempProduct) - (double)tempProduct.Min_Price).ToString("0.00")}$";
                    //We add the product to our list and we keep itterating. If the list contains 20 items we stop and return it.
                    foundProducts.Enqueue(tempProduct);
                    if (foundProducts.Count == 15)
                        return foundProducts;
                }
            }
            //Now that it is relevant, we acquire details about the last sales of the products.
            return foundProducts;
        }
    }
}
