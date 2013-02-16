using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamCarving
{
    class CAIR
    {
        private Bitmap original, energy;
        public int width { get; private set; }
        public int height { get; private set; }

        public CAIR(Bitmap original)
        {
            this.original = original;
            width = original.Width;
            height = original.Height;
        }

        public Bitmap GetOriginalImage()
        {
            return original;
        }

        public string GetFormattedSize()
        {
            return width.ToString() + "x" + height.ToString();
        }

        public Bitmap CalculateEnergy(Bitmap input)
        {
            LockBitmap img = new LockBitmap(input);
            energy = new Bitmap(input.Width, input.Height);
            LockBitmap lockedEnergy = new LockBitmap(energy);

            img.LockBits();
            lockedEnergy.LockBits();

            // calculate grayscale image
            for (int j = 0; j < input.Height; j++)
            {
                for (int i = 0; i < input.Width; i++)
                {
                    Color c = img.GetPixel(i, j);
                    int gray = ((int)c.R + (int)c.G + (int)c.B) / 3;
                    lockedEnergy.SetPixel(i, j, Color.FromArgb(255, gray, gray, gray));
                }
            }

            // calculate gradient (energy)
            for (int j = 1; j < input.Height - 1; j++)
            {
                for (int i = 1; i < input.Width - 1; i++)
                {
                    int gray = Math.Abs((img.GetPixel(i + 1, j - 1).R + 2 * img.GetPixel(i + 1, j).R + img.GetPixel(i + 1, j + 1).R) - (img.GetPixel(i - 1, j - 1).R + 2 * img.GetPixel(i - 1, j).R + img.GetPixel(i - 1, j + 1).R)) / 4 + 
                        Math.Abs((img.GetPixel(i - 1, j + 1).R + 2 * img.GetPixel(i, j + 1).R + img.GetPixel(i + 1, j + 1).R) - (img.GetPixel(i - 1, j - 1).R + 2 * img.GetPixel(i, j - 1).R + img.GetPixel(i + 1, j - 1).R)) / 4;
                    gray = Math.Min(gray, 255);
                    lockedEnergy.SetPixel(i, j, Color.FromArgb(255, gray, gray, gray));
                }
            }

            lockedEnergy.UnlockBits();
            img.UnlockBits();

            return energy;
        }

        public Bitmap CalculateAccumulatedEnergy(Bitmap input)
        {
            return ConvertToBitmap(CalculateAccumulatedEnergyHelper(input));
        }

        public Bitmap Resize(Bitmap input, int val)
        {
            int number = width - val;

            while (number != 0)
            {
                if (number < 0)
                    input = AddOneSeam(input);
                else
                    input = RemoveOneSeam(input);
                --number;
            }

            return input;
        }

        private Bitmap RemoveOneSeam(Bitmap input)
        {
            int[,] newImage = new int[input.Height, input.Width - 1];
            LockBitmap lockedInput = new LockBitmap(input);

            // wat herschikken
            Seam s = FindBestSeam(input);
            List<int> columns = new List<int>();
            columns.Add(s.startColumn);
            columns.AddRange(s.others);
            columns.Reverse();

            lockedInput.LockBits();

            for (int r = 0; r < input.Height; ++r)
            {
                for (int c = 0; c < input.Width; ++c)
                {
                    if (c == columns[r]) // te verwijderen klolom overslaan
                        continue;
                    else if (c < columns[r])
                        newImage[r, c] = lockedInput.GetPixel(c, r).R;
                    else
                        newImage[r, c - 1] = lockedInput.GetPixel(c, r).R;
                }
            }

            lockedInput.UnlockBits();

            return ConvertToBitmap(newImage);
        }

        private Bitmap AddOneSeam(Bitmap input)
        {
            return null;
        }

        private Seam FindBestSeam(Bitmap input)
        {
            energy = null; // make sure it's recalculated
            int[,] acc = CalculateAccumulatedEnergyHelper(input);
            Seam s = new Seam();

            // start kolom vinden
            int lowestEnergy = int.MaxValue;
            for (int c = 0; c < input.Width; ++c)
            {
                if (acc[input.Height - 1, c] < lowestEnergy)
                {
                    lowestEnergy = acc[input.Height - 1, c];
                    s.startColumn = c;
                }
            }

            // pad naar boven vinden
            int currColumn = s.startColumn;
            for (int r = input.Height - 1; r > 0; --r)
            {
                int c1 = int.MaxValue, c2 = acc[r - 1, currColumn], c3 = int.MaxValue;
                if (currColumn > 0)
                    c1 = acc[r - 1, currColumn - 1];
                if (currColumn < input.Width - 1)
                    c2 = acc[r - 1, currColumn + 1];

                int min = c2;
                if (c1 < min)
                    min = c1;
                if (c3 < min)
                    min = c3;

                if (min == c1)
                    currColumn--;
                else if (min == c3)
                    currColumn++;

                s.others.Add(currColumn);
            }

            return s;
        }

        private int[,] CalculateAccumulatedEnergyHelper(Bitmap input)
        {
            if (energy == null)
                CalculateEnergy(input);

            LockBitmap lockedEnergy = new LockBitmap(energy);
            lockedEnergy.LockBits();

            int[,] acc = new int[input.Height, input.Width];

            // initiele waarden kopieren
            for (int r = 0; r < input.Height; ++r)
                for (int c = 0; c < input.Width; ++c)
                    acc[r, c] = lockedEnergy.GetPixel(c, r).R;
            lockedEnergy.UnlockBits();

            for (int r = 1; r < input.Height; ++r)
            {
                for (int c = 0; c < input.Width; ++c)
                {
                    // min waarde van de vorige 3 vinden
                    int best = int.MaxValue;
                    for (int i = -1; i <= 1; ++i)
                    {
                        if (c + i < 0 || c + i >= input.Width)
                            continue;
                        if (acc[r - 1, c + i] < best)
                            best = acc[r - 1, c + i];
                    }

                    // bij het huidige tellen
                    acc[r, c] += best;
                }
            }

            return acc;
        }

        private Bitmap ConvertToBitmap(int[,] acc)
        {
            Bitmap heatmap = new Bitmap(acc.GetLength(1), acc.GetLength(0));
            LockBitmap lockedHeatmap = new LockBitmap(heatmap);
            lockedHeatmap.LockBits();

            IEnumerable<int> flat = acc.Cast<int>();
            int min = flat.Min();
            int max = flat.Max();

            for (int r = 0; r < acc.GetLength(0); ++r)
            {
                for (int c = 0; c < acc.GetLength(1); ++c)
                {
                    double val = (double)(acc[r, c] - min) / (max - min);
                    byte component = Convert.ToByte(val * 255);
                    lockedHeatmap.SetPixel(c, r, Color.FromArgb(component, component, component));
                }
            }

            lockedHeatmap.UnlockBits();

            return heatmap;
        }
    }
}
