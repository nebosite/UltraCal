﻿using Newtonsoft.Json;
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
            if(e.PropertyName == AppModel.LAYOUT)
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
            double pageTopArea = 4 * _model.DPI;

            int boxesPerRow = 7;
            int rowsPerPage = 5;

            if (rowsPerPage * boxesPerRow == 0) return;

            double dateBoxWidth = (_model.DocumentPixelWidth - pageMargin * 2) / boxesPerRow;
            double dateBoxHeight = (_model.DocumentPixelHeight - pageTopArea - pageMargin * 2) / rowsPerPage;

            _printablePages.Clear();
            PageContainer.Children.Clear();

            var pageCanvas = new Canvas()
            {
                Background = Brushes.White,
                ClipToBounds = true,
            };

            int y = 0;
            int x = 0;
            var titleWidth = _model.DocumentWidthInches * _model.DPI - pageMargin * 2;

            var month = 6;
            var date = new DateTime(2019, month, 1,0,0,0);
            x = (int)date.DayOfWeek;
            int day = 1;
            var daysInMonth = DateTime.DaysInMonth(2019, month);
            var rowCount = (daysInMonth - (7-x)) / 7.0;
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
                Canvas.SetTop(noteBox, pageMargin + pageTopArea);
                Canvas.SetLeft(noteBox, pageMargin);
            }

            for (; y < rowsPerPage; y++)
            {
                for (; x < boxesPerRow; x++)
                {

                    var dateBox = CreateDateBox(dateBoxWidth, dateBoxHeight, day.ToString());
                    day++;

                    Canvas.SetTop(dateBox, y * dateBoxHeight + pageMargin + pageTopArea);
                    Canvas.SetLeft(dateBox, x * dateBoxWidth + pageMargin);
                    pageCanvas.Children.Add(dateBox);
                    if (day > daysInMonth)
                    {
                        // draw a note taking box if there is room at the bottom
                        if (x < boxesPerRow - 1)
                        {
                            var noteBox = CreateDateBox((boxesPerRow - x - 1) * dateBoxWidth, dateBoxHeight, null);
                            noteBox.Background = noteBackgroundBrush;
                            pageCanvas.Children.Add(noteBox);
                            Canvas.SetTop(noteBox, pageMargin + y * dateBoxHeight + pageTopArea);
                            Canvas.SetLeft(noteBox, pageMargin + (x +1)* dateBoxWidth);
                        }

                        var monthBox = CreateMonthbox(dateBoxWidth, dateBoxHeight, date.AddMonths(1));
                        pageCanvas.Children.Add(monthBox);
                        if (x < boxesPerRow - 1)
                        {
                            Canvas.SetTop(monthBox, pageMargin + y * dateBoxHeight + pageTopArea);
                            Canvas.SetLeft(monthBox, pageMargin + 6 * dateBoxWidth);
                        }
                        else
                        {
                            Canvas.SetTop(monthBox, pageMargin + pageTopArea);
                            Canvas.SetLeft(monthBox, pageMargin + 1 * dateBoxWidth);
                        }

                        monthBox = CreateMonthbox(dateBoxWidth, dateBoxHeight, date.AddMonths(-1));
                        pageCanvas.Children.Add(monthBox);
                        if (x < boxesPerRow - 2)
                        {
                            Canvas.SetTop(monthBox, pageMargin + y * dateBoxHeight + pageTopArea);
                            Canvas.SetLeft(monthBox, pageMargin + 5 * dateBoxWidth);
                        }
                        else
                        {
                            Canvas.SetTop(monthBox, pageMargin + pageTopArea);
                            Canvas.SetLeft(monthBox, pageMargin);
                        }

                        break;
                    }
                }
                x = 0;
            }

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
                Margin = new Thickness(0, -3 * _model.DPI, 0, 0)
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
            Canvas.SetTop(yearLabel, pageMargin/2);
            Canvas.SetLeft(yearLabel, pageMargin);


            var container = new Grid()
            {
                Width = _model.DocumentPixelWidth,
                Height = _model.DocumentPixelHeight
            };
            container.Children.Add(pageCanvas);

            PageContainer.Children.Add(container);
            _printablePages.Add(pageCanvas);
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
            };

            var lineStacker = new StackPanel()
            {

            };
            dateBox.Children.Add(lineStacker);

            var columnWidth = width / 8;
            var margin = columnWidth / 2;
            var contentWidth = columnWidth * 7;
            var contentHeight = height - columnWidth;
            var rowHeight = contentHeight / 7;
            var fontSize = rowHeight * .8 / _model.DPI * 72 * .6;

            lineStacker.Margin = new Thickness(margin);

            lineStacker.Children.Add(new Label()
            {
                Foreground = Brushes.Black,
                Content = boxDate.ToString("MMMM").ToLower(),
                FontFamily = _model.NumberFontFamily,
                FontSize = fontSize * 1.5,
                Width = contentWidth,
                Height = rowHeight,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            });

            int day = 1 - (int)boxDate.DayOfWeek;;
            int x = 0;
            var rowStack = new StackPanel() { Orientation = Orientation.Horizontal };
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
        Grid CreateDateBox(double width, double height, string number)
        {
            var dateBox = new Grid()
            {
                Width = width,
                Height = height,
            };


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

            if(!string.IsNullOrEmpty(number))
            {
                var boxHeightInches = height / _model.DPI;
                dateBox.Children.Add(new Label()
                {
                    Foreground = Brushes.Black,
                    Content = number,
                    FontFamily = _model.NumberFontFamily,
                    FontSize = boxHeightInches * _model.DateNumberHeightFraction * 72,
                    FontWeight = FontWeights.Bold,
                    Width = width,
                    Height = height
                });

            }

            return dateBox;
        }

        //-----------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        //-----------------------------------------------------------------------------
        private void UpdatePreviewClick(object sender, RoutedEventArgs e)
        {
            if (_printablePages.Count == 0) return;
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(_printablePages[0], "Page of Cards");
            }
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
