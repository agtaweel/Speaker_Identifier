using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Audio.Filters;
using Recorder.Recorder;
using Recorder.MFCC;

namespace Recorder
{
    class PRUNINGDTW
    {
        static public double dtwcost(MFCCFrame frame1, MFCCFrame frame2)
        {
            double tempSum = 0;
            for (int i = 0; i < 13; i++)
                tempSum += Math.Pow(Math.Abs(frame1.Features[i] - frame2.Features[i]), 2);
            return Math.Sqrt(tempSum);
        }


        static public double DTW2fun(Sequence seq1, Sequence seq2)
        {
            int m = seq1.Frames.Length, n = seq2.Frames.Length;
            int w= Math.Abs(m - n);
            double[,] DTW = new double[m+1, n+1];
           /* for (int i = 1; i < m; i++)
            {
                for (int j = Math.Max(1, (i - w)); j <= Math.Min(2, i + w); j++)
                {
                    DTW[i-1, j] = double.PositiveInfinity;
                }
            }*/
            DTW[0, 0] = 0;
            for (int i = 1; i < m; i++)
            {
                for (int j = Math.Max(1, i - w); j <= Math.Min(n, i + w); j++)
                {
                    
                        double cost = dtwcost(seq1.Frames[i-1], seq2.Frames[j-1]);
                        DTW[i, j] = (cost + Math.Min(DTW[i - 1, j], DTW[i - 1, j - 1]));
                    
                }
            }
            return DTW[m - 1, n-1];
        }
    }
}

