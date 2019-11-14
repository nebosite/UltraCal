using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltraCal
{
    public class HolidayResponse
    {
        public Holiday[] holidays { get; set; }
    }

    public class HolidayApiPacket
    {
        public Dictionary<string, string> meta { get; set; }
        public HolidayResponse response { get; set; }
    }

    public class HolidayState
    {
        public long id { get; set; }
        public string abbrev { get; set; }
        public string name { get; set; }
        public string exception { get; set; }
        public string iso { get; set; }
    }

    public class HolidayDateDetails
    {
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
    }
    public class HolidayDate
    {
        public string iso { get; set; }
        public HolidayDateDetails datetime { get; set; }
    }

    public class Holiday
    {
        public string name { get; set; }
        public string description { get; set; }
        public HolidayDate  date { get; set; }
        public string[] type { get; set; }
        public string locations { get; set; }
        //public HolidayState[] states { get; set; }
    }
}
