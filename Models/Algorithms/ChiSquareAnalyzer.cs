using System;
using System.Linq;
using MathNet.Numerics.Distributions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace StegoAnalysis.Models.Algorithms
{
    public class ChiSquareAnalyzer
    {
        public ChiSquareAnalysisResult Analyze(string imagePath)
        {
            using var image = Image.Load<Rgba32>(imagePath);
            long[] red = new long[256], green = new long[256], blue = new long[256];
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var p = image[x, y];
                    red[p.R]++; green[p.G]++; blue[p.B]++;
                }

            var redRes = ComputeChannel(red);
            var greenRes = ComputeChannel(green);
            var blueRes = ComputeChannel(blue);

            var all = new[] { redRes, greenRes, blueRes };
            return new ChiSquareAnalysisResult
            {
                AveragePValue = all.Average(c => c.PValue),
                MaxPairSimilarityScore = all.Max(c => c.PairSimilarityScore),
                MinChannelPValue = all.Min(c => c.PValue),
                PValueText = FormatPValue(all.Min(c => c.PValue))
            };
        }

        private ChannelResult ComputeChannel(long[] observed)
        {
            double chiSquare = 0;
            int validPairs = 0;
            double totalAbsDiff = 0, totalPairMass = 0;

            for (int i = 0; i < 256; i += 2)
            {
                long even = observed[i], odd = observed[i + 1];
                long pairTotal = even + odd;
                if (pairTotal == 0) continue;
                double diff = even - odd;
                chiSquare += diff * diff / pairTotal;
                totalAbsDiff += Math.Abs(diff);
                totalPairMass += pairTotal;
                validPairs++;
            }

            int dof = Math.Max(validPairs - 1, 1);
            double pValue = 1.0 - ChiSquared.CDF(dof, chiSquare);
            if (double.IsNaN(pValue) || double.IsInfinity(pValue)) pValue = 0;
            pValue = Math.Clamp(pValue, 0, 1);

            double pairSimilarity = totalPairMass > 0 ? 1.0 - totalAbsDiff / totalPairMass : 0;

            return new ChannelResult { PValue = pValue, PairSimilarityScore = pairSimilarity };
        }

        private string FormatPValue(double p) =>
            p <= 0 ? "< 1E-300" : p < 1E-16 ? p.ToString("0.000E+0") : p.ToString("0.000000");
    }

    public class ChiSquareAnalysisResult
    {
        public double AveragePValue { get; set; }
        public double MaxPairSimilarityScore { get; set; }
        public double MinChannelPValue { get; set; }
        public string PValueText { get; set; } = string.Empty;
    }

    internal class ChannelResult
    {
        public double PValue { get; set; }
        public double PairSimilarityScore { get; set; }
    }
}