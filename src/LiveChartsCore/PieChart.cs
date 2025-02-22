﻿// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using LiveChartsCore.Kernel;
using LiveChartsCore.Drawing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LiveChartsCore.Measure;

namespace LiveChartsCore
{
    /// <summary>
    /// Defines a pie chart.
    /// </summary>
    /// <typeparam name="TDrawingContext">The type of the drawing context.</typeparam>
    /// <seealso cref="Chart{TDrawingContext}" />
    public class PieChart<TDrawingContext> : Chart<TDrawingContext>
        where TDrawingContext : DrawingContext
    {
        private readonly HashSet<ISeries> _everMeasuredSeries = new();
        private readonly IPieChartView<TDrawingContext> _chartView;
        private int _nextSeries = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PieChart{TDrawingContext}"/> class.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="defaultPlatformConfig">The default platform configuration.</param>
        /// <param name="canvas">The canvas.</param>
        public PieChart(
            IPieChartView<TDrawingContext> view,
            Action<LiveChartsSettings> defaultPlatformConfig,
            MotionCanvas<TDrawingContext> canvas)
            : base(canvas, defaultPlatformConfig)
        {
            _chartView = view;

            view.PointStates.Chart = this;
            foreach (var item in view.PointStates.GetStates())
            {
                if (item.Fill != null)
                {
                    item.Fill.ZIndex += 1000000;
                    canvas.AddDrawableTask(item.Fill);
                }
                if (item.Stroke != null)
                {
                    item.Stroke.ZIndex += 1000000;
                    canvas.AddDrawableTask(item.Stroke);
                }
            }
        }

        /// <summary>
        /// Gets the series.
        /// </summary>
        /// <value>
        /// The series.
        /// </value>
        public IPieSeries<TDrawingContext>[] Series { get; private set; } = new IPieSeries<TDrawingContext>[0];

        /// <summary>
        /// Gets the drawable series.
        /// </summary>
        /// <value>
        /// The drawable series.
        /// </value>
        public override IEnumerable<IDrawableSeries<TDrawingContext>> DrawableSeries => Series;

        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <value>
        /// The view.
        /// </value>
        public override IChartView<TDrawingContext> View => _chartView;

        /// <summary>
        /// Gets the value bounds.
        /// </summary>
        /// <value>
        /// The value bounds.
        /// </value>
        public Bounds ValueBounds { get; private set; } = new Bounds();

        /// <summary>
        /// Gets the index bounds.
        /// </summary>
        /// <value>
        /// The index bounds.
        /// </value>
        public Bounds IndexBounds { get; private set; } = new Bounds();

        /// <summary>
        /// Gets the pushout bounds.
        /// </summary>
        /// <value>
        /// The pushout bounds.
        /// </value>
        public Bounds PushoutBounds { get; private set; } = new Bounds();

        /// <summary>
        /// Finds the points near to the specified point.
        /// </summary>
        /// <param name="pointerPosition">The pointer position.</param>
        /// <returns></returns>
        public override IEnumerable<TooltipPoint> FindPointsNearTo(PointF pointerPosition)
        {
            return _chartView.Series.SelectMany(series => series.FindPointsNearTo(this, pointerPosition));
        }

        /// <inheritdoc cref="IChart.Update(bool)" />
        public override void Update(bool throttling = true)
        {
            updateThrottler.Call();
        }

        /// <summary>
        /// Measures this chart.
        /// </summary>
        /// <returns></returns>
        protected override void Measure()
        {
            if (Series == null)
            {
                //chartView.CoreCanvas.ForEachGeometry((geometry, drawable) =>
                //{
                //    if (MeasuredDrawables.Contains(geometry)) return; // then the geometry was measured

                //    // at this point,the geometry is not required in the UI
                //    geometry.RemoveOnCompleted = true;
                //});
                return;
            }

            Canvas.MeasuredDrawables = new HashSet<IDrawable<TDrawingContext>>();
            seriesContext = new SeriesContext<TDrawingContext>(Series);

            if (legend != null) legend.Draw(this);

            var theme = LiveCharts.CurrentSettings.GetTheme<TDrawingContext>();
            if (theme.CurrentColors == null || theme.CurrentColors.Length == 0)
                throw new Exception("Default colors are not valid");

            ValueBounds = new Bounds();
            IndexBounds = new Bounds();
            PushoutBounds = new Bounds();
            foreach (var series in Series)
            {
                if (series.SeriesId == -1) series.SeriesId = _nextSeries++;
                theme.ResolveSeriesDefaults(theme.CurrentColors, series);

                var seriesBounds = series.GetBounds(this);

                _ = ValueBounds.AppendValue(seriesBounds.PrimaryBounds.max);
                _ = ValueBounds.AppendValue(seriesBounds.PrimaryBounds.min);
                _ = IndexBounds.AppendValue(seriesBounds.SecondaryBounds.max);
                _ = IndexBounds.AppendValue(seriesBounds.SecondaryBounds.min);
                _ = PushoutBounds.AppendValue(seriesBounds.TertiaryBounds.max);
                _ = PushoutBounds.AppendValue(seriesBounds.TertiaryBounds.min);
            }

            if (viewDrawMargin == null)
            {
                var m = viewDrawMargin ?? new Margin();
                SetDrawMargin(controlSize, m);
                SetDrawMargin(controlSize, m);
            }

            // invalid dimensions, probably the chart is too small
            // or it is initializing in the UI and has no dimensions yet
            if (drawMarginSize.Width <= 0 || drawMarginSize.Height <= 0) return;

            var toDeleteSeries = new HashSet<ISeries>(_everMeasuredSeries);
            foreach (var series in Series)
            {
                series.Measure(this);
                _ = _everMeasuredSeries.Add(series);
                _ = toDeleteSeries.Remove(series);

                var deleted = false;
                foreach (var item in series.DeletingTasks)
                {
                    canvas.RemovePaintTask(item);
                    item.Dispose();
                    deleted = true;
                }
                if (deleted) series.DeletingTasks.Clear();
            }

            foreach (var series in toDeleteSeries) { series.Delete(View); _ = _everMeasuredSeries.Remove(series); }

            //chartView.CoreCanvas.ForEachGeometry((geometry, drawable) =>
            //{
            //    if (measuredDrawables.Contains(geometry)) return; // then the geometry was measured

            //    // at this point,the geometry is not required in the UI
            //    geometry.RemoveOnCompleted = true;
            //});

            Canvas.Invalidate();
        }

        /// <summary>
        /// Called when the updated the throttler is unlocked.
        /// </summary>
        /// <returns></returns>
        protected override void UpdateThrottlerUnlocked()
        {
            // before measure every element in the chart
            // we copy the properties that might change while we are updating the chart
            // this call should be thread safe
            // ToDo: ensure it is thread safe...

            viewDrawMargin = _chartView.DrawMargin;
            controlSize = _chartView.ControlSize;

            Series = _chartView.Series
                .Where(x => x.IsVisible)
                .Cast<IPieSeries<TDrawingContext>>()
                .Select(series =>
                {
                    // a good implementation of ISeries<T>
                    // must use the measureWorker to identify
                    // if the points are already fetched.

                    // this way no matter if the Series.Values collection changes
                    // the fetch method will always return the same collection for the
                    // current measureWorker instance

                    _ = series.Fetch(this);
                    return series;
                }).ToArray();

            legendPosition = _chartView.LegendPosition;
            legendOrientation = _chartView.LegendOrientation;
            legend = _chartView.Legend; // ... this is a reference type.. this has no sense

            tooltipPosition = _chartView.TooltipPosition;
            tooltipFindingStrategy = _chartView.TooltipFindingStrategy;
            tooltip = _chartView.Tooltip; //... no sense again...

            animationsSpeed = _chartView.AnimationsSpeed;
            easingFunction = _chartView.EasingFunction;

            Measure();
        }
    }
}
