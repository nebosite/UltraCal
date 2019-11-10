using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
