using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkinHound
{
    /// <summary>
    /// Interaction logic for ItemDeal.xaml
    /// </summary>
    public partial class PriceCheckedItem : UserControl
    {
        private ScrollViewer priceCheckScrollBar;
        public PriceCheckedItem(ScrollViewer dealScroll, Product product, string currencySymbol)
        {
            InitializeComponent();
            priceCheckScrollBar = dealScroll;
            InitializeAllValues(currencySymbol, product);
        }
        private async void InitializeAllValues(string currencySymbol, Product product) 
        {
            GlobalMarketDataObject curItemGlobalData = await CSGOTradersPricesFactory.GetItemGlobalData(product.Market_Hash_Name);
            if (curItemGlobalData == null)
                curItemGlobalData = new GlobalMarketDataObject();

            //We begin initializing the values.
            PriceCheckedXItemName.Text = product.Market_Hash_Name;
            PriceCheckedButtonX.Tag = product.Item_Page;
            PriceCheckedXSkinportDiscount.Text = $"{product.Percentage_Off}%";
            PriceCheckedXSkinportPrice.Text = $"{product.Min_Price.ToString("0.00")}{currencySymbol}";
            PriceCheckedXSkinportVolumeSoldLast30Days.Text = $"{product.productMarketHistory.Last_30_days.Volume}";
            PriceCheckedXSkinportMedianSoldLast30Days.Text = $"{product.productMarketHistory.Last_30_days.Median.ToString("0.00")}{currencySymbol}";
            PriceCheckedXBuffStartingAt.Text = $"{(curItemGlobalData.Buff163.Starting_At * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            PriceCheckedXBuffHighestOrder.Text = $"{(curItemGlobalData.Buff163.Highest_Order * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            PriceCheckedXSteamLast7Days.Text = $"{(curItemGlobalData.Steam.Last_7d * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            PriceCheckedXSteamLast30Days.Text = $"{(curItemGlobalData.Steam.Last_30d * Utils.GetCurrencyRateFromUSD(SkinHoundConfiguration.Currency)).ToString("0.00")}{currencySymbol}";
            PriceCheckedXRecommendedDiscount.Text = $"{product.recommendedDiscount}";
            PriceCheckedXRecommendedSalePrice.Text = $"{product.recommendedResellPrice}{currencySymbol}";
            PriceCheckedXProfitPOnResale.Text = $"{product.profitPercentageOnResellPrice}";
            PriceCheckedXProfitCOnResale.Text = $"{product.profitMoneyOnResellPrice}{currencySymbol}";
            PriceCheckedXLTII.Text = $"{await product.productMarketHistory.GetLongTermPercentageProfit()}%";
            PriceCheckedXMovingAverage.Text = $"{await product.productMarketHistory.GetLongMovingMedian()}{currencySymbol}";
        }
        private void PriceCheckedClicked(object sender, RoutedEventArgs e)
        {
            //The default link.
            string link = "https://skinport.com/";
            // Obtain the arguments from the button
            link = (string)PriceCheckedButtonX.Tag;
            //We attempt to start a browser to item.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                link = link.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
        }
        private bool _scrolling = true;
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (this._scrolling)
            {
                priceCheckScrollBar.ScrollToVerticalOffset(priceCheckScrollBar.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this._scrolling = false;
            e.Handled = true;
        }

        private void ScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            this._scrolling = true;
            e.Handled = true;
        }
    }
}
