using System;
using System.Drawing;

namespace SeamCarving
{
    class CAIR
    {
        public int width { get; private set; }  // width of the original image
        public int height { get; private set; } // height of the original image
        private int adjWidth;  // width after adding or removing seams
        private int adjHeight; // height after adding or removing seams

        private Bitmap coloredOriginal; // original image (not in grayscale)
        private int[,] original, energy, accEnergy; // internal representations of images

        public CAIR(Bitmap input)
        {
            this.coloredOriginal = input;
            width = adjWidth = input.Width;
            height = adjHeight = input.Height;

            // allocate and fill images
            original = new int[height * 2, width * 2];
            energy = new int[height * 2, width * 2];
            accEnergy = new int[height * 2, width * 2];
            ConvertToGrayscale(input, original);
            CalculateEnergy(original, energy);
            CalculateAccumulatedEnergy(energy, accEnergy);
        }

        private Bitmap ConvertToBitmap(int[,] arr, int w, int h)
        {
            Bitmap ret = new Bitmap(w, h);
            LockBitmap lockedRet = new LockBitmap(ret);
            lockedRet.LockBits();

            // find max to scale the pixels in the [0..255] range
            double max = 0;
            for (int r = 1; r < h - 1; ++r)
                for (int c = 1; c < w - 1; ++c)
                    if (arr[r, c] > max)
                        max = arr[r, c];

            // fill result bitmap
            for (int r = 1; r < h - 1; ++r)
            {
                for (int c = 1; c < w - 1; ++c)
                {
                    byte col = Convert.ToByte(arr[r, c] / max * 255);
                    lockedRet.SetPixel(c, r, Color.FromArgb(col, col, col));
                }
            }

            lockedRet.UnlockBits();
            return ret;
        }

        private void ConvertToArray(Bitmap b, int[,] ret)
        {
            LockBitmap lockedB = new LockBitmap(b);
            lockedB.LockBits();

            // fill result array
            for (int r = 0; r < b.Height; ++r)
                for (int c = 0; c < b.Width; ++c)
                    ret[r, c] = lockedB.GetPixel(c, r).R;

            lockedB.UnlockBits();
        }

        private void ConvertToGrayscale(Bitmap input, int[,] ret)
        {
            LockBitmap img = new LockBitmap(input);
            adjHeight = input.Height;
            adjWidth = input.Width;

            img.LockBits();

            // convert to grayscale
            for (int j = 0; j < adjHeight; j++)
            {
                for (int i = 0; i < adjWidth; i++)
                {
                    Color c = img.GetPixel(i, j);
                    double gray = (double)c.R * 0.3 + (double)c.G * 0.59 + (double)c.B * 0.11;
                    ret[j, i] = (int)gray;
                }
            }

            img.UnlockBits();
        }

        private void CalculateEnergy(int[,] input, int[,] ret)
        {
            // border of the image
            for (int i = 0; i < adjWidth; ++i) // first and last row
                ret[0, i] = ret[adjHeight - 1, i] = int.MaxValue;
            for (int i = 0; i < adjHeight; ++i) // first and last collumn
                ret[i, 0] = ret[i, adjWidth - 1] = int.MaxValue;

            // other pixels with gradient (sum of sobel derivative in x and y directions)
            for (int i = 1; i < adjHeight - 1; i++)
            {
                for (int j = 1; j < adjWidth - 1; j++)
                {
                    int gray = Math.Abs((original[i + 1, j - 1] + 2 * original[i + 1, j] + original[i + 1, j + 1]) - (original[i - 1, j - 1] + 2 * original[i - 1, j] + original[i - 1, j + 1])) / 4 +
                               Math.Abs((original[i - 1, j + 1] + 2 * original[i, j + 1] + original[i + 1, j + 1]) - (original[i - 1, j - 1] + 2 * original[i, j - 1] + original[i + 1, j - 1])) / 4;
                    gray = Math.Min(gray, 255);
                    ret[i, j] = gray;
                }
            }
        }

        private void CalculateAccumulatedEnergy(int[,] input, int[,] ret)
        {
            // copy initial values (the energy image)
            for (int r = 0; r < adjHeight; ++r)
                for (int c = 0; c < adjWidth; ++c)
                    ret[r, c] = input[r, c];

            for (int r = 2; r < adjHeight - 1; ++r)
            {
                for (int c = 1; c < adjWidth - 1; ++c)
                {
                    // find min of 3
                    int best = ret[r - 1, c];
                    if (ret[r - 1, c - 1] < best)
                        best = ret[r - 1, c - 1];
                    if (ret[r - 1, c + 1] < best)
                        best = ret[r - 1, c + 1];

                    // accumulate the minimum
                    ret[r, c] += best;
                }
            }
        }

        private Seam FindBestSeam(int[,] input)
        {
            Seam s = new Seam();

            // find start column with the least accumulated energy
            int lowestEnergy = int.MaxValue;
            int bestColumn = 0;
            for (int c = 1; c < adjWidth - 1; ++c)
            {
                if (accEnergy[adjHeight - 2, c] < lowestEnergy)
                {
                    lowestEnergy = accEnergy[adjHeight - 2, c];
                    bestColumn = c;
                }
            }
            s.columns.Add(bestColumn);

            // construct path to first row
            for (int r = adjHeight - 2; r > 1; --r)
            {
                int c1 = accEnergy[r - 1, bestColumn - 1];
                int c2 = accEnergy[r - 1, bestColumn];
                int c3 = accEnergy[r - 1, bestColumn + 1];

                // select min energy
                int min = c2;
                if (c1 < min) min = c1;
                if (c3 < min) min = c3;

                // adjust column
                if (min == c1) bestColumn--;
                else if (min == c3) bestColumn++;

                s.columns.Add(bestColumn);
            }

            return s;
        }

        private void RemoveOneSeam(int[,] input, Seam s)
        {
            // reverse because we want to start at the first row
            s.columns.Reverse();

            for (int r = 1; r < adjHeight - 1; ++r)
                for (int c = s.columns[r-1] + 1; c < adjWidth - 1; ++c)
                    input[r, c - 1] = input[r, c]; // skip pixel if we passed the deleted column
        }

        private void AddOneSeam(int[,] input, Seam s)
        {
            // not implemented
        }

        public Bitmap GetOriginalImage()
        {
            return ConvertToBitmap(original, adjWidth, adjHeight);
        }

        public Bitmap GetEnergyImage()
        {
            return ConvertToBitmap(energy, adjWidth, adjHeight);
        }

        public Bitmap GetAccEnergyImage()
        {
            return ConvertToBitmap(accEnergy, adjWidth, adjHeight);
        }

        public string GetFormattedSize()
        {
            return width.ToString() + "x" + height.ToString();
        }

        public void Resize(Update callback, int newWidth)
        {
            int currWidth = adjWidth;
            int diff = currWidth - newWidth;
            double step = 100.0 / Math.Abs(diff);

            ConvertToGrayscale(coloredOriginal, original);      // 1) convert to grayscale
            for (int i = 0; i < Math.Abs(diff); ++i)
            {
                CalculateEnergy(original, energy);              // 2) find energy in image
                CalculateAccumulatedEnergy(energy, accEnergy);  // 3) accumulate energy
                Seam bestSeam = FindBestSeam(accEnergy);

                if (diff > 0)
                {
                    RemoveOneSeam(original, bestSeam);          // 4a) remove seam if new width is less than original
                    adjWidth--;
                }
                else                                            // 4b) add seam if new width is bigger than original
                {
                    AddOneSeam(original, bestSeam);
                    adjWidth++;
                }

                callback((int)(i * step)); // update progressbar
            }

            // TODO: remove columns in colored image (operations are done on a grayscale version now)
        }
    }
}