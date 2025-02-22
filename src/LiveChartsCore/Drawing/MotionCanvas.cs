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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiveChartsCore.Drawing
{
    /// <summary>
    /// Defines a canvas that is able to animate the shapes inside it.
    /// </summary>
    /// <typeparam name="TDrawingContext">The type of the drawing context.</typeparam>
    public class MotionCanvas<TDrawingContext>
        where TDrawingContext : DrawingContext
    {
        private readonly Stopwatch _stopwatch = new();
        private HashSet<IDrawableTask<TDrawingContext>> _paintTasks = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MotionCanvas{TDrawingContext}"/> class.
        /// </summary>
        public MotionCanvas()
        {
            _stopwatch.Start();
        }

        internal HashSet<IDrawable<TDrawingContext>> MeasuredDrawables { get; set; } = new();

        /// <summary>
        /// Occurs then the visual is invalidated.
        /// </summary>
        public event Action<MotionCanvas<TDrawingContext>>? Invalidated;

        /// <summary>
        /// Returns true if the visual is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets the synchronize object.
        /// </summary>
        /// <value>
        /// The synchronize.
        /// </value>
        public object Sync { get; } = new();

        /// <summary>
        /// Draws the frame.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public void DrawFrame(TDrawingContext context)
        {
            lock (Sync)
            {
                var isValid = true;
                var frameTime = _stopwatch.ElapsedMilliseconds;
                context.ClearCanvas();

                var toRemoveGeometries = new List<Tuple<IDrawableTask<TDrawingContext>, IDrawable<TDrawingContext>>>();

                foreach (var task in _paintTasks.OrderBy(x => x.ZIndex).ToArray())
                {
                    task.IsCompleted = true;
                    task.CurrentTime = frameTime;
                    task.InitializeTask(context);

                    foreach (var geometry in task.GetGeometries())
                    {
                        geometry.IsCompleted = true;
                        geometry.CurrentTime = frameTime;
                        geometry.Draw(context);

                        isValid = isValid && geometry.IsCompleted;

                        if (geometry.IsCompleted && geometry.RemoveOnCompleted)
                            toRemoveGeometries.Add(
                                new Tuple<IDrawableTask<TDrawingContext>, IDrawable<TDrawingContext>>(task, geometry));
                        //if (!MeasuredDrawables.Contains(geometry))
                        //    toRemoveGeometries.Add(
                        //        new Tuple<IDrawableTask<TDrawingContext>, IDrawable<TDrawingContext>>(task, geometry));
                    }

                    task.Dispose();

                    isValid = isValid && task.IsCompleted;
                    task.Dispose();

                    if (task.RemoveOnCompleted && task.IsCompleted) _ = _paintTasks.Remove(task);
                }

                foreach (var tuple in toRemoveGeometries)
                {
                    tuple.Item1.RemoveGeometryFromPainTask(tuple.Item2);
                    // if we removed at least one gemetry, we need to redraw the chart
                    // to ensure it is not present in the next frame
                    isValid = false;
                }

                IsValid = isValid;
            }
        }

        /// <summary>
        /// Gets the drawables count.
        /// </summary>
        /// <value>
        /// The drawables count.
        /// </value>
        public int DrawablesCount => _paintTasks.Count;

        /// <summary>
        /// Invalidates this instance.
        /// </summary>
        /// <returns></returns>
        public void Invalidate()
        {
            IsValid = false;
            Invalidated?.Invoke(this);
        }

        /// <summary>
        /// Adds a drawable task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns></returns>
        public void AddDrawableTask(IDrawableTask<TDrawingContext> task)
        {
            _ = _paintTasks.Add(task);
            Invalidate();
        }

        /// <summary>
        /// Sets the paint tasks.
        /// </summary>
        /// <param name="tasks">The tasks.</param>
        /// <returns></returns>
        public void SetPaintTasks(HashSet<IDrawableTask<TDrawingContext>> tasks)
        {
            _paintTasks = tasks;
            Invalidate();
        }

        /// <summary>
        /// Removes the paint task.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns></returns>
        public void RemovePaintTask(IDrawableTask<TDrawingContext> task)
        {
            _ = _paintTasks.Remove(task);
            Invalidate();
        }

        /// <summary>
        /// Counts the geometries.
        /// </summary>
        /// <returns></returns>
        public int CountGeometries()
        {
            var count = 0;

            foreach (var task in _paintTasks)
            {
                foreach (var geometry in task.GetGeometries())
                {
                    count++;
                }
            }

            return count;
        }
    }
}
