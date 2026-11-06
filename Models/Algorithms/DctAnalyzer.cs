using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace StegoAnalysis.Models.Algorithms
{
    public class DctAnalyzer
    {
        private const int BlockSize = 8;
        private const int Step = 16; 

        public DctAnalysisResult Analyze(string imagePath)
        {
            using var image = Image.Load<Rgba32>(imagePath);
            if (image.Width < BlockSize || image.Height < BlockSize)
                return new DctAnalysisResult { IsMessageDetected = false, Message = "Изображение меньше 8x8 пикселей" };

            double[,] luminance = ExtractLuminance(image);
            List<double> allDiffs = new List<double>();

            for (int y = 0; y <= luminance.GetLength(0) - BlockSize; y += Step)
            {
                for (int x = 0; x <= luminance.GetLength(1) - BlockSize; x += Step)
                {
                    double[,] block = ExtractBlock(luminance, x, y);
                    double[,] dct = ComputeDCT(block);
                    double diff = Math.Abs(dct[3, 4]) - Math.Abs(dct[4, 3]);
                    allDiffs.Add(diff);
                }
            }

            if (allDiffs.Count < 10)
                return new DctAnalysisResult { IsMessageDetected = false, Message = "Недостаточно блоков для анализа" };

            var (detected, threshold, start, end) = FindStepping(allDiffs);
            if (detected)
            {
                return new DctAnalysisResult
                {
                    IsMessageDetected = true,
                    EmbeddingThreshold = threshold,
                    EmbeddedMessageLength = end - start + 1,
                    Message = $"Обнаружено встраивание (порог {threshold:F2}, длина ~{end - start + 1} бит)"
                };
            }
            else
            {
                return new DctAnalysisResult
                {
                    IsMessageDetected = false,
                    Message = "Встраивание по методу Коха-Жао не обнаружено"
                };
            }
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

        private double[,] ExtractBlock(double[,] source, int startX, int startY)
        {
            double[,] block = new double[BlockSize, BlockSize];
            for (int i = 0; i < BlockSize; i++)
                for (int j = 0; j < BlockSize; j++)
                    block[i, j] = source[startY + i, startX + j];
            return block;
        }

        private double[,] ComputeDCT(double[,] block)
        {
            int N = BlockSize;
            double[,] coeff = new double[N, N];
            for (int u = 0; u < N; u++)
            {
                for (int v = 0; v < N; v++)
                {
                    double cu = u == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                    double cv = v == 0 ? 1.0 / Math.Sqrt(2) : 1.0;
                    double sum = 0.0;
                    for (int x = 0; x < N; x++)
                        for (int y = 0; y < N; y++)
                            sum += block[x, y] *
                                   Math.Cos((2 * x + 1) * u * Math.PI / (2 * N)) *
                                   Math.Cos((2 * y + 1) * v * Math.PI / (2 * N));
                    coeff[u, v] = 0.25 * cu * cv * sum;
                }
            }
            return coeff;
        }

        private (bool detected, double threshold, int start, int end) FindStepping(List<double> diffs)
        {
            if (diffs.Count < 10) return (false, 0, 0, 0);

            int windowSize = Math.Min(20, diffs.Count / 2);
            double baseline = diffs.Take(windowSize).Average();
            double std = Math.Sqrt(diffs.Take(windowSize).Select(d => Math.Pow(d - baseline, 2)).Average());
            double threshold = 2.5 * std;

            int start = -1, end = -1;
            int consecutive = 0;
            for (int i = 0; i < diffs.Count; i++)
            {
                double deviation = Math.Abs(diffs[i] - baseline);
                if (deviation > threshold)
                {
                    if (start == -1) start = i;
                    consecutive++;
                    end = i;
                }
                else
                {
                    if (consecutive > 5) break;
                    consecutive = 0;
                }
            }

            return (start != -1 && consecutive > 5, threshold, start, end);
        }
    }

    public class DctAnalysisResult
    {
        public bool IsMessageDetected { get; set; }
        public double EmbeddingThreshold { get; set; }
        public int EmbeddedMessageLength { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}