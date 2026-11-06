using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Linq;

namespace StegoAnalysis.Models.Algorithms
{
    public class HistogramAnalyzer
    {
        public PlotModel GenerateChiSquareHistogram(string imagePath)
        {
            var plotModel = new PlotModel {  };

            plotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside
            });

            byte[] channelData = ExtractChannel(imagePath, "R");
            long[] observed = new long[256];
            foreach (byte v in channelData) observed[v]++;

            int validPairs = 0;
            var observedFreq = new double[128];
            var expectedFreq = new double[128];
            for (int i = 0; i < 128; i++)
            {
                long even = observed[i * 2];
                long odd = observed[i * 2 + 1];
                double total = even + odd;
                if (total > 0)
                {
                    observedFreq[validPairs] = even;
                    expectedFreq[validPairs] = total / 2.0;
                    validPairs++;
                }
            }

            observedFreq = observedFreq.Take(validPairs).ToArray();
            expectedFreq = expectedFreq.Take(validPairs).ToArray();

            // Серия для наблюдаемых частот (синяя линия с кружками)
            var observedSeries = new LineSeries
            {
                Title = "Наблюдаемые частоты",
                Color = OxyColors.DodgerBlue, // более яркий синий
                StrokeThickness = 3,          // толще
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,               // крупнее
                MarkerStroke = OxyColors.Black,
                MarkerFill = OxyColors.DodgerBlue
            };
            // Серия для ожидаемых частот (красная линия с треугольниками)
            var expectedSeries = new LineSeries
            {
                Title = "Ожидаемые частоты",
                Color = OxyColors.DarkRed,    // тёмно-красный
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Dash,   // пунктирная линия
                MarkerType = MarkerType.Diamond,
                MarkerSize = 3,
                MarkerFill = OxyColors.DarkRed
            };

            for (int i = 0; i < observedFreq.Length; i++)
            {
                observedSeries.Points.Add(new DataPoint(i, observedFreq[i]));
                expectedSeries.Points.Add(new DataPoint(i, expectedFreq[i]));
            }

            plotModel.Series.Add(observedSeries);
            plotModel.Series.Add(expectedSeries);

            // Ось X 
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Номер пары",
                FontSize = 10,
                Minimum = 0,
                Maximum = observedFreq.Length - 1,
                MajorStep = Math.Max(1, observedFreq.Length / 10), 
                StringFormat = "F0" 
            });

            // Ось Y 
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Частота",
                FontSize = 10,
                LabelFormatter = x => x >= 1000 ? $"{x / 1000:F0}K" : x.ToString("F0")
            });

            return plotModel;
        }

        private byte[] ExtractChannel(string imagePath, string channel)
        {
            using var image = Image.Load<Rgba32>(imagePath);
            byte[] data = new byte[image.Width * image.Height];
            int idx = 0;
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    data[idx++] = channel switch
                    {
                        "R" => pixel.R,
                        "G" => pixel.G,
                        "B" => pixel.B,
                        _ => (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B)
                    };
                }
            return data;
        }
    }
}