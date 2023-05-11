using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkinHound
{
    public class SkinHoundConfiguration
    {
        //The list of desired weapons, which will bypass the default discount.
        public List<string> Desired_Weapons { get; set; }
        //This value is used to bypass 'Great discounts' or 'Oustanding discounts' and allows us to show items we've listed as desired in the listOfDesiredWeapons.
        public double Desired_Weapons_Min_Discount_Threshold { get; set; }
        //The minimum discount which needs to be applied for the offer to be shown.
        public double Good_Discount_Threshold { get; set; }
        //Great discounts are usually discounts which you can make a decent profit off of.
        public double Great_Discount_Threshold { get; set; }
        //Outstanding discounts are discounts that rarely happen. 
        public double Outstanding_Discount_Threshold { get; set; }
        //Any item with a value lower than the min desired price will not be shown even if they have a great discount.
        public decimal Minimum_Worth_Value { get; set; }
        //This var dictates at which interval (in minutes) we make calls to Skinport to refresh the store.
        public int Minutes_Between_Queries { get; set; }
        //Having this set to true will make it so that you will receive notifications for all desired weapons above or equal the Desired_Weapons_Min_Discount_Threshold.
        public bool Notify_On_All_Desired_Weapons { get; set; }
        //Are Windows notifications enabled or not
        public bool Notifications_Enabled { get; set; }
        //In what currency should the prices be shown?
        public string Currency { get; set; }
    }
}
