using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinHound
{
    public class Product
    {
        public string Market_Hash_Name { get; set; }
        public string Currency { get; set; }
        public decimal Suggested_Price { get; set; }
        public string Item_Page { get; set; }
        public string Market_Page { get; set; }
        public decimal Min_Price { get; set; }
        public decimal Max_Price { get; set; }
        public decimal Mean_Price { get; set; }
        public decimal Median_Price { get; set; }
        public int Quantity { get; set; }
        public int Created_At { get; set; }
        public int Updated_At { get; set; }
        public float Percentage_Off { get; set; }
        public ProductMarketHistory productMarketHistory { get; set; }
        public string recommendedResellPrice { get; set; }
        public string recommendedDiscount { get; set; }
        public string profitMoneyOnResellPrice { get; set; }
        public string profitPercentageOnResellPrice { get; set; }
        public double InvestmentValue { get; set; }
        public double LongTermInvestmentIndicator { get; set; }
        public DealType dealType { get; set; }
        public bool isDesired { get; set; }
        public string imagePath { get; set; }
        public bool isNew { get; set; }
        public GlobalMarketDataObject GlobalMarketData { get; set; }
        public Product(string market_Hash_Name, string currency, decimal suggested_Price, string item_Page, string market_Page, decimal min_Price, decimal max_Price, decimal mean_Price, decimal median_Price, int quantity, int created_At, int updated_At)
        {
            Market_Hash_Name = market_Hash_Name;
            Currency = currency;
            Suggested_Price = suggested_Price;
            Item_Page = item_Page;
            Market_Page = market_Page;
            Min_Price = min_Price;
            Max_Price = max_Price;
            Mean_Price = mean_Price;
            Median_Price = median_Price;
            Quantity = quantity;
            Created_At = created_At;
            Updated_At = updated_At;
            UpdatePercentageOff();
        }
        public Product() { }
        public void UpdatePercentageOff()
        {
            try
            {
                Percentage_Off = (float)Math.Round((1 - (double)(Min_Price / Suggested_Price)) * 100, 2);
            }
            catch (Exception e)
            {
                this.Percentage_Off = 0;
            }
        }
        //This function is overriden to allow us to register the history of our notifications without having to store full objects.
        public override string ToString()
        {
            return $"{Item_Page}{Min_Price}";
        }
    }
}