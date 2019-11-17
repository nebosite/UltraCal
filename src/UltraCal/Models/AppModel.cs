using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace UltraCal
{
    public class AppModel : BaseModel
    {
        public const string LAYOUT = "__LAYOUT__";
        public int DPI => 96;
        public int DocumentPixelWidth => (int)(DPI * DocumentWidthInches);
        public int DocumentPixelHeight => DPI * DocumentHeightInches;
        public int DocumentWidthInches => 18;
        public int DocumentHeightInches => 24;
        public Transform DocumentLayoutTransform => new ScaleTransform(.5, .5);


        /// <summary>
        /// Start Date
        /// </summary>
        private DateTime _startDate = new DateTime(DateTime.Now.Year + 1, 1, 1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                RaisePropertyChanged(nameof(StartDate));
            }
        }

        /// <summary>
        /// End Date
        /// </summary>
        private DateTime _endDate = new DateTime(DateTime.Now.Year + 1, 12, 1);
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                RaisePropertyChanged(nameof(EndDate));
            }
        }

        public FontFamily TitleFontFamily { get; set; }
        public FontFamily NumberFontFamily { get; set; }
        public double DateNumberHeightFraction => .12;

        private Dictionary<int, Dictionary<int, Holiday>> _holidayLookup;
        private readonly string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UltraCal");

        //-------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //-------------------------------------------------------------------------------
        public AppModel()
        {

            foreach (FontFamily fontFamily in Fonts.GetFontFamilies(@"C:\Windows\fonts"))
            {
                // Add the font family name to the fonts combo box.
                System.Diagnostics.Debug.WriteLine(fontFamily.Source);
                if(fontFamily.Source.Contains("fuego"))
                {
                    TitleFontFamily = fontFamily;
                }
            }
            NumberFontFamily = new FontFamily("Lato Light");
            RaisePropertyChanged(LAYOUT);
        }

        //-------------------------------------------------------------------------------
        /// <summary>
        /// Get Holiday information for the specified date.  Returns null if there is
        /// no holiday.
        /// </summary>
        //-------------------------------------------------------------------------------
        public Holiday GetHoliday(DateTime date)
        {
            if(_holidayLookup == null)
            {
                _holidayLookup = new Dictionary<int, Dictionary<int, Holiday>>();
            }

            if(!_holidayLookup.ContainsKey(date.Year))
            {
                PopulateLookup(date.Year);
            }

            _holidayLookup[date.Year].TryGetValue(date.DayOfYear, out var holiday);
            return holiday;
        }

        //-------------------------------------------------------------------------------
        /// <summary>
        /// Fill lookup data with info for the specific year
        /// </summary>
        //-------------------------------------------------------------------------------
        private void PopulateLookup(int year)
        {
            if(!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            var dataFileName = Path.Combine(DataPath, $"{year}_Holidays.json");

            string holidayJson = null;
            if(!File.Exists(dataFileName))
            {
                holidayJson = QueryHolidayServer(year);
                if (!string.IsNullOrWhiteSpace(holidayJson))
                {
                    File.WriteAllText(dataFileName, holidayJson);
                }
            }
            else
            {
                holidayJson = File.ReadAllText(dataFileName);
            }

            var lookup = new Dictionary<int, Holiday>();
            _holidayLookup.Add(year, lookup);
            if(!string.IsNullOrWhiteSpace( holidayJson))
            {
                var data = JsonConvert.DeserializeObject<HolidayApiPacket>(holidayJson);
                foreach(var holiday in data.response.holidays)
                {
                    var type = string.Join(";", holiday.type);
                    if(type == "Clock change/Daylight Saving Time"
                        || type == "National holiday"
                        || type == "National holiday;Christian"
                        || type == "Observance;Christian"
                        || type == "Season"
                        )
                    {
                        var date = DateTime.Parse(holiday.date.iso);
                        if(lookup.ContainsKey(date.DayOfYear))
                        {
                            Debug.WriteLine("Losing this holiday: " + holiday.name);
                        }
                        lookup[date.DayOfYear] =holiday;
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------
        /// <summary>
        /// Do a REST call to get the data we need
        /// </summary>
        //-------------------------------------------------------------------------------
        private string QueryHolidayServer(int year)
        {
            var template = "https://calendarific.com/api/v2/json?api_key=57b5efbe22bb22a85c15dd4f1120562d7233a001&country={0}&year={1}";
            var baseUri = string.Format(template, "US", year);
            return RestCall(baseUri);
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Make a rest call and return the result
        /// </summary>
        //------------------------------------------------------------------------------
        internal string RestCall(string uri)
        {
            var request = WebRequest.Create(uri) as HttpWebRequest;
            string result = null;
            try
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    result = reader.ReadToEnd();
                }
                return result;
            }
            catch (WebException e)
            {
                StreamReader reader = new StreamReader(e.Response.GetResponseStream());
                result = reader.ReadToEnd();
                Debug.WriteLine($"WEB ERROR: ({e.Status}) {e.Message}: {result}");
                return null;
            }
        }
    }
}
