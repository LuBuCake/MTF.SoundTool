using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static System.Math;

namespace MTF.SoundTool.Base.Helpers
{
    // Code reference: https://github.com/jackoalan/gc-dspadpcm-encode

    public class NGCDSPEncoder
    {
        const double DBL_EPSILON = 2.2204460492503131e-016;
        const double DBL_MAX = 1.79769e+308;

        static double[] InnerProductMerge(short[][] pcmBuf, int bufField)
        {
            double[] vecOut = new double[3];

            for (int i = 0; i <= 2; i++)
            {
                vecOut[i] = 0.0f;
                for (int x = 0; x < 14; x++)
                {
                    if (x - i < 0)
                        vecOut[i] -= pcmBuf[bufField - 1][14 + (x - i)] * pcmBuf[bufField][x];
                    else
                        vecOut[i] -= pcmBuf[bufField][x - i] * pcmBuf[bufField][x];
                }
            }

            return vecOut;
        }

        static double[][] OuterProductMerge(short[][] pcmBuf, int bufField)
        {
            var mtxOut = new double[3][];
            for (int i = 0; i < 3; i++)
                mtxOut[i] = new double[3];

            for (int x = 1; x <= 2; x++)
                for (int y = 1; y <= 2; y++)
                {
                    mtxOut[x][y] = 0.0;
                    for (int z = 0; z < 14; z++)
                    {
                        if (z - x < 0 && z - y < 0)
                            mtxOut[x][y] += pcmBuf[bufField - 1][14 + (z - x)] * pcmBuf[bufField - 1][14 + (z - y)];
                        else if (z - x < 0)
                            mtxOut[x][y] += pcmBuf[bufField - 1][14 + (z - x)] * pcmBuf[bufField][z - y];
                        else if (z - y < 0)
                            mtxOut[x][y] += pcmBuf[bufField][z - x] * pcmBuf[bufField - 1][14 + (z - y)];
                        else
                            mtxOut[x][y] += pcmBuf[bufField][z - x] * pcmBuf[bufField][z - y];
                    }
                }

            return mtxOut;
        }

        static int[] AnalyzeRanges(double[][] mtx, int[] vecIdxsOut, out bool result)
        {
            result = false;

            double[] recips = new double[3];
            double val, tmp, min, max;

            for (int x = 1; x <= 2; x++)
            {
                val = Max(Abs(mtx[x][1]), Abs(mtx[x][2]));
                if (val < DBL_EPSILON)
                {
                    result = true;
                    return vecIdxsOut;
                }

                recips[x] = 1.0 / val;
            }

            int maxIndex = 0;
            for (int i = 1; i <= 2; i++)
            {
                for (int x = 1; x < i; x++)
                {
                    tmp = mtx[x][i];
                    for (int y = 1; y < x; y++)
                        tmp -= mtx[x][y] * mtx[y][i];
                    mtx[x][i] = tmp;
                }

                val = 0.0;
                for (int x = i; x <= 2; x++)
                {
                    tmp = mtx[x][i];
                    for (int y = 1; y < i; y++)
                        tmp -= mtx[x][y] * mtx[y][i];

                    mtx[x][i] = tmp;
                    tmp = Abs(tmp) * recips[x];
                    if (tmp >= val)
                    {
                        val = tmp;
                        maxIndex = x;
                    }
                }

                if (maxIndex != i)
                {
                    for (int y = 1; y <= 2; y++)
                    {
                        tmp = mtx[maxIndex][y];
                        mtx[maxIndex][y] = mtx[i][y];
                        mtx[i][y] = tmp;
                    }
                    recips[maxIndex] = recips[i];
                }

                vecIdxsOut[i] = maxIndex;

                if (mtx[i][i] == 0.0)
                {
                    result = true;
                    return vecIdxsOut;
                }

                if (i != 2)
                {
                    tmp = 1.0 / mtx[i][i];
                    for (int x = i + 1; x <= 2; x++)
                        mtx[x][i] *= tmp;
                }
            }

            min = 1.0e10;
            max = 0.0;

            for (int i = 1; i <= 2; i++)
            {
                tmp = Abs(mtx[i][i]);
                if (tmp < min)
                    min = tmp;
                if (tmp > max)
                    max = tmp;
            }

            if (min / max < 1.0e-10)
            {
                result = true;
                return vecIdxsOut;
            }

            return vecIdxsOut;
        }

        static double[] BidirectionalFilter(double[][] mtx, int[] vecIdxs, double[] vecOut)
        {
            double tmp;

            for (int i = 1, x = 0; i <= 2; i++)
            {
                int index = vecIdxs[i];
                tmp = vecOut[index];
                vecOut[index] = vecOut[i];
                if (x != 0)
                    for (int y = x; y <= i - 1; y++)
                        tmp -= vecOut[y] * mtx[i][y];
                else if (tmp != 0.0)
                    x = i;
                vecOut[i] = tmp;
            }

            for (int i = 2; i > 0; i--)
            {
                tmp = vecOut[i];
                for (int y = i + 1; y <= 2; y++)
                    tmp -= vecOut[y] * mtx[i][y];
                vecOut[i] = tmp / mtx[i][i];
            }

            vecOut[0] = 1.0;

            return vecOut;
        }

        static double[] QuadraticMerge(double[] inOutVec, out bool res)
        {
            res = false;

            double v0, v1, v2 = inOutVec[2];
            double tmp = 1.0 - (v2 * v2);

            if (tmp == 0.0)
            {
                res = true;
                return inOutVec;
            }

            v0 = (inOutVec[0] - (v2 * v2)) / tmp;
            v1 = (inOutVec[1] - (inOutVec[1] * v2)) / tmp;

            inOutVec[0] = v0;
            inOutVec[1] = v1;

            res = Abs(v1) > 1.0;
            return inOutVec;
        }

        static double[] FinishRecord(double[] input)
        {
            double[] output = new double[3];
            for (int z = 1; z <= 2; z++)
            {
                if (input[z] >= 1.0)
                    input[z] = 0.9999999999;
                else if (input[z] <= -1.0)
                    input[z] = -0.9999999999;
            }
            output[0] = 1.0;
            output[1] = (input[2] * input[1]) + input[1];
            output[2] = input[2];

            return output;
        }

        static double[] MatrixFilter(double[] src, double[] dst)
        {
            double[][] mtx = new double[3][];
            for (int i = 0; i < 3; i++)
                mtx[i] = new double[3];

            mtx[2][0] = 1.0;
            for (int i = 1; i <= 2; i++)
                mtx[2][i] = -src[i];

            for (int i = 2; i > 0; i--)
            {
                double val = 1.0 - (mtx[i][i] * mtx[i][i]);
                for (int y = 1; y <= i; y++)
                    mtx[i - 1][y] = ((mtx[i][i] * mtx[i][y]) + mtx[i][y]) / val;
            }

            dst[0] = 1.0;
            for (int i = 1; i <= 2; i++)
            {
                dst[i] = 0.0;
                for (int y = 1; y <= i; y++)
                    dst[i] += mtx[i][y] * dst[i - y];
            }

            return dst;
        }

        static double[] MergeFinishRecord(double[] src, double[] dst)
        {
            double[] tmp = new double[3];
            double val = src[0];

            dst[0] = 1.0;
            for (int i = 1; i <= 2; i++)
            {
                double v2 = 0.0;
                for (int y = 1; y < i; y++)
                    v2 += dst[y] * src[i - y];

                if (val > 0.0)
                    dst[i] = -(v2 + src[i]) / val;
                else
                    dst[i] = 0.0;

                tmp[i] = dst[i];

                for (int y = 1; y < i; y++)
                    dst[y] += dst[i] * dst[i - y];

                val *= 1.0 - (dst[i] * dst[i]);
            }

            return FinishRecord(tmp);
        }

        static double ContrastVectors(double[] source1, double[] source2)
        {
            double val = (source2[2] * source2[1] + -source2[1]) / (1.0 - source2[2] * source2[2]);
            double val1 = (source1[0] * source1[0]) + (source1[1] * source1[1]) + (source1[2] * source1[2]);
            double val2 = (source1[0] * source1[1]) + (source1[1] * source1[2]);
            double val3 = source1[0] * source1[2];
            return val1 + (2.0 * val * val2) + (2.0 * (-source2[1] * val + -source2[2]) * val3);
        }

        static double[][] FilterRecords(double[][] vecBest, int exp, double[][] records, int recordCount)
        {
            double[][] bufferList = new double[8][];
            for (int i = 0; i < 8; i++)
                bufferList[i] = new double[3];

            int[] buffer1 = new int[8];
            double[] buffer2 = new double[3];

            int index;
            double value, tempVal = 0;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < exp; y++)
                {
                    buffer1[y] = 0;
                    for (int i = 0; i <= 2; i++)
                        bufferList[y][i] = 0.0;
                }
                for (int z = 0; z < recordCount; z++)
                {
                    index = 0;
                    value = 1.0e30;
                    for (int i = 0; i < exp; i++)
                    {
                        tempVal = ContrastVectors(vecBest[i], records[z]);
                        if (tempVal < value)
                        {
                            value = tempVal;
                            index = i;
                        }
                    }
                    buffer1[index]++;
                    MatrixFilter(records[z], buffer2);
                    for (int i = 0; i <= 2; i++)
                        bufferList[index][i] += buffer2[i];
                }

                for (int i = 0; i < exp; i++)
                    if (buffer1[i] > 0)
                        for (int y = 0; y <= 2; y++)
                            bufferList[i][y] /= buffer1[i];

                for (int i = 0; i < exp; i++)
                    vecBest[i] = MergeFinishRecord(bufferList[i], vecBest[i]);
            }

            return vecBest;
        }

        public static short[][] DSPCorrelateCoefs(byte[] soundData, int samples)
        {
            List<short> coefsOut = new List<short>();
            for (int i = 0; i < 16; i++)
                coefsOut.Add(0);

            int numFrames = (samples + 13) / 14;

            short[][] pcmHistBuffer = new short[2][];
            pcmHistBuffer[0] = new short[14];
            pcmHistBuffer[1] = new short[14];

            double[] vec1 = new double[3];
            double[] vec2 = new double[3];

            double[][] mtx = new double[3][];
            for (int i = 0; i < 3; i++)
                mtx[i] = new double[3];
            int[] vecIdxs = new int[3];

            double[][] records = new double[numFrames * 2][];
            for (int i = 0; i < numFrames * 2; i++)
                records[i] = new double[3];
            int recordCount = 0;

            double[][] vecBest = new double[8][];
            for (int i = 0; i < 8; i++)
                vecBest[i] = new double[3];

            using (var br = new BinaryReader(new MemoryStream(soundData)))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    int frameSamples = (br.BaseStream.Length - br.BaseStream.Position > 0x3800) ?
                        0x3800 :
                        (int)(br.BaseStream.Length - br.BaseStream.Position);
                    var blockFrame = br.ReadBytes(frameSamples * 2);
                    if (blockFrame.Length % 28 != 0)
                    {
                        var tmp = blockFrame.ToList();
                        while (tmp.Count() % 28 != 0) tmp.AddRange(new byte[] { 0, 0 });
                        blockFrame = tmp.ToArray();
                    }

                    using (var blockBr = new BinaryReader(new MemoryStream(blockFrame)))
                    {
                        while (blockBr.BaseStream.Position < blockBr.BaseStream.Length)
                        {
                            for (int z = 0; z < 14; z++)
                                pcmHistBuffer[0][z] = pcmHistBuffer[1][z];
                            for (int z = 0; z < 14; z++)
                                pcmHistBuffer[1][z] = blockBr.ReadInt16();

                            vec1 = InnerProductMerge(pcmHistBuffer, 1);
                            if (Abs(vec1[0]) > 10.0)
                            {
                                mtx = OuterProductMerge(pcmHistBuffer, 1);
                                bool res;
                                vecIdxs = AnalyzeRanges(mtx, vecIdxs, out res);
                                if (!res)
                                {
                                    vec1 = BidirectionalFilter(mtx, vecIdxs, vec1);
                                    vec1 = QuadraticMerge(vec1, out res);
                                    if (!res)
                                    {
                                        records[recordCount] = FinishRecord(vec1);
                                        recordCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            vec1[0] = 1.0;
            vec1[1] = 0.0;
            vec1[2] = 0.0;

            for (int z = 0; z < recordCount; z++)
            {
                vecBest[0] = MatrixFilter(records[z], vecBest[0]);
                for (int y = 1; y <= 2; y++)
                    vec1[y] += vecBest[0][y];
            }
            for (int y = 1; y <= 2; y++)
                vec1[y] /= recordCount;

            vecBest[0] = MergeFinishRecord(vec1, vecBest[0]);


            int exp = 1;
            for (int w = 0; w < 3;)
            {
                vec2[0] = 0.0;
                vec2[1] = -1.0;
                vec2[2] = 0.0;
                for (int i = 0; i < exp; i++)
                    for (int y = 0; y <= 2; y++)
                        vecBest[exp + i][y] = (0.01 * vec2[y]) + vecBest[i][y];
                ++w;
                exp = 1 << w;
                vecBest = FilterRecords(vecBest, exp, records, recordCount);
            }

            for (int z = 0; z < 8; z++)
            {
                double d;
                d = -vecBest[z][1] * 2048.0;
                if (d > 0.0)
                    coefsOut[z * 2] = (d > 32767.0) ? (short)32767 : (short)Round(d, MidpointRounding.AwayFromZero);
                else
                    coefsOut[z * 2] = (d < -32768.0) ? (short)-32768 : (short)Round(d, MidpointRounding.AwayFromZero);

                d = -vecBest[z][2] * 2048.0;
                if (d > 0.0)
                    coefsOut[z * 2 + 1] = (d > 32767.0) ? (short)32767 : (short)Round(d, MidpointRounding.AwayFromZero);
                else
                    coefsOut[z * 2 + 1] = (d < -32768.0) ? (short)-32768 : (short)Round(d, MidpointRounding.AwayFromZero);
            }

            short[][] coefsOutS = new short[8][];
            var cCount = 0;
            for (int i = 0; i < 8; i++)
            {
                coefsOutS[i] = new short[2];
                for (int j = 0; j < 2; j++)
                    coefsOutS[i][j] = coefsOut[cCount++];
            }

            return coefsOutS;
        }

        public static byte[] DSPEncodeFrame(short[] pcmInOut, int sampleCount, short[][] coefsIn)
        {
            byte[] adpcmOut = new byte[8];

            int[][] inSamples = new int[8][];
            for (int i = 0; i < 8; i++)
                inSamples[i] = new int[16];
            int[][] outSamples = new int[8][];
            for (int i = 0; i < 8; i++)
                outSamples[i] = new int[14];

            int bestIndex = 0;

            int[] scale = new int[8];
            double[] distAccum = new double[8];

            for (int i = 0; i < 8; i++)
            {
                int v1, v2, v3;
                int distance, index;

                inSamples[i][0] = pcmInOut[0];
                inSamples[i][1] = pcmInOut[1];

                distance = 0;
                for (int s = 0; s < sampleCount; s++)
                {
                    inSamples[i][s + 2] = v1 = ((pcmInOut[s] * coefsIn[i][1]) + (pcmInOut[s + 1] * coefsIn[i][0])) / 2048;
                    v2 = pcmInOut[s + 2] - v1;
                    v3 = (v2 >= 32767) ? 32767 : (v2 <= -32768) ? -32768 : v2;
                    if (Abs(v3) > Abs(distance))
                        distance = v3;
                }

                for (scale[i] = 0; (scale[i] <= 12) && ((distance > 7) || (distance < -8)); scale[i]++, distance /= 2) { }
                scale[i] = (scale[i] <= 1) ? -1 : scale[i] - 2;

                do
                {
                    scale[i]++;
                    distAccum[i] = 0;
                    index = 0;

                    for (int s = 0; s < sampleCount; s++)
                    {
                        v1 = ((inSamples[i][s] * coefsIn[i][1]) + (inSamples[i][s + 1] * coefsIn[i][0]));
                        v2 = ((pcmInOut[s + 2] << 11) - v1) / 2048;
                        v3 = (v2 > 0) ? (int)((double)v2 / (1 << scale[i]) + 0.4999999f) : (int)((double)v2 / (1 << scale[i]) - 0.4999999f);

                        if (v3 < -8)
                        {
                            if (index < (v3 = -8 - v3))
                                index = v3;
                            v3 = -8;
                        }
                        else if (v3 > 7)
                        {
                            if (index < (v3 -= 7))
                                index = v3;
                            v3 = 7;
                        }

                        outSamples[i][s] = v3;

                        v1 = (v1 + ((v3 * (1 << scale[i])) << 11) + 1024) >> 11;
                        inSamples[i][s + 2] = v2 = (v1 >= 32767) ? 32767 : (v1 <= -32768) ? -32768 : v1;
                        v3 = pcmInOut[s + 2] - v2;
                        distAccum[i] += v3 * (double)v3;
                    }

                    for (int x = index + 8; x > 256; x >>= 1)
                        if (++scale[i] >= 12)
                            scale[i] = 11;

                } while ((scale[i] < 12) && (index > 1));
            }

            double min = DBL_MAX;
            for (int i = 0; i < 8; i++)
            {
                if (distAccum[i] < min)
                {
                    min = distAccum[i];
                    bestIndex = i;
                }
            }

            for (int s = 0; s < sampleCount; s++)
                pcmInOut[s + 2] = (short)inSamples[bestIndex][s + 2];

            adpcmOut[0] = (byte)((bestIndex << 4) | (scale[bestIndex] & 0xF));

            for (int s = sampleCount; s < 14; s++)
                outSamples[bestIndex][s] = 0;

            for (int y = 0; y < 7; y++)
            {
                adpcmOut[y + 1] = (byte)((outSamples[bestIndex][y * 2] << 4) | (outSamples[bestIndex][y * 2 + 1] & 0xF));
            }

            return adpcmOut;
        }
    }
}
