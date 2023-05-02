using SkinHound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinHound
{
    internal class BuffMarketHistory
    {
        public BuffData Data { get; set; }
    }

    internal class BuffData
    {
        public List<BuffItem> Items { get; set; }
    }

    internal class BuffItem
    {
        public int Buy_Num { get; set; }
        public string Market_Hash_Name { get; set; }
        public float Sell_Min_Price { get; set; }
        public int Sell_Num { get; set; }
        public float Buy_Max_Price { get; set; }
        public float Sell_Reference_Price{ get; set; }
        public float Quick_Price { get; set; }
        public BuffGoodsInfo Goods_Info { get; set; }
    }
    internal class BuffGoodsInfo
    {
        public string Icon_Url { get; set; }
    }
}
