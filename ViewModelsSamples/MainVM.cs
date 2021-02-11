﻿using LiveChartsCore;
using LiveChartsCore.SkiaSharp;
using LiveChartsCore.SkiaSharp.Drawing;
using LiveChartsCore.SkiaSharp.Painting;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ViewModelsSamples
{
    public class MainVM
    {
        public ObservableCollection<ISeries<SkiaDrawingContext>> Series { get; set; }
        public List<IAxis<SkiaDrawingContext>> YAxes { get; set; }
        public List<IAxis<SkiaDrawingContext>> XAxes { get; set; }

        public MainVM()
        {
            Series = new ObservableCollection<ISeries<SkiaDrawingContext>>
            {
                new LineSeries<double>
                {
                    Name = "lineas",
                    Values = new[]{ 1d, 4, 2, 1, 7, 3, 5, 6, 3, 6, 8, 3},
                    Stroke = new SolidColorPaintTask(new SKColor(2, 136, 209), 3),
                    Fill = new SolidColorPaintTask(new SKColor(2, 136, 209, 50), 3),
                    HighlightFill = new SolidColorPaintTask(new SKColor(2, 136, 209), 3),
                    HighlightStroke = new SolidColorPaintTask(new SKColor(20, 20, 20), 3)
                },
                new ColumnSeries<double>
                {
                    Name = "columnas",
                    Values =  new[]{ 10d, -4, 2, -1, 7, -3, 5, -6, 3, -6, 8, -3},
                    //Stroke = new SolidColorPaintTask(new SKColor(217, 47, 47), 3),
                    Fill = new SolidColorPaintTask(new SKColor(217, 47, 47, 30)),
                    HighlightFill = new SolidColorPaintTask(new SKColor(217, 47, 47, 80)),
                }
            };

            YAxes = new List<IAxis<SkiaDrawingContext>>
            {
                new Axis
                {
                    TextBrush = new TextPaintTask(new SKColor(90,90,90), 25),
                    SeparatorsBrush = new SolidColorPaintTask(new SKColor(180, 180, 180)),
                    LabelsRotation = 0
                }
            };

            XAxes = new List<IAxis<SkiaDrawingContext>>
            {
                new Axis
                {
                    TextBrush = new TextPaintTask(new SKColor(90,90,90), 25),
                    SeparatorsBrush = new SolidColorPaintTask(new SKColor(180, 180, 180)),
                    LabelsRotation = 0,
                    Labeler = (value, tick) => $"this {value}"
                }
            };
        }
    }

    public class HelloGeometry : SVGPathGeometry
    {
        // This SVG path was taken from MS docs
        // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/curves/path-data

        private static readonly SKPath helloPath = SKPath.ParseSvgPathData(
                "M 0 0 L 0 100 M 0 50 L 50 50 M 50 0 L 50 100" +                // H
                "M 125 0 C 60 -10, 60 60, 125 50, 60 40, 60 110, 125 100" +     // E
                "M 150 0 L 150 100, 200 100" +                                  // L
                "M 225 0 L 225 100, 275 100" +                                  // L
                "M 300 50 A 25 50 0 1 0 300 49.9 Z");                           // O

        public HelloGeometry() 
            : base(helloPath) // We pass the already parsed SVG path, this way it is not parsed for every shape.
        {
            // alternatively we could use the SVG property.
            // but then the SVGPathGeometryClass would require to parse the SVG for each instance.
            // SVG = "M 0 0 L 0 100 M 0 50 L 50 50 M 50 0 L 50 100" +                // H
            //       "M 125 0 C 60 -10, 60 60, 125 50, 60 40, 60 110, 125 100" +     // E
            //       "M 150 0 L 150 100, 200 100" +                                  // L
            //       "M 225 0 L 225 100, 275 100" +                                  // L
            //       "M 300 50 A 25 50 0 1 0 300 49.9 Z";                            // O
        }
    }

    public class HelloColumnSeries : ColumnSeries<double, HelloGeometry>
    {

    }
}