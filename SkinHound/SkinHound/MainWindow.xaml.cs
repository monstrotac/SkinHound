using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
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
    
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //Private fields
        private SkinportApiFactory skinportApiFactory;
        private Timer refreshProcess;
        private int timeIntervalBetweenQuerries;
        //Used for the current filter applied to the Displayed deals
        DealsFilterType dealsFilterType = DealsFilterType.PriceAsc;
        //Important Variable for Price Display depending on currency
        private string currencySymbol = "$";
        //lock object for synchronization;
        private static object _syncLock = new object();
        //Suggestion TextBox related fields
        private List<string> skinNamesList = new List<string>();
        private ObservableCollection<string> desiredWeaponsList = new ObservableCollection<string>();
        //Deals that will be displayed (We create a binding with this)
        private ObservableCollection<PriceCheckedItem> _displayedPriceChecks;
        public ObservableCollection<PriceCheckedItem> DisplayedPriceChecks
        {
            get { return _displayedPriceChecks; }
            set
            {
                _displayedPriceChecks = value;
                OnPropertyChanged(nameof(DisplayedPriceChecks));
            }
        }
        private ObservableCollection<ItemDeal> _displayedDeals;
        public ObservableCollection<ItemDeal> DisplayedDeals
        {
            get { return _displayedDeals; }
            set
            {
                _displayedDeals = value;
                OnPropertyChanged(nameof(DisplayedDeals));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //Components
        private WrapPanel dealsGrid;
        private Image loadingGif;
        private ScrollViewer dealScroll;
        private ScrollViewer priceCheckScroll;
        private WsClient SkinportWebSocket { get; set; }
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
          "\n\t\"currency\":\"CAD\"" +
          "\n}";

        public MainWindow()
        {
            DisplayedDeals = new ObservableCollection<ItemDeal>();
            DisplayedPriceChecks = new ObservableCollection<PriceCheckedItem>();
            //The very first step is to acquire the configuration from the file, if it exists.
            GetUserConfigFromFile();
            //Then we update the symbol to correspond to the currency's.
            UpdateCurrencySymbol();
            //Then we proceed to Initialize our APIFactories followed by the components.
            skinportApiFactory = new SkinportApiFactory();
            CSGOTradersPricesFactory.PrepareData();
            InitializeComponent();
            DataContext = this;
            //Websocket Initialization and DataContext set
            SkinportWebSocket = new WsClient(ActivityFeedStatus);
            ActivityFeed.DataContext = SkinportWebSocket.DisplayedSaleFeed;

            ActivityFeedUntrackedCheckBox.DataContext = SkinportWebSocket;
            ActivityFeedListingCheckBox.DataContext = SkinportWebSocket;
            ActivityFeedSalesCheckBox.DataContext = SkinportWebSocket;
            ActivityFeedGoodDealsCheckBox.DataContext = SkinportWebSocket;
            ActivityFeedDesiredWeaponsCheckBox.DataContext = SkinportWebSocket;

            InitWebSocket();
            //We take a quick moment to Init the values of the settings.
            InitSettingsValue();
            //We update the value of the rates for money conversion later on since everything the Buff163 API returns is in CNY
            Utils.UpdateRates();
            //Initialize the methods linked to components of the application.
            loadingGif = (Image)FindName("LoadingIcon");
            dealScroll = (ScrollViewer)FindName("DealScrollBar");
            priceCheckScroll = (ScrollViewer)FindName("PriceCheckerScrollBar");
            //We start the timer which will automate the deals and refresh them on X configured basis.
            timeIntervalBetweenQuerries = 1000 * 60 * SkinHoundConfiguration.Minutes_Between_Queries;
            refreshProcess = new Timer(DealsGridHandler, null, 0, timeIntervalBetweenQuerries);

        }
        private async void InitWebSocket()
        {
            await SkinportWebSocket.Connect("wss://skinport.com/socket.io/?EIO=4&transport=websocket");
        }
        private void InitSettingsValue()
        {
            //Check which index represents the currency
            int currencyIndex = 0;
            switch (SkinHoundConfiguration.Currency)
            {
                case "CAD":
                    currencyIndex = 0;
                    break;
                case "EUR":
                    currencyIndex = 1;
                    break;
                case "USD":
                    currencyIndex = 2;
                    break;
            }
            //Notification section
            SettingsNotificationsEnabled.IsChecked = SkinHoundConfiguration.Notifications_Enabled;
            SettingsNotifyOnAllDesiredWeapons.IsChecked = SkinHoundConfiguration.Notify_On_All_Desired_Weapons;
            //General section
            if(Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_CLIENT_ENV_VAR) != null)
                SettingsSkinportClientId.Password = Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_CLIENT_ENV_VAR);
            if (Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_SECRET_ENV_VAR) != null)
                SettingsSkinportClientSecret.Password = Environment.GetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_SECRET_ENV_VAR);
            SettingsMinWorthValue.Text = SkinHoundConfiguration.Minimum_Worth_Value.ToString("0.00");
            SettingsMinutesBetweenQuerries.Text = SkinHoundConfiguration.Minutes_Between_Queries.ToString();
            SettingsCurrencyList.SelectedIndex = currencyIndex;
            //Deals section
            SettingsDesiredItemsList.ItemsSource = desiredWeaponsList;
            BindingOperations.EnableCollectionSynchronization(desiredWeaponsList, _syncLock);
            //There's this dumb issue to if you don't refresh the list it simply won't show what's inside.
            SettingsDesiredDiscountThreshold.Text = SkinHoundConfiguration.Desired_Weapons_Min_Discount_Threshold.ToString();
            SettingsGoodDiscountThreshold.Text = SkinHoundConfiguration.Good_Discount_Threshold.ToString();
            SettingsGreatDiscountThreshold.Text = SkinHoundConfiguration.Great_Discount_Threshold.ToString();
            SettingsOutstandingDiscountThreshold.Text = SkinHoundConfiguration.Outstanding_Discount_Threshold.ToString();
        }
        //This function is needed to update what symbol we're showing on the UI depending on the currency.
        private void UpdateCurrencySymbol()
        {
            switch (SkinHoundConfiguration.Currency)
            {
                case "CAD":
                    currencySymbol = "$";
                    break;
                case "EUR":
                    currencySymbol = "€";
                    break;
                case "USD":
                    currencySymbol = "$";
                    break;
            }
        }
        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            if (!HandleSavingErrors())
                return;
            string currencyString = SkinHoundConfiguration.Currency;
            switch (SettingsCurrencyList.SelectedIndex)
            {
                case 0://CAD
                    currencyString = "CAD";
                    break;
                case 1://EUR
                    currencyString = "EUR";
                    break;
                case 2://USD
                    currencyString = "USD";
                    break;
            }

            //We update the Environment Variables, this data is stored in the environement for safety measures.
            Environment.SetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_SECRET_ENV_VAR, SettingsSkinportClientSecret.Password, EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable(SkinportApiFactory.SKINPORT_TOKEN_CLIENT_ENV_VAR, SettingsSkinportClientId.Password, EnvironmentVariableTarget.User);
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
            "\n\t\"notifications_enabled\":"+SettingsNotificationsEnabled.IsChecked.ToString().ToLower()+ "," +
            "\n\t\"currency\":\""+ currencyString +"\"" +
            "\n}";
            //We overwrite the current Config File with our new settings.
            File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json", newSettings);
            SkinHoundConfiguration.SetNewConfig(JsonConvert.DeserializeObject<ConfigurationObject>(newSettings));
            //We check if the timer changed, if so we order an update
            if (timeIntervalBetweenQuerries != int.Parse(SettingsMinutesBetweenQuerries.Text))
                ChangeRefreshIntervals(int.Parse(SettingsMinutesBetweenQuerries.Text));
            //We update the currency symbol
            UpdateCurrencySymbol();
            //We Notify the user if the operation was a success.
            SettingsErrorText.Text = "Settings saved.";
        }
        private void ResetSettings(object sender, RoutedEventArgs e)
        {
            //We overwrite the current Config File with the Default settings.
            File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json", DEFAULT_CONFIG_FILE_CONTENT);
            SkinHoundConfiguration.SetNewConfig(JsonConvert.DeserializeObject<ConfigurationObject>(DEFAULT_CONFIG_FILE_CONTENT));
            //We check if the timer changed, if so we order an update
            if (timeIntervalBetweenQuerries != 2)
                ChangeRefreshIntervals(2);
            //We update the currency symbol
            UpdateCurrencySymbol();
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
            refreshProcess.Change(3000, timeIntervalBetweenQuerries);
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
            await this.Dispatcher.Invoke(async () =>
            {
                await RemoveDeals();
            });
            //We acquire the product list.
            List<Product> list = await skinportApiFactory.AcquireProductList();
            if(list != null)
            {
                await this.Dispatcher.Invoke(async () =>
                {
                    await ShowDeals(list);
                    await FilterDealList();
                });
            }
            this.Dispatcher.Invoke(() => { loadingGif.Visibility = Visibility.Hidden; });
            return;
        }
        private async Task RemoveDeals()
        {
            DisplayedDeals.Clear();
            return;
        }
        //This Task takes care of updating the deals for the user and formats them with the values which have been placed inside of it.
        private async Task ShowDeals(List<Product> productQueue)
        {
            await Application.Current.Dispatcher.InvokeAsync( async () =>
            {
                if (productQueue != null || productQueue.Count > 0)
                {
                    foreach(Product product in productQueue)
                    {
                        ItemDeal curDeal = new ItemDeal(dealScroll, product, currencySymbol);
                        DisplayedDeals.Add(curDeal);
                    }
                    return;
                }
                else
                    return;
            });
            return;
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
            SkinHoundConfiguration.SetNewConfig(JsonConvert.DeserializeObject<ConfigurationObject>(File.ReadAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SkinHound\\config.json")));
            foreach (string name in SkinHoundConfiguration.Desired_Weapons)
                desiredWeaponsList.Add(name);
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
            DisplayedPriceChecks.Clear();
            //We initialize our display by setting everything to nothing.
            PriceCheckerMessageBox.Text = "Enter a Skin Name to Price Check it";
            //Then we make a request to our factory and find out soon enough if the Skin exists.
            List<Product> productsToDisplay = await skinportApiFactory.PriceCheck(PriceCheckerSuggestingTextBox.Text);
            if(productsToDisplay == null)
            {
                PriceCheckerMessageBox.Text = $"An error occured searching for \"{PriceCheckerSuggestingTextBox.Text}\", please wait a few seconds and try again.";
                return;
            }
            if(productsToDisplay.Count == 0)
            {
                PriceCheckerMessageBox.Text = $"Couldn't find any skins with the name \"{PriceCheckerSuggestingTextBox.Text}\"";
                return;
            }
            //At this point, we know it exists and order for it to show.
            await DisplayPriceCheckedItems(productsToDisplay);
        }
        //This method does essentially the same thing as the one which displays our Deals except it is for the PriceChecks.
        private async Task DisplayPriceCheckedItems(List<Product> productList)
        {
            if (productList == null || productList.Count > 0)
            {
                foreach(Product product in productList)
                {
                    PriceCheckedItem curPriceCheckedProduct = new PriceCheckedItem(priceCheckScroll, product, currencySymbol);
                    DisplayedPriceChecks.Add(curPriceCheckedProduct);
                }
                return;
            } else
                return;
        }
        //Method used to filter through the shown deals.
        private async void DealsFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (DealsFilterBox.SelectedIndex)
            {
                case 0:
                    dealsFilterType = DealsFilterType.PriceAsc;
                    break;
                case 1:
                    dealsFilterType = DealsFilterType.PriceDesc;
                    break;
                case 2:
                    dealsFilterType = DealsFilterType.Newest;
                    break;
                case 3:
                    dealsFilterType = DealsFilterType.DesiredDeals;
                    break;
                case 4:
                    dealsFilterType = DealsFilterType.QualityAsc;
                    break;
                case 5:
                    dealsFilterType = DealsFilterType.QualityDesc;
                    break;
                case 6:
                    dealsFilterType = DealsFilterType.Name;
                    break;
                case 7:
                    dealsFilterType = DealsFilterType.InvestmentValueAsc;
                    break;
                case 8:
                    dealsFilterType = DealsFilterType.InvestmentValueDesc;
                    break;
                case 9:
                    dealsFilterType = DealsFilterType.LtiiAsc;
                    break;
                case 10:
                    dealsFilterType = DealsFilterType.LtiiDesc;
                    break;
                default:
                    dealsFilterType = DealsFilterType.PriceAsc;
                    break;
            }
            //Once the filter has been changed, we make sure to apply it.
            await FilterDealList();
        }
        private async Task FilterDealList()
        {
            ObservableCollection<ItemDeal> newList;
            switch (dealsFilterType)
            {
                case DealsFilterType.PriceAsc:
                     newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderBy(item => item.product.Min_Price).ToList());
                    break;
                case DealsFilterType.PriceDesc:
                    newList= new ObservableCollection<ItemDeal>(DisplayedDeals.OrderByDescending(item => item.product.Min_Price).ToList());
                    break;
                case DealsFilterType.Newest:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderByDescending(item => item.product.isNew).ToList());
                    break;
                case DealsFilterType.DesiredDeals:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderByDescending(item => item.product.isDesired).ToList());
                    break;
                case DealsFilterType.QualityAsc:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderBy(item => (int)item.product.dealType).ToList());
                    break;
                case DealsFilterType.QualityDesc:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderByDescending(item => (int)item.product.dealType).ToList());
                    break;
                case DealsFilterType.Name:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderBy(item => item.product.Market_Hash_Name).ToList());
                    break;
                case DealsFilterType.InvestmentValueAsc:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderBy(item => item.product.InvestmentValue).ToList());
                    break;
                case DealsFilterType.InvestmentValueDesc:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderByDescending(item => item.product.InvestmentValue).ToList());
                    break;
                case DealsFilterType.LtiiAsc:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderBy(item => item.product.LongTermInvestmentIndicator).ToList());
                    break;
                case DealsFilterType.LtiiDesc:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderByDescending(item => item.product.LongTermInvestmentIndicator).ToList());
                    break;
                default:
                    newList = new ObservableCollection<ItemDeal>(DisplayedDeals.OrderBy(item => item.product.Min_Price).ToList());
                    break;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DisplayedDeals = newList;
            });
            return;
        }
        enum DealsFilterType 
        {
            PriceAsc = 0,
            PriceDesc = 1,
            Newest = 2,
            DesiredDeals = 3,
            QualityAsc = 4,
            QualityDesc = 5,
            Name = 6,
            InvestmentValueAsc = 7,
            InvestmentValueDesc = 8,
            LtiiAsc = 9,
            LtiiDesc = 10
        }
    }
}
