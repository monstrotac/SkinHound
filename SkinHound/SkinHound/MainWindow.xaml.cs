using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Usb;

namespace SkinHound
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Private fields
        private SkinHoundConfiguration configuration;
        private SkinportApiFactory skinportApiFactory;
        private Buff163ApiFactory buff163ApiFactory;
        private CSGOTradersPricesFactory csgoTradersPriceFactory;
        private bool buffCookieFunctionnal = false;
        private Timer refreshProcess;
        private int timeIntervalBetweenQuerries;
        //lock object for synchronization;
        private static object _syncLock = new object();
        //Suggestion TextBox related fields
        private List<string> skinNamesList = new List<string>();
        private ObservableCollection<string> desiredWeaponsList = new ObservableCollection<string>();
        //Components
        private WrapPanel dealsGrid;
        private Image loadingGif;
        private ScrollViewer dealScroll;
        //The default content for the config file, in case it does not already exist.
        private const string DEFAULT_CONFIG_FILE_CONTENT = "{" +
          "\n\t\"desired_weapons_min_discount_threshold\": 22.0," +
          "\n\t\"good_discount_threshold\": 25.0," +
          "\n\t\"great_discount_threshold\": 30.0," +
          "\n\t\"outstanding_discount_threshold\": 35.0," +
          "\n\t\"minimum_worth_value\": 3.00," +
          "\n\t\"minutes_between_queries\": 2," +
          "\n\t\"desired_weapons\":[" +
          "\n\t]," +
          "\n\t\"notify_on_all_desired_weapons\":true," +
          "\n\t\"notifications_enabled\":true," +
          "\n\t\"buff_cookie\":''" +
          "\n}";

        public MainWindow()
        {
            //The very first step is to acquire the configuration from the file, if it exists.
            GetUserConfigFromFile();
            //Then we proceed to Initialize our APIFactories followed by the components.
            skinportApiFactory = new SkinportApiFactory(configuration);
            buff163ApiFactory = new Buff163ApiFactory(configuration);
            csgoTradersPriceFactory = new CSGOTradersPricesFactory();
            InitializeComponent();
            //We take a quick moment to Init the values of the settings.
            InitSettingsValue();
            //We obtain Buff's Market History here since doing it constantly is not good practice for this one, and is not needed.
            RefreshBuffMarketValue();
            //We update the value of the rates for money conversion later on since everything the Buff163 API returns is in CNY
            Utils.UpdateRates();
            //Initialize the methods linked to components of the application.
            dealsGrid = (WrapPanel)FindName("DealsGrid");
            loadingGif = (Image)FindName("LoadingIcon");
            dealScroll = (ScrollViewer)FindName("DealScrollBar");
            //We start the timer which will automate the deals and refresh them on X configured basis.
            timeIntervalBetweenQuerries = 1000 * 60 * configuration.Minutes_Between_Queries;
            refreshProcess = new Timer(DealsGridHandler, null, 0, timeIntervalBetweenQuerries);

        }
        private void InitSettingsValue()
        {
            //Notification section
            SettingsNotificationsEnabled.IsChecked = configuration.Notifications_Enabled;
            SettingsNotifyOnAllDesiredWeapons.IsChecked = configuration.Notify_On_All_Desired_Weapons;
            //General section
            if(Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_CLIENT_ENV_VAR) != null)
                SettingsSkinportClientId.Password = Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_CLIENT_ENV_VAR);
            if (Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_SECRET_ENV_VAR) != null)
                SettingsSkinportClientSecret.Password = Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_SECRET_ENV_VAR);
            SettingsMinWorthValue.Text = configuration.Minimum_Worth_Value.ToString();
            SettingsMinutesBetweenQuerries.Text = configuration.Minutes_Between_Queries.ToString();
            //Deals section
            SettingsDesiredItemsList.ItemsSource = desiredWeaponsList;
            BindingOperations.EnableCollectionSynchronization(desiredWeaponsList, _syncLock);
            //There's this dumb issue to if you don't refresh the list it simply won't show what's inside.
            SettingsDesiredDiscountThreshold.Text = configuration.Desired_Weapons_Min_Discount_Threshold.ToString();
            SettingsBuffCookie.Text = configuration.Buff_Cookie;
            SettingsGoodDiscountThreshold.Text = configuration.Good_Discount_Threshold.ToString();
            SettingsGreatDiscountThreshold.Text = configuration.Great_Discount_Threshold.ToString();
            SettingsOutstandingDiscountThreshold.Text = configuration.Outstanding_Discount_Threshold.ToString();
        }
        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            if (!HandleSavingErrors())
                return;
            //We update the Environment Variables, this data is stored in the environement for safety measures.
            Environment.SetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_SECRET_ENV_VAR, SettingsSkinportClientSecret.Password);
            Environment.SetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_CLIENT_ENV_VAR, SettingsSkinportClientId.Password);
            //We do a bunch of string editing.
            string newSettings = "{" +
                "\n\t\"desired_weapons_min_discount_threshold\": " + SettingsDesiredDiscountThreshold.Text + "," +
                "\n\t\"good_discount_threshold\": " + SettingsGoodDiscountThreshold.Text + "," +
                "\n\t\"great_discount_threshold\": " + SettingsGreatDiscountThreshold.Text + "," +
                "\n\t\"outstanding_discount_threshold\": " + SettingsOutstandingDiscountThreshold.Text + "," +
                "\n\t\"minimum_worth_value\": " + SettingsMinWorthValue.Text + "," +
                "\n\t\"minutes_between_queries\": " + SettingsMinutesBetweenQuerries.Text + "," +
                "\n\t\"desired_weapons\":[";
            if(desiredWeaponsList.Count > 0)
                for (int i = 0; i < desiredWeaponsList.Count; i++)
                {
                    newSettings += "\"" + desiredWeaponsList[i] + "\"";
                    if (i != desiredWeaponsList.Count - 1)
                        newSettings += ",";
                }
            newSettings += "\n\t]," +
            "\n\t\"notify_on_all_desired_weapons\":"+SettingsNotifyOnAllDesiredWeapons.IsChecked.ToString().ToLower()+"," +
            "\n\t\"notifications_enabled\":"+SettingsNotificationsEnabled.IsChecked.ToString().ToLower()+"," +
            "\n\t\"buff_cookie\":\""+SettingsBuffCookie.Text+"\"" +
            "\n}";
            //We overwrite the current Config File with our new settings.
            File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json", newSettings);
            configuration = JsonConvert.DeserializeObject<SkinHoundConfiguration>(newSettings);
            //We check if the timer changed, if so we order an update
            if (timeIntervalBetweenQuerries != int.Parse(SettingsMinutesBetweenQuerries.Text))
                ChangeRefreshIntervals(int.Parse(SettingsMinutesBetweenQuerries.Text));
            //We order an update on the API factories with our new config.
            UpdateConfigurationInFactories();
            //We Notify the user if the operation was a success.
            SettingsErrorText.Text = "Settings saved.";
        }
        private void ResetSettings(object sender, RoutedEventArgs e)
        {
            //We overwrite the current Config File with the Default settings.
            File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json", DEFAULT_CONFIG_FILE_CONTENT);
            configuration = JsonConvert.DeserializeObject<SkinHoundConfiguration>(DEFAULT_CONFIG_FILE_CONTENT);
            //We check if the timer changed, if so we order an update
            if (timeIntervalBetweenQuerries != 2)
                ChangeRefreshIntervals(2);
            //We order an update on the API factories with our new config.
            UpdateConfigurationInFactories();
            //Since we are resetting everything we reinit the settings Value
            InitSettingsValue();
        }
        private bool HandleSavingErrors()
        {
            //We make sure that if any of the variables we have can't be parsed, it won't allow it.
            double doubleTester;
            int intTester;
            if(!int.TryParse(SettingsMinutesBetweenQuerries.Text, out intTester) ||!double.TryParse(SettingsMinWorthValue.Text, out doubleTester)||!double.TryParse(SettingsGoodDiscountThreshold.Text, out doubleTester) ||!double.TryParse(SettingsGreatDiscountThreshold.Text, out doubleTester) ||!double.TryParse(SettingsOutstandingDiscountThreshold.Text, out doubleTester) || double.TryParse(SettingsDesiredDiscountThreshold.Text, out doubleTester))
            if (SettingsMinutesBetweenQuerries.Text == "" || SettingsMinWorthValue.Text == "" || SettingsGoodDiscountThreshold.Text == "" || SettingsGreatDiscountThreshold.Text == "" || SettingsOutstandingDiscountThreshold.Text == "" || SettingsDesiredDiscountThreshold.Text == "")
            {
                SettingsErrorText.Text = "Error: One of these values is Invalid: MinWorth, MinutesBetweenQuerries, GoodDiscountThreshold, GreatDiscountThreshold, OutstandingDiscountThreshold, DesiredDiscountThreshold.";
                return false;
            }
            //Error handling to make sure required values aren't null
            if (SettingsMinutesBetweenQuerries.Text == "" || SettingsMinWorthValue.Text == "" || SettingsGoodDiscountThreshold.Text == "" || SettingsGreatDiscountThreshold.Text == "" || SettingsOutstandingDiscountThreshold.Text == "" || SettingsDesiredDiscountThreshold.Text == "")
            {
                SettingsErrorText.Text = "Error: These values can't be left empty: MinWorth, MinutesBetweenQuerries, GoodDiscountThreshold, GreatDiscountThreshold, OutstandingDiscountThreshold, DesiredDiscountThreshold.";
                return false;
            }
            //Error handling to make sure discount thresholds are bigger in the following order: Good < Great < Outstanding
            if (double.Parse(SettingsGoodDiscountThreshold.Text) > double.Parse(SettingsGreatDiscountThreshold.Text) || double.Parse(SettingsGoodDiscountThreshold.Text) > double.Parse(SettingsOutstandingDiscountThreshold.Text))
            {
                SettingsErrorText.Text = "Error: Good Discount Threshold must be smaller than Great Discount Threshold and Outstanding Discount Threshold.";
                return false;
            }
            if (double.Parse(SettingsGreatDiscountThreshold.Text) > double.Parse(SettingsOutstandingDiscountThreshold.Text))
            {
                SettingsErrorText.Text = "Error: Great Discount Threshold must be smaller than Outstanding Discount Threshold.";
                return false;
            }
            if (double.Parse(SettingsGoodDiscountThreshold.Text) < 15 || double.Parse(SettingsGreatDiscountThreshold.Text) < 15 || double.Parse(SettingsOutstandingDiscountThreshold.Text) < 15 || double.Parse(SettingsDesiredDiscountThreshold.Text) < 10)
            {
                SettingsErrorText.Text = "Error: Global Discount Thresholds must be equal or higher than 15% and Desired item Threshold must be equal or higher than 10%.";
                return false;
            }
            //Error handling to make sure that the minutes between querries isn't lower than 1 to avoid the user getting timed out.
            if (int.Parse(SettingsMinutesBetweenQuerries.Text) < 1)
            {
                SettingsErrorText.Text = "Error: Minutes between querries can't be lower than 1.";
                return false;
            }
            return true;
        }
        private async void InitializeSuggestionLists()
        {
            skinNamesList = await skinportApiFactory.GetItemsNameList();
        }
        public void ChangeRefreshIntervals(int period)
        {
            timeIntervalBetweenQuerries = period * 60 * 1000;
            refreshProcess.Change(10000, timeIntervalBetweenQuerries);
        }
        private void DealsGridHandler(object? state)
        {
            RefreshDeals().GetAwaiter().GetResult();
            //We initialize the SuggetionLists if it's empty
            if(skinNamesList.Count == 0)
                InitializeSuggestionLists();
        }
        private async Task RefreshDeals()
        {
            this.Dispatcher.Invoke(() => { loadingGif.Visibility = Visibility.Visible; });
            await RemoveDeals();
            Queue<Product> deals = new Queue<Product>();
            List<Product> list = await skinportApiFactory.AcquireProductList();
            if(list != null)
            {
                foreach (var element in list)
                {
                    deals.Enqueue(element);
                }
                await ShowDeals(deals);

            }
            this.Dispatcher.Invoke(() => { loadingGif.Visibility = Visibility.Hidden; });
            return;
        }
        private async Task RemoveDeals()
        {
            this.Dispatcher.Invoke(() => { dealsGrid.Children.Clear(); });
            return;
        }
        //This Task takes care of updating the deals for the user and formats them with the values which have been placed inside of it.
        private async Task<Queue<Product>> ShowDeals(Queue<Product> productQueue)
        {
            await Application.Current.Dispatcher.InvokeAsync( async () =>
            {
                BuffMarketHistory buffMarketHistory = buff163ApiFactory.GetBuffMarketHistory();
                if (productQueue == null || productQueue.Count > 0)
                {
                    ItemDeal curDeal = new ItemDeal(dealScroll);
                    //In this section we rename everything and keep them as a variable to give them a new value later on.
                    ((Grid)curDeal.FindName("DealXGrid")).Name = $"Deal{productQueue.Count}Grid";
                    Button itemButton = ((Button)curDeal.FindName("DealButtonX"));
                    itemButton.Name = $"DealButton{productQueue.Count}";
                    TextBlock itemName = ((TextBlock)curDeal.FindName("DealXItemName"));
                    itemName.Name = $"Deal{productQueue.Count}ItemName";
                    Image itemImage = ((Image)curDeal.FindName("DealXImage"));
                    itemImage.Name = $"Deal{productQueue.Count}Image";
                    //Skinport Variables
                    TextBlock itemSkinportDiscount = ((TextBlock)curDeal.FindName("DealXSkinportDiscount"));
                    itemSkinportDiscount.Name = $"Deal{productQueue.Count}SkinportDiscount";
                    TextBlock itemPrice = ((TextBlock)curDeal.FindName("DealXSkinportPrice"));
                    itemPrice.Name = $"Deal{productQueue.Count}SkinportPrice";
                    TextBlock itemSkinportVolumeSold30Days = ((TextBlock)curDeal.FindName("DealXSkinportVolumeSoldLast30Days"));
                    itemSkinportVolumeSold30Days.Name = $"Deal{productQueue.Count}SkinportVolumeSoldLast30Days";
                    TextBlock itemSkinportMedianSold30Days = ((TextBlock)curDeal.FindName("DealXSkinportMedianSoldLast30Days"));
                    itemSkinportMedianSold30Days.Name = $"Deal{productQueue.Count}SkinportMedianSoldLast30Days";
                    //Buff163 Variables
                    TextBlock itemBuffStartingAt = ((TextBlock)curDeal.FindName("DealXBuffStartingAt"));
                    itemBuffStartingAt.Name = $"Deal{productQueue.Count}BuffStartingAt";
                    TextBlock itemBuffHighestOrder = ((TextBlock)curDeal.FindName("DealXBuffHighestOrder"));
                    itemBuffHighestOrder.Name = $"Deal{productQueue.Count}BuffHighestOrder";
                    //Steam Variables
                    TextBlock itemSteamLast7Days = ((TextBlock)curDeal.FindName("DealXSteamLast7Days"));
                    itemSteamLast7Days.Name = $"Deal{productQueue.Count}SteamLast7Days";
                    TextBlock itemSteamLast30Days = ((TextBlock)curDeal.FindName("DealXSteamLast30Days"));
                    itemSteamLast30Days.Name = $"Deal{productQueue.Count}SteamLast30Days";
                    //SkinHound Variables
                    TextBlock itemRecommendedDiscount = ((TextBlock)curDeal.FindName("DealXRecommendedDiscount"));
                    itemRecommendedDiscount.Name = $"Deal{productQueue.Count}RecommendedDiscount";
                    TextBlock itemRecommendedSalePrice = ((TextBlock)curDeal.FindName("DealXRecommendedSalePrice"));
                    itemRecommendedSalePrice.Name = $"Deal{productQueue.Count}RecommendedSalePrice";
                    TextBlock itemProfitPOnResale = ((TextBlock)curDeal.FindName("DealXProfitPOnResale"));
                    itemProfitPOnResale.Name = $"Deal{productQueue.Count}ProfitPOnResale";
                    TextBlock itemProfitCOnResale = ((TextBlock)curDeal.FindName("DealXProfitCOnResale"));
                    itemProfitCOnResale.Name = $"Deal{productQueue.Count}ProfitCOnResale";
                    TextBlock itemLongTermInvestmentIndicator = ((TextBlock)curDeal.FindName("DealXLTII"));
                    itemLongTermInvestmentIndicator.Name = $"Deal{productQueue.Count}LTII";                    
                    TextBlock itemInvestmentValue = ((TextBlock)curDeal.FindName("DealXMovingAverage"));
                    itemInvestmentValue.Name = $"Deal{productQueue.Count}MovingAverage";
                    //We Dequeue the product, gather its global data and start assigning its values in the front-end
                    Product curProduct = productQueue.Dequeue();
                    GlobalMarketDataObject curItemGlobalData = await csgoTradersPriceFactory.GetItemGlobalData(curProduct.Market_Hash_Name);
                    //We begin initializing the values.
                    itemName.Text = curProduct.Market_Hash_Name;
                    itemButton.Tag = curProduct.Item_Page;
                    itemSkinportDiscount.Text = $"{curProduct.Percentage_Off}%";
                    itemPrice.Text = $"{curProduct.Min_Price.ToString("0.00")}$";
                    itemSkinportVolumeSold30Days.Text = $"{curProduct.productMarketHistory.Last_30_days.Volume}";
                    itemSkinportMedianSold30Days.Text = $"{curProduct.productMarketHistory.Last_30_days.Median.ToString("0.00")}$";
                    itemBuffStartingAt.Text = $"{(curItemGlobalData.Buff163.Starting_At * Utils.usdToCadRate).ToString("0.00")}$";
                    itemBuffHighestOrder.Text = $"{(curItemGlobalData.Buff163.Highest_Order * Utils.usdToCadRate).ToString("0.00")}$";
                    itemSteamLast7Days.Text = $"{(curItemGlobalData.Steam.Last_7d * Utils.usdToCadRate).ToString("0.00")}$";
                    itemSteamLast30Days.Text = $"{(curItemGlobalData.Steam.Last_30d * Utils.usdToCadRate).ToString("0.00")}$";
                    itemRecommendedDiscount.Text = $"{curProduct.recommendedDiscount}";
                    itemRecommendedSalePrice.Text = $"{curProduct.recommendedResellPrice}";
                    itemProfitPOnResale.Text = $"{curProduct.profitPercentageOnResellPrice}";
                    itemProfitCOnResale.Text = $"{curProduct.profitMoneyOnResellPrice}";
                    itemLongTermInvestmentIndicator.Text = $"{await curProduct.productMarketHistory.GetLongTermPercentageProfit(curProduct)}%";
                    itemInvestmentValue.Text = $"{await curProduct.productMarketHistory.GetLongMovingMedian()}$";
                    //We check if Buff is functionnal, in the case where it is, the image source and information changes
                    if (buffCookieFunctionnal)
                    {
                        //We verify that the item in question isn't null before doing anything.
                        BuffItem buffItem = await buff163ApiFactory.GetItem(curProduct.Market_Hash_Name);
                        if (buffItem != null)
                        {
                            //This section gives the item its actual image, if it can't be found it assigns a placeholder which represents the deal's type.
                            if (buffItem.Goods_Info != null)
                                itemImage.Source = new BitmapImage(new Uri($"{buffItem.Goods_Info.Icon_Url}", UriKind.Absolute));
                            else
                                itemImage.Source = new BitmapImage(new Uri($"{curProduct.imagePath}", UriKind.Relative));
                        }
                        else
                            itemImage.Source = new BitmapImage(new Uri($"{curProduct.imagePath}", UriKind.Relative));
                    }
                    else
                    {
                        //Here we have to initiate a new image in order to assign a new ImageSource
                        itemImage.Source = new BitmapImage(new Uri($"{curProduct.imagePath}", UriKind.Relative));
                    }
                    dealsGrid.Children.Add(curDeal);
                    return await ShowDeals(productQueue);
                }
                else
                    return productQueue;
            });
            return productQueue;
        }
        //Function used to refresh the Data from buff.
        private async void RefreshBuffMarketValue()
        {
            buffCookieFunctionnal = await buff163ApiFactory.VerifyCookie();
        }
        //This function is responsible for acquiring the user configs in /AppData/Roaming/SkinHound
        private void GetUserConfigFromFile()
        {
            //If the config file does not already exist on the machine, we create it.
            if (!Directory.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound"))
                Directory.CreateDirectory($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound");
            if (!File.Exists($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json"))
            {
                var configFile = File.CreateText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json");
                configFile.Write(DEFAULT_CONFIG_FILE_CONTENT);
                configFile.Flush();
            }
            configuration = JsonConvert.DeserializeObject<SkinHoundConfiguration>(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json"));
            foreach (string name in configuration.Desired_Weapons)
                desiredWeaponsList.Add(name);
        }
        //Sets the Configuration variable in all of the API factories
        private async void UpdateConfigurationInFactories()
        {
            skinportApiFactory.SetConfig(configuration);
            buff163ApiFactory.SetConfig(configuration);
        }
        //This method is used by all of the fields which accept a double value and is needed to prevent unwanted characters.
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            // Only allow numbers and at most one decimal point
            Regex regex = new Regex("[^0-9.]");
            // Check if the new text matches the pattern
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            if (regex.IsMatch(newText))
            {
                e.Handled = true;
                return;
            }
            // Check if the new text has more than two decimal places
            int decimalIndex = newText.IndexOf('.');
            if (decimalIndex >= 0 && (newText.Length - decimalIndex > 3 || decimalIndex == 0))
            {
                e.Handled = true;
                return;
            }
            // Check if the new text has a decimal point that is not at the beginning and is not preceded by another decimal point
            if (e.Text == "." && (decimalIndex == 0 || newText.Split(".").Length-1 > 1))
            {
                e.Handled= true;
                return;
            }
            //Make sure that we don't exceed the double Max Value to avoir breaking the app.
            double value = 0;
            bool ok = double.TryParse(newText, out value);
            if (!double.TryParse(newText, out value))
            {
                e.Handled = true;
                return;
            }
        }
        //This method is used by the Minutes between querry field and is needed to prevent unwanted characters.
        private void MinutesBetweenQuerriesValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            // Only allow numbers and at most one decimal point
            Regex regex = new Regex("[^0-9]");
            // Check if the new text matches the pattern
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            if (regex.IsMatch(newText))
            {
                e.Handled = true;
                return;
            }
            // Prevent the new Text from starting with 0
            if (e.Text == "0" && newText.Length == 0)
            {
                e.Handled = true;
                return;
            }
            //Make sure that we don't exceed the Integer Max Value to avoir breaking the app.
            if (long.Parse(newText) > int.MaxValue)
            {
                e.Handled = true;
                return;
            }
        }

        private async void SettingsSuggestionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await UpdateSettingsSuggestionBox();
        }
        private async Task UpdateSettingsSuggestionBox()
        {
            try
            {
                if (string.IsNullOrEmpty(this.SettingsSuggestingTextBox.Text))
                {
                    this.CloseSettingsAutoSkinSuggestionBox();
                    return;
                }
                this.OpenSettingsAutoSkinSuggestionBox();
                var listToShow = this.skinNamesList.Where(text => text.ToLower().Contains(this.SettingsSuggestingTextBox.Text.ToLower())).ToList();
                if (listToShow.Count > 10)
                    listToShow = listToShow.GetRange(0, 10);
                this.SettingsSuggestionList.ItemsSource = listToShow;
                return;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
                return;
            }
            
        }
        private void SettingsSuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (this.SettingsSuggestionList.SelectedIndex <= -1)
                { 
                    this.CloseSettingsAutoSkinSuggestionBox();
                    return;
                }  
                this.CloseSettingsAutoSkinSuggestionBox();
                this.SettingsSuggestingTextBox.Text = this.SettingsSuggestionList.SelectedItem.ToString();
                this.SettingsSuggestionList.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void CloseSettingsAutoSkinSuggestionBox()
        {
            try
            {
                this.SettingsSuggestionListPopup.Visibility = Visibility.Collapsed;
                this.SettingsSuggestionListPopup.IsOpen = false;
                this.SettingsSuggestionList.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void OpenSettingsAutoSkinSuggestionBox()
        {
            try
            {
                this.SettingsSuggestionListPopup.Visibility = Visibility.Visible;
                this.SettingsSuggestionListPopup.IsOpen = true;
                this.SettingsSuggestionList.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        //These two methods take care of removing and adding new items to the Desired weapons list when their corresponding button is clicked.
        private void SettingsDesiredAddButton_Click(object sender, RoutedEventArgs e)
        {
            this.SettingsDesiredItemsList.Visibility = Visibility.Hidden;
            lock (_syncLock)
            {
                desiredWeaponsList.Add(SettingsSuggestingTextBox.Text);
            }
            this.SettingsSuggestingTextBox.Text = "";
            this.SettingsDesiredItemsList.Visibility = Visibility.Visible;
        }
        private void SettingsDesiredRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            this.SettingsDesiredItemsList.Visibility = Visibility.Hidden;
            if (SettingsDesiredItemsList.HasItems != false && SettingsDesiredItemsList.SelectedIndex != -1) lock (_syncLock)
                {
                    desiredWeaponsList.RemoveAt(SettingsDesiredItemsList.SelectedIndex);
                }
            this.SettingsDesiredItemsList.Visibility = Visibility.Visible;
        }
        //These methods are essentially the same thing as the settings one, the only difference is that they are not used by the same elements of the Application.
        //Instead of the settings, they are used in the PriceChecker.
        private async void PriceCheckerSuggestionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await UpdatePriceCheckerSuggestionBox();
        }
        private async Task UpdatePriceCheckerSuggestionBox()
        {
            try
            {
                if (string.IsNullOrEmpty(this.PriceCheckerSuggestingTextBox.Text))
                {
                    this.ClosePriceCheckerAutoSkinSuggestionBox();
                    return;
                }
                this.OpenPriceCheckerAutoSkinSuggestionBox();
                var listToShow = this.skinNamesList.Where(text => text.ToLower().Contains(this.PriceCheckerSuggestingTextBox.Text.ToLower())).ToList();
                if (listToShow.Count > 10)
                    listToShow = listToShow.GetRange(0, 10);
                this.PriceCheckerSuggestionList.ItemsSource = listToShow;
                return;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
                return;
            }

        }
        private void PriceCheckerSuggestionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (this.PriceCheckerSuggestionList.SelectedIndex <= -1)
                {
                    this.ClosePriceCheckerAutoSkinSuggestionBox();
                    return;
                }
                this.ClosePriceCheckerAutoSkinSuggestionBox();
                this.PriceCheckerSuggestingTextBox.Text = this.PriceCheckerSuggestionList.SelectedItem.ToString();
                this.PriceCheckerSuggestionList.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                // Info.  
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void ClosePriceCheckerAutoSkinSuggestionBox()
        {
            try
            {
                this.PriceCheckerSuggestionListPopup.Visibility = Visibility.Collapsed;
                this.PriceCheckerSuggestionListPopup.IsOpen = false;
                this.PriceCheckerSuggestionList.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        private void OpenPriceCheckerAutoSkinSuggestionBox()
        {
            try
            {
                this.PriceCheckerSuggestionListPopup.Visibility = Visibility.Visible;
                this.PriceCheckerSuggestionListPopup.IsOpen = true;
                this.PriceCheckerSuggestionList.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.Write(ex);
            }
        }
        //This method is called when the price check button is clicked.
        private async void LaunchPriceCheck(object sender, RoutedEventArgs e)
        {
            //We initialize our display by setting everything to nothing.
            PriceCheckerMessageBox.Text = "Enter a Skin Name to Price Check it";
            PriceCheckGrid.Children.Clear();
            //Then we make a request to our factory and find out soon enough if the Skin exists.
            Queue<Product> productsToDisplay = await skinportApiFactory.PriceCheck(PriceCheckerSuggestingTextBox.Text);
            if(productsToDisplay.Count == 0)
            {
                PriceCheckerMessageBox.Text = $"Couldn't find any skins with the name \"{PriceCheckerSuggestingTextBox.Text}\"";
                return;
            }
            //At this point, we know it exists and order for it to show.
            await DisplayPriceCheckedItems(productsToDisplay);
        }
        //This method does essentially the same thing as the one which displays our Deals except it is for the PriceChecks.
        private async Task<Queue<Product>> DisplayPriceCheckedItems(Queue<Product> productQueue)
        {
            BuffMarketHistory buffMarketHistory = buff163ApiFactory.GetBuffMarketHistory();
            if (productQueue == null || productQueue.Count > 0)
            {
                PriceCheckedItem curPriceCheckedProduct = new PriceCheckedItem(PriceCheckerScrollBar);
                //In this section we rename everything and keep them as a variable to give them a new value later on.
                ((Grid)curPriceCheckedProduct.FindName("PriceCheckedXGrid")).Name = $"PriceChecked{productQueue.Count}Grid";
                Button itemButton = ((Button)curPriceCheckedProduct.FindName("PriceCheckedButtonX"));
                itemButton.Name = $"PriceCheckedButton{productQueue.Count}";
                TextBlock itemName = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXItemName"));
                itemName.Name = $"PriceChecked{productQueue.Count}ItemName";
                Image itemImage = ((Image)curPriceCheckedProduct.FindName("PriceCheckedXImage"));
                itemImage.Name = $"PriceChecked{productQueue.Count}Image";
                //Skinport Variables
                TextBlock itemSkinportDiscount = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXSkinportDiscount"));
                itemSkinportDiscount.Name = $"PriceChecked{productQueue.Count}SkinportDiscount";
                TextBlock itemPrice = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXSkinportPrice"));
                itemPrice.Name = $"PriceChecked{productQueue.Count}SkinportPrice";
                TextBlock itemSkinportVolumeSold30Days = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXSkinportVolumeSoldLast30Days"));
                itemSkinportVolumeSold30Days.Name = $"PriceChecked{productQueue.Count}SkinportVolumeSoldLast30Days";
                TextBlock itemSkinportMedianSold30Days = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXSkinportMedianSoldLast30Days"));
                itemSkinportMedianSold30Days.Name = $"PriceChecked{productQueue.Count}SkinportMedianSoldLast30Days";
                //Buff163 Variables
                TextBlock itemBuffStartingAt = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXBuffStartingAt"));
                itemBuffStartingAt.Name = $"PriceChecked{productQueue.Count}BuffStartingAt";
                TextBlock itemBuffHighestOrder = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXBuffHighestOrder"));
                itemBuffHighestOrder.Name = $"PriceChecked{productQueue.Count}BuffHighestOrder";
                //Steam Variables
                TextBlock itemSteamLast7Days = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXSteamLast7Days"));
                itemSteamLast7Days.Name = $"PriceChecked{productQueue.Count}SteamLast7Days";
                TextBlock itemSteamLast30Days = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXSteamLast30Days"));
                itemSteamLast30Days.Name = $"PriceChecked{productQueue.Count}SteamLast30Days";
                //SkinHound Variables
                TextBlock itemRecommendedDiscount = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXRecommendedDiscount"));
                itemRecommendedDiscount.Name = $"PriceChecked{productQueue.Count}RecommendedDiscount";
                TextBlock itemRecommendedSalePrice = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXRecommendedSalePrice"));
                itemRecommendedSalePrice.Name = $"PriceChecked{productQueue.Count}RecommendedSalePrice";
                TextBlock itemProfitPOnResale = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXProfitPOnResale"));
                itemProfitPOnResale.Name = $"PriceChecked{productQueue.Count}ProfitPOnResale";
                TextBlock itemProfitCOnResale = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXProfitCOnResale"));
                itemProfitCOnResale.Name = $"PriceChecked{productQueue.Count}ProfitCOnResale";
                TextBlock itemLongTermInvestmentIndicator = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXLTII"));
                itemLongTermInvestmentIndicator.Name = $"PriceChecked{productQueue.Count}LTII";
                TextBlock itemInvestmentValue = ((TextBlock)curPriceCheckedProduct.FindName("PriceCheckedXMovingAverage"));
                itemInvestmentValue.Name = $"PriceChecked{productQueue.Count}MovingAverage";
                //We Dequeue the product, gather its global data and start assigning its values in the front-end
                Product curProductObject = productQueue.Dequeue();
                GlobalMarketDataObject curItemGlobalData = await csgoTradersPriceFactory.GetItemGlobalData(curProductObject.Market_Hash_Name);
                //We begin initializing the values.
                itemName.Text = curProductObject.Market_Hash_Name;
                itemButton.Tag = curProductObject.Item_Page;
                itemSkinportDiscount.Text = $"{curProductObject.Percentage_Off}%";
                itemPrice.Text = $"{curProductObject.Min_Price.ToString("0.00")}$";
                itemSkinportVolumeSold30Days.Text = $"{curProductObject.productMarketHistory.Last_30_days.Volume}";
                itemSkinportMedianSold30Days.Text = $"{curProductObject.productMarketHistory.Last_30_days.Median.ToString("0.00")}$";
                itemBuffStartingAt.Text = $"{(curItemGlobalData.Buff163.Starting_At * Utils.usdToCadRate).ToString("0.00")}$";
                itemBuffHighestOrder.Text = $"{(curItemGlobalData.Buff163.Highest_Order * Utils.usdToCadRate).ToString("0.00")}$";
                itemSteamLast7Days.Text = $"{(curItemGlobalData.Steam.Last_7d * Utils.usdToCadRate).ToString("0.00")}$";
                itemSteamLast30Days.Text = $"{(curItemGlobalData.Steam.Last_30d * Utils.usdToCadRate).ToString("0.00")}$";
                itemRecommendedDiscount.Text = $"{curProductObject.recommendedDiscount}";
                itemRecommendedSalePrice.Text = $"{curProductObject.recommendedResellPrice}";
                itemProfitPOnResale.Text = $"{curProductObject.profitPercentageOnResellPrice}";
                itemProfitCOnResale.Text = $"{curProductObject.profitMoneyOnResellPrice}";
                itemLongTermInvestmentIndicator.Text = $"{await curProductObject.productMarketHistory.GetLongTermPercentageProfit(curProductObject)}%";
                itemInvestmentValue.Text = $"{await curProductObject.productMarketHistory.GetLongMovingMedian()}$";
                //We check if Buff is functionnal, in the case where it is, the image source and information changes
                if (buffCookieFunctionnal)
                {
                    //We verify that the item in question isn't null before doing anything.
                    BuffItem buffItem = await buff163ApiFactory.GetItem(curProductObject.Market_Hash_Name);
                    if (buffItem != null)
                    {
                        //This section gives the item its actual image, if it can't be found it assigns a placeholder which represents the product's type.
                        if (buffItem.Goods_Info != null)
                            itemImage.Source = new BitmapImage(new Uri($"{buffItem.Goods_Info.Icon_Url}", UriKind.Absolute));
                    }
                }
                PriceCheckGrid.Children.Add(curPriceCheckedProduct);
                return await DisplayPriceCheckedItems(productQueue);
            } else
                return productQueue;
        }
    }
}
