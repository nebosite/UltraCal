using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace UltraCal
{
    //-----------------------------------------------------------------------------
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    //-----------------------------------------------------------------------------
    public partial class MainWindow : Window
    {
        AppModel _model = new AppModel();
        //-----------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        //-----------------------------------------------------------------------------
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _model;
            _model.PropertyChanged += _model_PropertyChanged;

            var me = Assembly.GetExecutingAssembly();
            var resouceName = me
                .GetManifestResourceNames()
                .Where(n => n.Contains("2019Holidays"))
                .FirstOrDefault();

            var foo = _model.GetHoliday(DateTime.Now);
        }

        bool _first = true;
        //-----------------------------------------------------------------------------
        /// <summary>
        /// On the first run, render the layout so we see a blank page
        /// </summary>
        //-----------------------------------------------------------------------------
        protected override void OnContentRendered(EventArgs e)
        {
            if(_first)
            {
                _first = false;
                RecalculateLayout();
                PageContainer.FitToHeight();
            }
            base.OnContentRendered(e);
        }

        //-----------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        //-----------------------------------------------------------------------------
        private void _model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //if(e.PropertyName == AppModel.LAYOUT)
            {
                RecalculateLayout();
            }
        }

        List<Visual> _printablePages = new List<Visual>();

        //-----------------------------------------------------------------------------
        /// <summary>
        /// Regenerate pages
        /// </summary>
        //-----------------------------------------------------------------------------
        private void RecalculateLayout()
        {
            double pageMargin = .5 * _model.DPI;
            double pageTopAreaHeight = 4 * _model.DPI;

            int boxesPerRow = 7;
            int rowsPerPage = 5;

            if (rowsPerPage * boxesPerRow == 0) return;

            double dateBoxWidth = (_model.DocumentPixelWidth - pageMargin * 2) / boxesPerRow;
            double dateBoxHeight = (_model.DocumentPixelHeight - pageTopAreaHeight - pageMargin * 2) / rowsPerPage;

            _printablePages.Clear();

            var year = _model.StartDate.Year;
            var month = _model.StartDate.Month;
            var endYear = _model.EndDate.Year;
            var endMonth = _model.EndDate.Month;

            while(true)
            {
                var titleWidth = _model.DocumentWidthInches * _model.DPI - pageMargin * 2;
                var pageCanvas = new Canvas()
                {
                    Background = Brushes.White,
                    ClipToBounds = true,
                };
                int y = 0;

                var date = new DateTime(year, month, 1);
                var x = (int)date.DayOfWeek;
                int day = 1;
                var daysInMonth = DateTime.DaysInMonth(year, month);
                var rowCount = (daysInMonth - (7 - x)) / 7.0;
                if (rowCount > 4)
                {
                    y = -1;
                    titleWidth -= (dateBoxWidth * (7 - x));
                }

                var noteBackgroundBrush = new SolidColorBrush(Color.FromRgb(242, 242, 242));

                // Draw a note taking box if there is room at the top
                if (x > 0 && y > -1)
                {
                    var noteBox = CreateDateBox(x * dateBoxWidth, dateBoxHeight, null);
                    noteBox.Background = noteBackgroundBrush;
                    pageCanvas.Children.Add(noteBox);
                    Canvas.SetTop(noteBox, pageMargin + pageTopAreaHeight);
                    Canvas.SetLeft(noteBox, pageMargin);
                }

                var daylabels = new List<Label>();

                for (; y < rowsPerPage; y++)
                {
                    for (; x < boxesPerRow; x++)
                    {
                        var thisDate = date.AddDays(day - 1);
                        var dateBox = CreateDateBox(dateBoxWidth, dateBoxHeight, thisDate);

                        Canvas.SetTop(dateBox, y * dateBoxHeight + pageMargin + pageTopAreaHeight);
                        Canvas.SetLeft(dateBox, x * dateBoxWidth + pageMargin);
                        pageCanvas.Children.Add(dateBox);
                        
                        if (day >= daysInMonth)
                        {
                            int boxesRemaining = boxesPerRow - x - 1;
                            // draw a note taking box if there is room at the bottom
                            if (boxesRemaining > 0)
                            {
                                var noteBox = CreateDateBox((boxesPerRow - x - 1) * dateBoxWidth, dateBoxHeight, null);
                                noteBox.Background = noteBackgroundBrush;
                                pageCanvas.Children.Insert(0, noteBox);
                                Canvas.SetTop(noteBox, pageMargin + y * dateBoxHeight + pageTopAreaHeight);
                                Canvas.SetLeft(noteBox, pageMargin + (x + 1) * dateBoxWidth);
                            }

                            // Draw Next month mini box
                            var monthBox = CreateMonthbox(dateBoxWidth, dateBoxHeight, date.AddMonths(1));
                            pageCanvas.Children.Add( monthBox);
                            if (boxesRemaining > 0)
                            {
                                Canvas.SetTop(monthBox, pageMargin + y * dateBoxHeight + pageTopAreaHeight);
                                Canvas.SetLeft(monthBox, pageMargin + 6 * dateBoxWidth);
                            }
                            else
                            {
                                Canvas.SetTop(monthBox, pageMargin + pageTopAreaHeight);
                                Canvas.SetLeft(monthBox, pageMargin + 1 * dateBoxWidth);
                            }

                            // Draw Previous month mini box
                            monthBox = CreateMonthbox(dateBoxWidth, dateBoxHeight, date.AddMonths(-1));
                            pageCanvas.Children.Add( monthBox);
                            if (boxesRemaining > 1)
                            {
                                Canvas.SetTop(monthBox, pageMargin + y * dateBoxHeight + pageTopAreaHeight);
                                Canvas.SetLeft(monthBox, pageMargin + 5 * dateBoxWidth);
                            }
                            else
                            {
                                Canvas.SetTop(monthBox, pageMargin + pageTopAreaHeight);
                                Canvas.SetLeft(monthBox, pageMargin);
                            }

                            break;
                        }

                        // Draw day labels
                        if (day <= 7)
                        {
                            var dayName = thisDate.ToString("dddd").ToLower();
                            var dayHeight = dateBoxHeight * .3;

                            var dayLabel = new Label()
                            {
                                Foreground = Brushes.Black,
                                //Background = Brushes.Cyan,
                                Content = dayName,
                                FontFamily = _model.TitleFontFamily,
                                FontSize = (dayHeight / _model.DPI) * .8 * 72,
                                //FontWeight = FontWeights.SemiBold,
                                Width = dateBoxWidth,
                                Height = dayHeight,
                                VerticalContentAlignment = VerticalAlignment.Bottom,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                            };

                            var dayY = y;
                            if (dayY > 0) dayY = 0;
                            daylabels.Add(dayLabel);
                            Canvas.SetTop(dayLabel, pageMargin + dateBoxHeight * dayY + pageTopAreaHeight - dayHeight * .85);
                            Canvas.SetLeft(dayLabel, pageMargin + dateBoxWidth * x);
                        }
                        day++;


                    }
                    x = 0;
                }

                daylabels.ForEach(l => pageCanvas.Children.Add(l));

                var titleLabel = new Label()
                {
                    Foreground = Brushes.Black,
                    //Background = Brushes.Red,
                    Content = "  " + date.ToString("MMMM").ToLower() + "  ",
                    FontFamily = _model.TitleFontFamily,
                    FontSize = (dateBoxHeight / _model.DPI) * 1.5 * 72,
                    // FontWeight = FontWeights.UltraBold,
                    Width = titleWidth,
                    Height = dateBoxHeight * 2,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, -3.5 * _model.DPI, 0, 0)
                };
                titleLabel.RenderTransform = new ScaleTransform(1, 1.3);

                pageCanvas.Children.Add(titleLabel);
                Canvas.SetTop(titleLabel, pageMargin);
                Canvas.SetLeft(titleLabel, pageMargin);

                var yearLabel = new Label()
                {
                    Foreground = Brushes.Black,
                    //Background = Brushes.Red,
                    Content = "  " + date.ToString("yyyy") + "  ",
                    FontFamily = _model.NumberFontFamily,
                    FontSize = ((dateBoxHeight) * .25) / _model.DPI * 72,
                    FontWeight = FontWeights.UltraBold,
                    Width = titleWidth,
                    Height = dateBoxHeight * .25,
                    Padding = new Thickness(0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                };

                pageCanvas.Children.Add(yearLabel);
                Canvas.SetTop(yearLabel, pageMargin / 2);
                Canvas.SetLeft(yearLabel, pageMargin);

                var container = new Grid()
                {
                    Width = _model.DocumentPixelWidth,
                    Height = _model.DocumentPixelHeight
                };
                container.Children.Add(pageCanvas);
                _printablePages.Add(container);

                month++;
                if(month > 12)
                {
                    year++;
                    month = 1;
                }

                if (year > endYear) break;
                if (year == endYear && month > endMonth) break;
            }

 
            var pageSize = new Size(_model.DocumentPixelWidth, _model.DocumentPixelHeight);
            var document = new FixedDocument();
            document.DocumentPaginator.PageSize = pageSize;

            foreach (var xamlPage in _printablePages)
            {
                // Create FixedPage
                var fixedPage = new FixedPage();
                fixedPage.Width = pageSize.Width;
                fixedPage.Height = pageSize.Height;
                // Add visual, measure/arrange page.
                fixedPage.Children.Add((UIElement)xamlPage);
                fixedPage.Measure(pageSize);
                fixedPage.Arrange(new Rect(new Point(), pageSize));
                fixedPage.UpdateLayout();

                // Add page to document
                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                document.Pages.Add(pageContent);

            }

            PageContainer.Document = document;

        }

        //-----------------------------------------------------------------------------
        /// <summary>
        /// Create the little box that shows a Date
        /// </summary>
        //-----------------------------------------------------------------------------
        Grid CreateMonthbox(double width, double height, DateTime boxDate)
        {
            var dateBox = new Grid()
            {
                Background = Brushes.White,
                Width = width,
                Height = height,
                Tag = "MonthBox: " + boxDate.ToString()
            };

            var lineStacker = new StackPanel()
            {

            };
            dateBox.Children.Add(lineStacker);

            var columnWidth = width / 8;
            var margin = columnWidth / 2;
            var contentWidth = columnWidth * 7;
            var contentHeight = height - columnWidth;
            var rowHeight = contentHeight / 9;
            var fontSize = rowHeight * 1 / _model.DPI * 72 * .6;

            lineStacker.Margin = new Thickness(margin);

            lineStacker.Children.Add(new Label()
            {
                Foreground = Brushes.Black,
                Content = boxDate.ToString("MMMM").ToLower(),
                FontFamily = _model.NumberFontFamily,
                FontSize = fontSize * 1.5,
                FontWeight = FontWeights.UltraBold,
                Width = contentWidth,
                Height = rowHeight * 1.5,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            });

            var rowStack = new StackPanel() { Orientation = Orientation.Horizontal };
            lineStacker.Children.Add(rowStack);
            var dayLetters = "smtwtfs";
            for(int i = 0; i < 7; i++)
            {
                var littleBox = new Label()
                {
                    Content = dayLetters[i],
                    FontFamily = _model.NumberFontFamily,
                    FontSize = fontSize,
                    FontWeight = FontWeights.Bold,
                    Width = columnWidth,
                    Height = rowHeight,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                rowStack.Children.Add(littleBox);
            }
            int day = 1 - (int)boxDate.DayOfWeek;;
            int x = 0;
            rowStack = new StackPanel() { Orientation = Orientation.Horizontal };
            lineStacker.Children.Add(rowStack);
            while(true)
            {
                var littleBox = new Label()
                {
                    Content = day > 0 ? day.ToString() : " ",
                    FontFamily = _model.NumberFontFamily,
                    FontSize = fontSize,
                    Width = columnWidth,
                    Height = rowHeight,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                };
                rowStack.Children.Add(littleBox);
                day++;
                if (day > DateTime.DaysInMonth(boxDate.Year, boxDate.Month)) break;
                x++;
                if(x > 6)
                {
                    rowStack = new StackPanel() { Orientation = Orientation.Horizontal };
                    lineStacker.Children.Add(rowStack);
                    x = 0;
                }
            }

            // outside border
            dateBox.Children.Add(new Border()
            {
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                Width = width,
                Height = height
            });

            return dateBox;
        }

        //-----------------------------------------------------------------------------
        /// <summary>
        /// Create the little box that shows a Date
        /// </summary>
        //-----------------------------------------------------------------------------
        Grid CreateDateBox(double width, double height, DateTime? date = null)
        {
            var boxHeightInches = height / _model.DPI;

            var dateBox = new Grid()
            {
                Width = width,
                Height = height,
                Tag = "DateBox: " + date
            };

            if (date != null)
            {
                var phase = MoonHelper.GetMoonPhase(date.Value);
                char phaseText = (char)0;
                switch (phase)
                {
                    case LunarPhase.New: phaseText = (char)0x98; break;
                    case LunarPhase.Full: phaseText = (char)0x9A; break;
                }
                if (phaseText != (char)0)
                {
                    dateBox.Children.Add(new Label()
                    {
                        Foreground = Brushes.Gray,
                        Content = phaseText,
                        FontFamily = new FontFamily("Wingdings 2"),
                        FontSize = boxHeightInches * _model.DateNumberHeightFraction * 72,
                        FontWeight = FontWeights.Bold,
                        Width = width,
                        Height = height,
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                    });

                }
            }

            var lineStacker = new StackPanel()
            {

            };
            dateBox.Children.Add(lineStacker);

            for(int i = 0; i < 8; i++)
            {
                lineStacker.Children.Add(new Border()
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(.5),
                    Width = width,
                    Height = height / 8
                });
            }

            // outside border
            dateBox.Children.Add(new Border()
            {
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                Width = width,
                Height = height
            });

            if(date != null)
            {
                dateBox.Children.Add(new Label()
                {
                    Foreground = Brushes.Black,
                    Content = date.Value.Day,
                    FontFamily = _model.NumberFontFamily,
                    FontSize = boxHeightInches * _model.DateNumberHeightFraction * 72,
                    FontWeight = FontWeights.Bold,
                    Width = width,
                    Height = height
                });

                var smallLabelHeight = height * 0.13;
                var smallFontSize = smallLabelHeight / _model.DPI * 72 * .4;

                var holiday = _model.GetHoliday(date.Value);
                if(holiday != null)
                {

                    var smallText = new Label()
                    {
                        Content = $" {holiday.name.ToLower()} ",
                        FontFamily = _model.NumberFontFamily,
                        FontSize = smallFontSize,
                        Width = width,
                        Height = smallLabelHeight,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Bottom,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };
                    dateBox.Children.Add(smallText);
                }
            }

            return dateBox;
        }


        private static readonly Regex _numberOnlyRegex = new Regex("^[0-9.]+"); 

        //-----------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        //-----------------------------------------------------------------------------
        private void PreviewNumericInput(object sender, TextCompositionEventArgs e)
        {
            var newText = (e.Source as TextBox).Text + e.Text;
            var isValid = _numberOnlyRegex.IsMatch(e.Text) && double.TryParse(newText, out var parsedValue);
            e.Handled = !isValid;
            Debug.WriteLine($"H: {e.Handled} {newText}");
        }
    }
}
