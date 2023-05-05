using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinHound
{
    public class GlobalMarketDataObject
    {
        public SteamHistory Steam { get; set; }
        public Buff163History Buff163 { get; set; }
    }
    public class SteamHistory
    {
        public double Last_24h { get; set; }
        public double Last_7d { get; set; }
        public double Last_30d { get; set; }
        public double Last_90d { get; set; }
    }
    public class Buff163History
    {
        public double Starting_At { get; set; }
        public double Highest_Order { get; set; }
    }
}
