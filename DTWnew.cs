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
    public class DTWnew
    {

        static public double dtwcost(MFCCFrame frame1, MFCCFrame frame2)
        {
            double tempSum = 0;
            for (int i = 0; i < 13; i++)
                tempSum += ((frame1.Features[i] - frame2.Features[i])*(frame1.Features[i] - frame2.Features[i]));
            return Math.Sqrt(tempSum);
        }

        public double DTWfun(Sequence seq1, Sequence seq2)
        {
            int m = seq1.Frames.Length, n = seq2.Frames.Length;
            double[,] DTW = new double[m, n];
            //double[,] DTW1 = new double[m, 1];
            for (int i = 0; i < m; i++)
            {
                DTW[i, 0] = 0;
            }
            for (int i = 0; i < n; i++)
            {
                DTW[0, i] = 0;
            }
            DTW[0, 0] = 0;
            //bool k = true;
            for (int i = 1; i < m; i++)
            {
                
                for (int j = 1; j < n; j++)
                {
                  //  DTW[i, 0] = DTW1[i, 0];


                    //if (k == true)
                    //{
                        double cost = dtwcost(seq1.Frames[i ], seq2.Frames[j]);
                        DTW[i, j] = (cost + Math.Min(DTW[i - 1, j], /*Math.Min(DTW[i-1 , j-2],*/ DTW[i - 1, j - 1]));
                      //  DTW[i, 1] = (cost + Math.Min(DTW[i - 1, 1], DTW[i - 1, 0]));
                    //}
                   
                      //else if (k == false)
                        //{
                          // double cost = dtwcost(seq1.Frames[i],seq2.Frames[j]);
                            //DTW[i, 0] = (cost + Math.Min(DTW[i , 0], DTW[i , 1]));
                            //DTW1[i, 0] = DTW[i, 1];
                        //} 
               }
              //  k = !k;
            }
            return DTW[m - 1, n-1];
        }
    }
}