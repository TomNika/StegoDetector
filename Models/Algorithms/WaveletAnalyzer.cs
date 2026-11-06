using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace StegoAnalysis.Models.Algorithms
{
    public class WaveletAnalyzer
    {
        public WaveletAnalysisResult Analyze(string imagePath)
        {
            using var image = Image.Load<Rgba32>(imagePath);
            double[,] luminance = ExtractLuminance(image);

            int rows = luminance.GetLength(0);
            int cols = luminance.GetLength(1);
            if (rows % 2 != 0) rows--;
            if (cols % 2 != 0) cols--;
            if (rows < 2 || cols < 2)
                return new WaveletAnalysisResult(0, false, "Изображение слишком мало для вейвлет-анализа");

            double[,] cropped = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    cropped[i, j] = luminance[i, j];

            var (ll, lh, hl, hh) = HaarWaveletTransform(cropped);

            double energyLL = ComputeEnergy(ll);
            double energyLH = ComputeEnergy(lh);
            double energyHL = ComputeEnergy(hl);
            double energyHH = ComputeEnergy(hh);
            double totalEnergy = energyLL + energyLH + energyHL + energyHH;

            double highFreqEnergy = energyLH + energyHL + energyHH;
            double estimatedLength = (highFreqEnergy / totalEnergy) * 100.0;
            estimatedLength = Math.Clamp(estimatedLength, 0.0, 100.0);

            var features = new List<double>();
            features.AddRange(ComputeStatistics(ll));
            features.AddRange(ComputeStatistics(lh));
            features.AddRange(ComputeStatistics(hl));
            features.AddRange(ComputeStatistics(hh));

            bool detected = estimatedLength > 25.0;

            string message = detected
                ? $"Вейвлет-анализ выявил аномалии. Оценка длины сообщения: {estimatedLength:F1}%"
                : $"Вейвлет-анализ не выявил явных аномалий. Оценка длины сообщения: {estimatedLength:F1}%";

            return new WaveletAnalysisResult(estimatedLength, detected, message, features);
        }

        private double[,] ExtractLuminance(Image<Rgba32> image)
        {
            double[,] lum = new double[image.Height, image.Width];
            for (int y = 0; y < image.Height; y++)
                for (int x = 0; x < image.Width; x++)
                {
                    var p = image[x, y];
                    lum[y, x] = 0.299 * p.R + 0.587 * p.G + 0.114 * p.B;
                }
            return lum;
        }

        private (double[,] ll, double[,] lh, double[,] hl, double[,] hh)
            HaarWaveletTransform(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int newRows = rows / 2;
            int newCols = cols / 2;

            var ll = new double[newRows, newCols];
            var lh = new double[newRows, newCols];
            var hl = new double[newRows, newCols];
            var hh = new double[newRows, newCols];

            for (int i = 0; i < rows; i += 2)
                for (int j = 0; j < cols; j += 2)
                {
                    int di = i / 2;
                    int dj = j / 2;

                    double a = matrix[i, j];
                    double b = matrix[i, j + 1];
                    double c = matrix[i + 1, j];
                    double d = matrix[i + 1, j + 1];

                    ll[di, dj] = (a + b + c + d) / 4.0;
                    lh[di, dj] = ((a + c) - (b + d)) / 4.0;
                    hl[di, dj] = ((a + b) - (c + d)) / 4.0;
                    hh[di, dj] = ((a + d) - (b + c)) / 4.0;
                }

            return (ll, lh, hl, hh);
        }

        private double ComputeEnergy(double[,] coefficients)
        {
            double sum = 0.0;
            for (int i = 0; i < coefficients.GetLength(0); i++)
                for (int j = 0; j < coefficients.GetLength(1); j++)
                    sum += coefficients[i, j] * coefficients[i, j];
            return sum;
        }

        private List<double> ComputeStatistics(double[,] coefficients)
        {
            var data = coefficients.Cast<double>().ToArray();
            if (data.Length == 0)
                return new List<double> { 0, 0, 0, 0 };

            double mean = data.Average();
            double variance = data.Variance();
            double skewness = data.Skewness();
            double kurtosis = data.Kurtosis();

            return new List<double> { mean, variance, skewness, kurtosis };
        }
    }

    public class WaveletAnalysisResult
    {
        public double EstimatedLengthPercent { get; private set; }
        public bool IsMessageDetected { get; private set; }
        public string Message { get; private set; }
        public List<double> FeatureVector { get; private set; }

        public WaveletAnalysisResult(double estimatedLength, bool detected, string message, List<double>? features = null)
        {
            EstimatedLengthPercent = estimatedLength;
            IsMessageDetected = detected;
            Message = message;
            FeatureVector = features ?? new List<double>();
        }
    }
}