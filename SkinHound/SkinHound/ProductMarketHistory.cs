using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace SkinportAnalyzer
{
    public class ProductMarketHistory
    {
        private const double DESIRED_PROFIT_PERCENTAGE = 0.05;
        public List<Sale> Sales { get; set; }
        public Last30days Last_30_days { get; set; }
        public Last7Days Last_7_days { get; set; }
        public async Task<double> GetRecommendedResellDiscount(Product product)
        {
            if (Last_30_days.Avg == 0)
                return 0.0;
            else
            {
                try
                {
                    double calculatedPercentage = await GetMedian() / (double)product.Suggested_Price;
                    double marketValueEstimate = 0;
                    if (Last_30_days.Avg != 0 && Last_7_days.Avg != 0)
                        marketValueEstimate = (double)Last_30_days.Avg / (double)Last_7_days.Avg / 10;
                    calculatedPercentage = 1 - calculatedPercentage + marketValueEstimate - DESIRED_PROFIT_PERCENTAGE;
                    calculatedPercentage *= 100;
                    return Math.Round(calculatedPercentage, 1); ;
                }
                catch (Exception e)
                {
                    return 0.0;
                }
            }
        }
        public async Task<double> GetMedian()
        {
            DeclusterSales();
            if (Sales.Count == 0)
                return 0.0;
            Sales.Sort((x, y) => x.Price.CompareTo(y.Price));
            if (Sales.Count % 2 == 0)
                return Math.Round(((double)Sales.ElementAt(Sales.Count / 2).Price + (double)Sales.ElementAt(Sales.Count / 2 - 1).Price) / 2, 2);
            else return (double)Sales.ElementAt(Sales.Count / 2).Price;
        }
        public void DeclusterSales(int x = 0)
        {
            if (Sales.Count > x)
                if (Sales.ElementAt(x).Wear_Value < 0.85 || Sales.ElementAt(x).Wear_Value > 0.02)
                {
                    DeclusterSales(x + 1);
                }
                else
                {
                    Sales.RemoveAt(x);
                    DeclusterSales(x);
                }
            else return;
        }
    }
    public class Sale
    {
        public decimal Price { get; set; }
        public float Wear_Value { get; set; }
        public int Sold_At { get; set; }
    }
    public class Last30days
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Avg { get; set; }
        public int Volume { get; set; }
    }
    public class Last7Days
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Avg { get; set; }
        public int Volume { get; set; }
    }
}