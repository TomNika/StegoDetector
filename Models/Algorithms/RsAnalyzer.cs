using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace StegoAnalysis.Models.Algorithms
{
    public class RsAnalyzer
    {
        public double? Analyze(string imagePath)
        {
            using var image = Image.Load<Rgba32>(imagePath);
            if (image.Width < 4 || image.Height < 4) return null;

            byte[] pixels = ExtractLuminance(image);
            int pixelCount = pixels.Length;
            int groupSize = 4;
            int groupCount = pixelCount / groupSize;

            int[] maskM = { 1, 0, 0, 1 };
            int[] maskMinusM = { -1, 0, 0, -1 };

            double rM, sM, rMinusM, sMinusM;
            CalculateRS(pixels, groupSize, groupCount, maskM, out rM, out sM);
            CalculateRS(pixels, groupSize, groupCount, maskMinusM, out rMinusM, out sMinusM);

            byte[] flippedPixels = new byte[pixelCount];
            for (int i = 0; i < pixelCount; i++)
                flippedPixels[i] = (byte)(pixels[i] ^ 0x01);

            double rMf, sMf, rMinusMf, sMinusMf;
            CalculateRS(flippedPixels, groupSize, groupCount, maskM, out rMf, out sMf);
            CalculateRS(flippedPixels, groupSize, groupCount, maskMinusM, out rMinusMf, out sMinusMf);

            double a = (rM - rMf) + (rMinusM - rMinusMf);
            double b = (sM - sMf) + (sMinusM - sMinusMf);
            double c = (rMf - rM) + (sMf - sM);

            if (Math.Abs(a) < 1e-9) return null;

            double p = (b - Math.Sqrt(b * b + 4 * a * c)) / (2 * a);
            if (p < 0) p = (b + Math.Sqrt(b * b + 4 * a * c)) / (2 * a);
            if (double.IsNaN(p) || p < 0) p = 0;
            if (p > 1) p = 1;

            return p * 100;
        }

        private byte[] ExtractLuminance(Image<Rgba32> image)
        {
            int total = image.Width * image.Height;
            byte[] lum = new byte[total];
            int idx = 0;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var p = image[x, y];
                    lum[idx++] = (byte)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
                }
            }
            return lum;
        }

        private int Smoothness(byte[] group)
        {
            int sum = 0;
            for (int i = 0; i < group.Length - 1; i++)
                sum += Math.Abs(group[i] - group[i + 1]);
            return sum;
        }

        private byte Flip(byte value, int flipType)
        {
            if (flipType == 0) return value;

            if (flipType == 1)
            {
                if (value % 2 == 0)
                {
                    if (value == 254) return 255;
                    return (byte)(value + 1);
                }
                else
                {
                    if (value == 1) return 0;
                    return (byte)(value - 1);
                }
            }

            else
            {
                if (value % 2 == 0)
                {
                    if (value == 0) return 1;
                    return (byte)(value - 1);
                }
                else
                {
                    if (value == 255) return 254;
                    return (byte)(value + 1);
                }
            }
        }

        private void ApplyMask(byte[] group, int[] mask, byte[] result)
        {
            for (int i = 0; i < group.Length; i++)
                result[i] = Flip(group[i], mask[i]);
        }

        private void CalculateRS(byte[] pixels, int groupSize, int groupCount, int[] mask, out double r, out double s)
        {
            int regular = 0, singular = 0;
            byte[] group = new byte[groupSize];
            byte[] flipped = new byte[groupSize];

            for (int g = 0; g < groupCount; g++)
            {
                Array.Copy(pixels, g * groupSize, group, 0, groupSize);
                int originalSmooth = Smoothness(group);
                ApplyMask(group, mask, flipped);
                int flippedSmooth = Smoothness(flipped);

                if (flippedSmooth > originalSmooth)
                    regular++;
                else if (flippedSmooth < originalSmooth)
                    singular++;
            }
            r = (double)regular / groupCount;
            s = (double)singular / groupCount;
        }
    }
}