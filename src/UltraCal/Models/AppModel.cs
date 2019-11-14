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
    public enum CardUnits
    {
        Millimeters,
        Centimeters,
        Inches,
    }

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
        /// AvailableUnits
        /// </summary>
        public CardUnits[] AvailableUnits => Enum.GetValues(typeof(CardUnits)).Cast<CardUnits>().ToArray();
        private CardUnits _selectedUnits = CardUnits.Inches;
        public CardUnits SelectedUnits
        {
            get => _selectedUnits;
            set
            {
                var oldUnits = SelectedUnits;
                _selectedUnits = value;
                _cardWidth = ConvertMeasure(CardWidth, oldUnits, SelectedUnits);
                _cardHeight = ConvertMeasure(CardHeight, oldUnits, SelectedUnits);
                _overDraw = ConvertMeasure(OverDraw, oldUnits, SelectedUnits);
                _minMargin = ConvertMeasure(MinMargin, oldUnits, SelectedUnits);
                RaisePropertyChanged(nameof(SelectedUnits));
                RaisePropertyChanged(nameof(CardHeight));
                RaisePropertyChanged(nameof(CardWidth));
                RaisePropertyChanged(nameof(OverDraw));
                RaisePropertyChanged(nameof(MinMargin));
                RaisePropertyChanged(LAYOUT);
            }
        }

        /// <summary>
        /// Card Width
        /// </summary>
        private double _cardWidth = 2.5;
        public double CardWidth
        {
            get => _cardWidth;
            set
            {
                _cardWidth = value;
                RaisePropertyChanged(nameof(CardWidth));
                RaisePropertyChanged(LAYOUT);
            }
        }

        /// <summary>
        /// Card Height
        /// </summary>
        private double _cardHeight = 3.5;
        public double CardHeight
        {
            get => _cardHeight;
            set
            {
                _cardHeight = value;
                RaisePropertyChanged(nameof(CardHeight));
                RaisePropertyChanged(LAYOUT);
            }
        }


        /// <summary>
        /// OverDraw
        /// </summary>
        private double _overDraw = 0.06;
        public double OverDraw
        {
            get => _overDraw;
            set
            {
                _overDraw = value;
                RaisePropertyChanged(nameof(OverDraw));
                RaisePropertyChanged(LAYOUT);
            }
        }


        /// <summary>
        /// MinMargin
        /// </summary>
        private double _minMargin = 0.25;
        public double MinMargin
        {
            get => _minMargin;
            set
            {
                _minMargin = value;
                RaisePropertyChanged(nameof(MinMargin));
                RaisePropertyChanged(LAYOUT);
            }
        }

        public FontFamily TitleFontFamily { get; set; }
        public FontFamily NumberFontFamily { get; set; }
        public double DateNumberHeightFraction => .14;

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
                        lookup.Add(date.DayOfYear, holiday);
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

        //-------------------------------------------------------------------------------
        /// <summary>
        /// Convert a number from old units to new units
        /// </summary>
        //-------------------------------------------------------------------------------
        public double ConvertMeasure(double number, CardUnits oldUnits, CardUnits newUnits)
        {
            double millimeters;
            switch(oldUnits)
            {
                case CardUnits.Millimeters: millimeters = number; break;
                case CardUnits.Centimeters: millimeters = number * 10.0; break;
                case CardUnits.Inches: millimeters = number / 0.0393701; break;
                default: millimeters = 0; break;
            }

            double newValue = 0;
            switch (newUnits)
            {
                case CardUnits.Millimeters: newValue = millimeters; break;
                case CardUnits.Centimeters: newValue = millimeters / 10.0; break;
                case CardUnits.Inches: newValue = millimeters * 0.0393701; break;
                default: newValue = 0; break;
            }

            // round to the 3rd decimal place
            return Math.Round(newValue * 10000) / 10000.0;
        }
    }
}
