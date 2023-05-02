using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SkinHound
{
    public class ProductMarketHistory
    {
        private const double DESIRED_PROFIT_PERCENTAGE = 0.05;
        public string Market_Hash_Name { get; set; }
        public string Currency { get; set; }
        public string Version { get; set; }
        public string Market_page { get; set; }
        public string Item_page { get; set; }
        public Last90Days Last_90_days { get; set; }
        public Last30days Last_30_days { get; set; }
        public Last7Days Last_7_days { get; set; }
        public Last24Hours Last_24_hours { get; set; }
        public async Task<double> GetLongTermPercentageProfit(Product product)
        {
            try
            {
                double movingMedian = await GetLongMovingMedian();
                //Although 4 might seem like a random value, it isn't. We are calculating what the 4th month profit/loss should be.
                double marketValueEstimate = (1- movingMedian / (double)product.Suggested_Price)/4 * 100;
                return Math.Round(marketValueEstimate, 1); ;
            }
            catch (Exception e)
            {
                return 0.0;
            }
        }
        public async Task<double> GetImmediateResellDiscount(Product product)
        {
            try
            {
                double marketValueEstimatePercentage = 1 - await GetShortMovingMedian() / (double)product.Suggested_Price;
                double calculatedPercentage = (marketValueEstimatePercentage - DESIRED_PROFIT_PERCENTAGE) * 100;
                return Math.Round(calculatedPercentage, 1); ;
            }
            catch (Exception e)
            {
                    return 0.0;
            }
        }
        public async Task<double> GetLongMovingMedian()
        {
            if (Last_90_days.Volume == 0 || Last_30_days.Volume == 0)
                return 0.0;
            if (Last_90_days.Median != 0 && Last_30_days.Median != 0)
            { 
                double movingMedian = ((double)Last_90_days.Median * 2) / 3 + (double)Last_30_days.Median / 3; 
                return Math.Round(movingMedian, 2); 
            }
            return 0.0;
        }
        public async Task<double> GetShortMovingMedian()
        {
            if (Last_7_days.Volume == 0 || Last_30_days.Volume == 0)
                return 0.0;
            if (Last_7_days.Median != 0 && Last_30_days.Median != 0)
            {
                double movingMedian = ((double)Last_30_days.Median * 3) / 4 + (double)Last_7_days.Median / 4;
                return Math.Round(movingMedian, 2);
            }
            return 0.0;
        }
    }
    public class Last24Hours
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Avg { get; set; }
        public decimal Median { get; set; }
        public int Volume { get; set; }
    }
    public class Last30days
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Avg { get; set; }
        public decimal Median { get; set; }
        public int Volume { get; set; }
    }
    public class Last7Days
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Avg { get; set; }

        public decimal Median { get; set; }
        public int Volume { get; set; }
    }
    public class Last90Days
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Avg { get; set; }

        public decimal Median { get; set; }
        public int Volume { get; set; }
    }
}