using System;
using System.Collections.Generic;
using System.Drawing;

namespace SeamCarving
{
    class CAIR
    {
        private const int BIGNUM = (int)10e6;

        private int adjWidth;  // width after adding or removing seams
        private int adjHeight; // height after adding or removing seams

        private Bitmap coloredOriginal; // original image (not in grayscale)
        private int[,] original, energy, accEnergy, seamImg; // internal representations of images

        public int width { get; private set; }  // width of the original image
        public int height { get; private set; } // height of the original image

        public bool withForwardEnergy { get; set; }

        public CAIR(Bitmap input)
        {
            this.coloredOriginal = input;
            width = adjWidth = input.Width;
            height = adjHeight = input.Height;

            // allocate and fill images
            original = new int[height * 2, width * 2];
            energy = new int[height * 2, width * 2];
            accEnergy = new int[height * 2, width * 2];
            seamImg = new int[height * 2, width * 2];

            ConvertToGrayscale(input, original);
            CalculateEnergy(original, energy);
            CalculateAccumulatedEnergy(energy, accEnergy);
            ConvertToGrayscale(input, seamImg);
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
                    if (arr[r, c] > max && arr[r,c] < BIGNUM)
                        max = arr[r, c];

            // fill result bitmap
            for (int r = 1; r < h - 1; ++r)
            {
                for (int c = 1; c < w - 1; ++c)
                {
                    if (arr[r, c] == -1)
                        lockedRet.SetPixel(c, r, Color.FromArgb(255, 0, 0)); // to visualise seams
                    else
                    {
                        byte col = Convert.ToByte(arr[r, c] / max * 255);
                        lockedRet.SetPixel(c, r, Color.FromArgb(col, col, col));
                    }
                }
            }

            lockedRet.UnlockBits();
            return ret;
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
            for (int i = 0; i < width * 2; ++i) // first and last row, all of it
                ret[0, i] = ret[adjHeight - 1, i] = BIGNUM;
            for (int i = 0; i < height * 2; ++i) // first and last column, all of it
                ret[i, 0] = ret[i, adjWidth - 1] = BIGNUM;

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
                    int cl = withForwardEnergy ? Math.Abs(input[r, c + 1] - input[r, c - 1]) + 
                                                 Math.Abs(input[r - 1, c] - input[r, c - 1]) : 0;
                    int cu = withForwardEnergy ? Math.Abs(input[r, c + 1] - input[r, c - 1]) : 0;
                    int cr = withForwardEnergy ? Math.Abs(input[r, c + 1] - input[r, c - 1]) + 
                                                 Math.Abs(input[r - 1, c] - input[r, c + 1]) : 0;

                    if (c == 1 || c == adjWidth - 2) 
                        cl = cu = cr = 0;

                    // find min of 3
                    int best = ret[r - 1, c] + cu;
                    if (ret[r - 1, c - 1] + cl < best)
                        best = ret[r - 1, c - 1] + cl;
                    if (ret[r - 1, c + 1] + cr < best)
                        best = ret[r - 1, c + 1] + cr;

                    // accumulate the minimum
                    ret[r, c] += best;
                }
            }
        }

        private Seam FindBestSeam(int[,] input)
        {
            Seam s = new Seam();

            // find start column with the least accumulated energy
            int lowestEnergy = BIGNUM;
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

            s.columns.Reverse();
            return s;
        }

        private void RemoveOneSeam(int[,] input, Seam s)
        {
            for (int r = 1; r < adjHeight - 1; ++r)
                for (int c = s.columns[r-1] + 1; c < adjWidth - 1; ++c)
                    input[r, c - 1] = input[r, c]; // skip pixel if we passed the deleted column
        }

        private List<Seam> RemoveSeams(Update callback, int diff)
        {
            List<Seam> ret = new List<Seam>();

            ConvertToGrayscale(coloredOriginal, original);
            for (int i = 0; i < diff; ++i)
            {
                CalculateEnergy(original, energy);
                CalculateAccumulatedEnergy(energy, accEnergy);
                Seam bestSeam = FindBestSeam(accEnergy);
                ret.Add(bestSeam);

                RemoveOneSeam(original, bestSeam);
                adjWidth--;

                callback((int)(i * 100.0 / diff)); // update progressbar
            }

            return ret;
        }

        private void AddDummyEnergy(int[,] input, Seam s)
        {
            const int energyAdded = 25;
            const int std = 5; // not really standard deviation, more like number of pixels affected to each side
            const int step = energyAdded / std;

            for (int r = 1; r < adjHeight - 1; ++r)
            {
                int insertCol = s.columns[r - 1];

                // a bit to the left
                for (int c = Math.Max(1, insertCol - std), weight = step; c < insertCol; ++c, weight += step)
                    input[r, c] += r * weight;
                
                // most of it at the center
                input[r, insertCol] += r * energyAdded;

                // a bit to the right
                for (int c = Math.Min(adjWidth - 2, insertCol + std), weight = std * step - step; c > insertCol; --c, weight -= step) 
                    input[r, c] -= r * weight;
            }
        }

        private void AddOneSeam(int[,] input, Seam s)
        {
            for (int r = 1; r < adjHeight - 1; ++r)
            {
                int insertCol = s.columns[r - 1];

                // from insert point on, move everything to the right
                for (int c = adjWidth - 1; c >= insertCol; --c)
                    input[r, c + 1] = input[r, c];

                if (insertCol == 1) // first column
                    input[r, insertCol] = input[r, insertCol + 1];
                else if (insertCol == adjWidth - 2) // last column
                    input[r, insertCol] = input[r, insertCol - 1];
                else // in the middle -> average
                    input[r, insertCol] = (input[r, insertCol - 1] + input[r, insertCol + 1]) / 2;
            }
        }

        private List<Seam> AddSeams(Update callback, int diff)
        {
            double step = 100.0 / diff;

            ConvertToGrayscale(coloredOriginal, original);
            CalculateEnergy(original, energy);
            CalculateAccumulatedEnergy(energy, accEnergy);
            
            // find seams to insert
            List<Seam> bestSeams = new List<Seam>();
            for (int i = 0; i < diff; ++i)
            {
                Seam bestSeam = FindBestSeam(accEnergy);
                bestSeams.Add(bestSeam);
                AddDummyEnergy(accEnergy, bestSeam);

                callback((int)(i * step / 2)); // update progressbar
            }

            // insert on the positions
            for (int i = 0; i < bestSeams.Count; ++i)
            {
                Seam bestSeam = bestSeams[i];

                AddOneSeam(original, bestSeam);
                adjWidth++;

                callback((int)(50 + i * step / 2)); // update progressbar
            }

            CalculateEnergy(original, energy);
            CalculateAccumulatedEnergy(energy, accEnergy);

            return bestSeams;
        }

        private void VisualizeSeams(List<Seam> seams, int shift)
        {
            for (int i = 0; i < seams.Count; ++i) // iterate seams
            {
                Seam curr = seams[i];
                for (int j = i + 1; j < seams.Count; ++j) // iterate seams removed after seams[i]
                    for (int r = 1; r < adjHeight - 1; ++r) // iterate rows
                        if (seams[j].columns[r - 1] > curr.columns[r - 1])
                            seams[j].columns[r - 1] += shift; // -1 for insertions, +1 for removals
            }

            foreach (Seam s in seams)
                for (int r = 1; r < adjHeight - 1; ++r)
                    seamImg[r, s.columns[r - 1]] = -1; // see converttobitmap (-1 is a special value)
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

        public Bitmap GetSeamImage()
        {
            return ConvertToBitmap(seamImg, width, height);
        }

        public string GetFormattedSize()
        {
            return width.ToString() + "x" + height.ToString();
        }

        public void Resize(Update callback, int newWidth)
        {
            // reset things
            ConvertToGrayscale(coloredOriginal, original);
            CalculateEnergy(original, energy);
            CalculateAccumulatedEnergy(energy, accEnergy);
            ConvertToGrayscale(coloredOriginal, seamImg);
            adjWidth = width;
            adjHeight = height;

            int currWidth = adjWidth;
            int diff = currWidth - newWidth;

            if (diff < 0) // insert seams (make it wider)
            {
                List<Seam> seams = AddSeams(callback, Math.Abs(diff));
                VisualizeSeams(seams, -1);
            }
            else // remove seams (make it smaller)
            {
                List<Seam> seams = RemoveSeams(callback, Math.Abs(diff));
                VisualizeSeams(seams, +1);
            }

            // TODO: remove columns in colored image (operations are done on a grayscale version now)
        }
    }
}