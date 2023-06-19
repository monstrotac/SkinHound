using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinHound
{
    public class SaleFeedActivity
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Image { get; set; }
        public string Color { get; set; }
        public string RarityColor { get; set; }
        public string Version { get; set; }
        public double SalePrice { get; set; }
        public double SuggestedPrice { get; set; }
        public int Pattern { get; set; }
        public string SaleType { get; set; }
        public string Exterior { get; set; }
        public string MarketHashName { get; set; }
        public string EventType { get; set; }
        public string Link { get; set; }
        public string Collection { get; set; }
        public string Url { get; set; }
        public float Wear { get; set; }
        public int SaleId { get; set; }
        public bool IsDesired { get; set; }
        public DateTime ActivityTime { get; set; }
    }
}
