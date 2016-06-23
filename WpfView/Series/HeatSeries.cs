﻿//The MIT License(MIT)

//Copyright(c) 2016 Alberto Rodriguez

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveCharts.Helpers;
using LiveCharts.SeriesAlgorithms;
using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf.Points;

// ReSharper disable once CheckNamespace
namespace LiveCharts.Wpf
{
    public class HeatSeries : Series.Series, IHeatSeries
    {
        #region Constructors

        public HeatSeries()
        {
            Model = new HeatAlgorithm(this);
            InitializeDefuaults();
        }

        public HeatSeries(object configuration)
        {
            Model = new HeatAlgorithm(this);
            Configuration = configuration;
            InitializeDefuaults();
        }

        #endregion

        #region Private Properties

        private HeatColorRange ColorRangeControl { get; set; }
        #endregion

        #region Properties

        public static readonly DependencyProperty DrawsHeatRangeProperty = DependencyProperty.Register(
            "DrawsHeatRange", typeof(bool), typeof(HeatSeries),
            new PropertyMetadata(default(bool), CallChartUpdater()));

        public bool DrawsHeatRange
        {
            get { return (bool)GetValue(DrawsHeatRangeProperty); }
            set { SetValue(DrawsHeatRangeProperty, value); }
        }

        public static readonly DependencyProperty GradientStopCollectionProperty = DependencyProperty.Register(
            "GradientStopCollection", typeof(GradientStopCollection), typeof(HeatSeries), new PropertyMetadata(default(GradientStopCollection)));

        public GradientStopCollection GradientStopCollection
        {
            get { return (GradientStopCollection)GetValue(GradientStopCollectionProperty); }
            set { SetValue(GradientStopCollectionProperty, value); }
        }

        public IList<CoreGradientStop> Stops
        {
            get
            {
                return GradientStopCollection.Select(x => new CoreGradientStop
                {
                    Offset = x.Offset,
                    Color = new CoreColor(x.Color.A, x.Color.R, x.Color.G, x.Color.B)
                }).ToArray();
            }
        }

        #endregion

        #region Overridden Methods

        public override IChartPointView GetPointView(IChartPointView view, ChartPoint point, string label)
        {
            var pbv = (view as HeatPoint);

            if (pbv == null)
            {
                pbv = new HeatPoint
                {
                    IsNew = true,
                    Rectangle = new Rectangle()
                };

                BindingOperations.SetBinding(pbv.Rectangle, Shape.StrokeProperty,
                    new Binding { Path = new PropertyPath(StrokeProperty), Source = this });
                BindingOperations.SetBinding(pbv.Rectangle, Shape.StrokeThicknessProperty,
                    new Binding { Path = new PropertyPath(StrokeThicknessProperty), Source = this });
                BindingOperations.SetBinding(pbv.Rectangle, VisibilityProperty,
                    new Binding { Path = new PropertyPath(VisibilityProperty), Source = this });
                BindingOperations.SetBinding(pbv.Rectangle, Panel.ZIndexProperty,
                    new Binding { Path = new PropertyPath(Panel.ZIndexProperty), Source = this });
                BindingOperations.SetBinding(pbv.Rectangle, Shape.StrokeDashArrayProperty,
                    new Binding { Path = new PropertyPath(StrokeDashArrayProperty), Source = this });

                Model.Chart.View.AddToDrawMargin(pbv.Rectangle);
            }
            else
            {
                pbv.IsNew = false;
                point.SeriesView.Model.Chart.View
                    .EnsureElementBelongsToCurrentDrawMargin(pbv.Rectangle);
                point.SeriesView.Model.Chart.View
                    .EnsureElementBelongsToCurrentDrawMargin(pbv.HoverShape);
                point.SeriesView.Model.Chart.View
                    .EnsureElementBelongsToCurrentDrawMargin(pbv.DataLabel);
            }

            if ((Model.Chart.View.HasTooltip || Model.Chart.View.HasDataClickEventAttached) && pbv.HoverShape == null)
            {
                pbv.HoverShape = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    StrokeThickness = 0
                };

                Panel.SetZIndex(pbv.HoverShape, int.MaxValue);
                BindingOperations.SetBinding(pbv.HoverShape, VisibilityProperty,
                    new Binding { Path = new PropertyPath(VisibilityProperty), Source = this });

                var wpfChart = Model.Chart.View as Chart;
                if (wpfChart == null) return null;

                wpfChart.AttachEventsTo(pbv.HoverShape);

                Model.Chart.View.AddToDrawMargin(pbv.HoverShape);
            }

            if (DataLabels && pbv.DataLabel == null)
            {
                pbv.DataLabel = BindATextBlock(0);
                Panel.SetZIndex(pbv.DataLabel, int.MaxValue - 1);

                Model.Chart.View.AddToDrawMargin(pbv.DataLabel);
            }

            if (pbv.DataLabel != null) pbv.DataLabel.Text = label;

            return pbv;
        }

        public override void Erase()
        {
            Values.Points.ForEach(p =>
            {
                if (p.View != null)
                    p.View.RemoveFromView(Model.Chart);
            });
            Model.Chart.View.RemoveFromView(this);
        }

        public override void DrawSpecializedElements()
        {
            if (DrawsHeatRange)
            {
                if (ColorRangeControl == null)
                {
                    ColorRangeControl = new HeatColorRange();
                    ColorRangeControl.SetBinding(TextBlock.FontFamilyProperty,
                        new Binding { Path = new PropertyPath(FontFamilyProperty), Source = this });
                    ColorRangeControl.SetBinding(FontSizeProperty,
                        new Binding { Path = new PropertyPath(FontSizeProperty), Source = this });
                    ColorRangeControl.SetBinding(TextBlock.FontStretchProperty,
                        new Binding { Path = new PropertyPath(FontStretchProperty), Source = this });
                    ColorRangeControl.SetBinding(TextBlock.FontStyleProperty,
                        new Binding { Path = new PropertyPath(FontStyleProperty), Source = this });
                    ColorRangeControl.SetBinding(TextBlock.FontWeightProperty,
                        new Binding { Path = new PropertyPath(FontWeightProperty), Source = this });
                    ColorRangeControl.SetBinding(TextBlock.ForegroundProperty,
                        new Binding { Path = new PropertyPath(ForegroundProperty), Source = this });
                    ColorRangeControl.SetBinding(VisibilityProperty,
                        new Binding { Path = new PropertyPath(VisibilityProperty), Source = this });
                }
                if (ColorRangeControl.Parent == null)
                {
                    Model.Chart.View.AddToView(ColorRangeControl);
                }
                var max = ColorRangeControl.SetMax(Values.Limit3.Max.ToString(CultureInfo.InvariantCulture));
                var min = ColorRangeControl.SetMin(Values.Limit3.Min.ToString(CultureInfo.InvariantCulture));

                var m = max > min ? max : min;

                ColorRangeControl.Width = m;

                Model.Chart.ControlSize = new CoreSize(Model.Chart.ControlSize.Width - m - 4,
                    Model.Chart.ControlSize.Height);
            }
            else
            {
                Model.Chart.View.RemoveFromView(ColorRangeControl);
            }
        }

        public override void PlaceSpecializedElements()
        {
            ColorRangeControl.UpdateFill(GradientStopCollection);

            ColorRangeControl.Height = Model.Chart.DrawMargin.Height;

            Canvas.SetTop(ColorRangeControl, Model.Chart.DrawMargin.Top);
            Canvas.SetLeft(ColorRangeControl, Model.Chart.DrawMargin.Left + Model.Chart.DrawMargin.Width + 4);
        }

        #endregion

        #region Private Methods

        private void InitializeDefuaults()
        {
            SetValue(StrokeThicknessProperty, 0d);
            SetValue(ForegroundProperty, Brushes.White);
            SetValue(StrokeProperty, Brushes.White);
            SetValue(DrawsHeatRangeProperty, true);
            SetValue(GradientStopCollectionProperty, new GradientStopCollection());

            Func<ChartPoint, string> defaultLabel = x => x.Weight.ToString(CultureInfo.InvariantCulture);
            SetValue(LabelPointProperty, defaultLabel);

            DefaultFillOpacity = 0.4;
        }

        public override void InitializeColors()
        {
            var wpfChart = (Chart)Model.Chart.View;
            var nextColor = wpfChart.GetNextDefaultColor();

            if (Stroke == null)
                SetValue(StrokeProperty, new SolidColorBrush(nextColor));
            if (Fill == null)
                SetValue(FillProperty, new SolidColorBrush(nextColor));

            var defaultColdColor = new Color
            {
                A = (byte)(nextColor.A * (DefaultFillOpacity > 1
                    ? 1
                    : (DefaultFillOpacity < 0 ? 0 : DefaultFillOpacity))),
                R = nextColor.R,
                G = nextColor.G,
                B = nextColor.B
            };

            if (!GradientStopCollection.Any())
            {
                GradientStopCollection.Add(new GradientStop
                {
                    Color = defaultColdColor,
                    Offset = 0
                });
                GradientStopCollection.Add(new GradientStop
                {
                    Color = nextColor,
                    Offset = 1
                });
            }
        }

        #endregion
    }
}