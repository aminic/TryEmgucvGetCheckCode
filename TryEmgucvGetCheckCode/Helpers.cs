
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace TryEmgucvGetCheckCode
{


    public class CompareTemplateResult
    {

        public double[] MinValues { get; set; }
        public double[] MaxValues { get; set; }
        public Point[] MinLocations { get; set; }
        public Point[] MaxLocations { get; set; }
    }

    public static class ImageHelper
    {

        //找到最匹配的点，以及该点的值
        public static void FindBestMatchPointAndValue(this Image<Gray, Single> image, Emgu.CV.CvEnum.TemplateMatchingType tmType, out double bestValue, out Point bestPoint)
        {
            bestValue = 0d;
            bestPoint = new Point(0, 0);
            double[] minValues, maxValues;
            Point[] minLocations, maxLocations;
            image.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
            //对于平方差匹配和归一化平方差匹配，最小值表示最好的匹配；其他情况下，最大值表示最好的匹配

            if (tmType == Emgu.CV.CvEnum.TemplateMatchingType.Sqdiff || tmType == Emgu.CV.CvEnum.TemplateMatchingType.SqdiffNormed)
            {
                bestValue = minValues[0];
                bestPoint = minLocations[0];
            }
            else
            {
                bestValue = maxValues[0];
                bestPoint = maxLocations[0];
            }
        }

        public static CompareTemplateResult CompareTemplate(this Image<Bgr, byte> image, Image<Bgr, byte> templateImage)
        {
            var compareImage = image.MatchTemplate(templateImage, TemplateMatchingType.CcoeffNormed);
            double[] minValues, maxValues;
            Point[] minLocations, maxLocations;
            compareImage.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
            var result = new CompareTemplateResult
            {
                MinValues = minValues,
                MaxValues = maxValues,
                MinLocations = minLocations,
                MaxLocations = maxLocations
            };
            return result;
        }


        ////图片裁剪
        //public static Image<Bgr, Byte> CutImage(this Image<Bgr, Byte> image, Tuple<int, int> beginWidthHeight, Tuple<int, int> lengthWidthHeight)
        //{

        //    try
        //    {
        //        System.Drawing.Size roisize = new System.Drawing.Size(lengthWidthHeight.Item1, lengthWidthHeight.Item2); //要裁剪的图片大小
        //        IntPtr dst = CvInvoke.cvCreateImage(roisize, IplDepth.IplDepth_8U, 3);
        //        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(
        //            0,//beginWidthHeight.Item1,
        //            0,//beginWidthHeight.Item2,
        //            lengthWidthHeight.Item1,//+beginWidthHeight.Item1, 
        //            lengthWidthHeight.Item2);//+beginWidthHeight.Item2);
        //        CvInvoke.cvSetImageROI(image.Ptr, rect);
        //        CvInvoke.cvCopy(image.Ptr, dst, IntPtr.Zero);
        //        return OpenCVEmguCVDotNet.IplImagePointerToEmgucvImage<Bgr, Byte>(dst);
        //    }
        //    catch (Exception e)
        //    {

        //        throw e;
        //    }




        //}



    }
    public class OpenCVEmguCVDotNet
    {
        /// <summary>
        /// 将MIplImage结构转换到IplImage指针；
        /// 注意：指针在使用完之后必须用Marshal.FreeHGlobal方法释放。
        /// </summary>
        /// <param name="mi">MIplImage对象</param>
        /// <returns>返回IplImage指针</returns>
        public static IntPtr MIplImageToIplImagePointer(MIplImage mi)
        {

            IntPtr ptr = Marshal.AllocHGlobal(mi.NSize);
            Marshal.StructureToPtr(mi, ptr, false);
            return ptr;
        }

        /// <summary>
        /// 将IplImage指针转换成MIplImage结构
        /// </summary>
        /// <param name="ptr">IplImage指针</param>
        /// <returns>返回MIplImage结构</returns>
        public static MIplImage IplImagePointerToMIplImage(IntPtr ptr)
        {
            return (MIplImage)Marshal.PtrToStructure(ptr, typeof(MIplImage));
        }

        /// <summary>
        /// 将IplImage指针转换成Emgucv中的Image对象；
        /// 注意：这里需要您自己根据IplImage中的depth和nChannels来决定
        /// </summary>
        /// <typeparam name="TColor">Color type of this image (either Gray, Bgr, Bgra, Hsv, Hls, Lab, Luv, Xyz or Ycc)</typeparam>
        /// <typeparam name="TDepth">Depth of this image (either Byte, SByte, Single, double, UInt16, Int16 or Int32)</typeparam>
        /// <param name="ptr">IplImage指针</param>
        /// <returns>返回Image对象</returns>
        public static Image<TColor, TDepth> IplImagePointerToEmgucvImage<TColor, TDepth>(IntPtr ptr)
            where TColor : struct, IColor
            where TDepth : new()
        {
            MIplImage mi = IplImagePointerToMIplImage(ptr);
            return new Image<TColor, TDepth>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
        }

        /// <summary>
        /// 将IplImage指针转换成Emgucv中的IImage接口；
        /// 1通道对应灰度图像，3通道对应BGR图像，4通道对应BGRA图像。
        /// 注意：3通道可能并非BGR图像，而是HLS,HSV等图像
        /// </summary>
        /// <param name="ptr">IplImage指针</param>
        /// <returns>返回IImage接口</returns>
        public static IImage IplImagePointToEmgucvIImage(IntPtr ptr)
        {
            MIplImage mi = IplImagePointerToMIplImage(ptr);
            Type tColor;
            Type tDepth;
            string unsupportedDepth = "不支持的像素位深度IPL_DEPTH";
            string unsupportedChannels = "不支持的通道数（仅支持1，2，4通道）";
            switch (mi.NChannels)
            {
                case 1:
                    tColor = typeof(Gray);
                    switch (mi.Depth)
                    {
                        case IplDepth.IplDepth_8U:
                            tDepth = typeof(Byte);
                            return new Image<Gray, Byte>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth16U:
                            tDepth = typeof(UInt16);
                            return new Image<Gray, UInt16>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth16S:
                            tDepth = typeof(Int16);
                            return new Image<Gray, Int16>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth32S:
                            tDepth = typeof(Int32);
                            return new Image<Gray, Int32>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth32F:
                            tDepth = typeof(Single);
                            return new Image<Gray, Single>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth64F:
                            tDepth = typeof(Double);
                            return new Image<Gray, Double>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        default:
                            throw new NotImplementedException(unsupportedDepth);
                    }
                case 3:
                    tColor = typeof(Bgr);
                    switch (mi.Depth)
                    {
                        case IplDepth.IplDepth_8U:
                            tDepth = typeof(Byte);
                            return new Image<Bgr, Byte>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth16U:
                            tDepth = typeof(UInt16);
                            return new Image<Bgr, UInt16>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth16S:
                            tDepth = typeof(Int16);
                            return new Image<Bgr, Int16>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth32S:
                            tDepth = typeof(Int32);
                            return new Image<Bgr, Int32>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth32F:
                            tDepth = typeof(Single);
                            return new Image<Bgr, Single>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth64F:
                            tDepth = typeof(Double);
                            return new Image<Bgr, Double>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        default:
                            throw new NotImplementedException(unsupportedDepth);
                    }
                case 4:
                    tColor = typeof(Bgra);
                    switch (mi.Depth)
                    {
                        case IplDepth.IplDepth_8U:
                            tDepth = typeof(Byte);
                            return new Image<Bgra, Byte>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth16U:
                            tDepth = typeof(UInt16);
                            return new Image<Bgra, UInt16>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth16S:
                            tDepth = typeof(Int16);
                            return new Image<Bgra, Int16>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth32S:
                            tDepth = typeof(Int32);
                            return new Image<Bgra, Int32>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth32F:
                            tDepth = typeof(Single);
                            return new Image<Bgra, Single>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        case IplDepth.IplDepth64F:
                            tDepth = typeof(Double);
                            return new Image<Bgra, Double>(mi.Width, mi.Height, mi.WidthStep, mi.ImageData);
                        default:
                            throw new NotImplementedException(unsupportedDepth);
                    }
                default:
                    throw new NotImplementedException(unsupportedChannels);
            }
        }

        /// <summary>
        /// 将Emgucv中的Image对象转换成IplImage指针；
        /// </summary>
        /// <typeparam name="TColor">Color type of this image (either Gray, Bgr, Bgra, Hsv, Hls, Lab, Luv, Xyz or Ycc)</typeparam>
        /// <typeparam name="TDepth">Depth of this image (either Byte, SByte, Single, double, UInt16, Int16 or Int32)</typeparam>
        /// <param name="image">Image对象</param>
        /// <returns>返回IplImage指针</returns>
        public static IntPtr EmgucvImageToIplImagePointer<TColor, TDepth>(Image<TColor, TDepth> image)
            where TColor : struct, IColor
            where TDepth : new()
        {
            return image.Ptr;
        }

        /// <summary>
        /// 将IplImage指针转换成位图对象；
        /// 对于不支持的像素格式，可以先使用cvCvtColor函数转换成支持的图像指针
        /// </summary>
        /// <param name="ptr">IplImage指针</param>
        /// <returns>返回位图对象</returns>
        public static Bitmap IplImagePointerToBitmap(IntPtr ptr)
        {
            MIplImage mi = IplImagePointerToMIplImage(ptr);
            PixelFormat pixelFormat;    //像素格式
            string unsupportedDepth = "不支持的像素位深度IPL_DEPTH";
            string unsupportedChannels = "不支持的通道数（仅支持1，2，4通道）";
            switch (mi.NChannels)
            {
                case 1:
                    switch (mi.Depth)
                    {
                        case IplDepth.IplDepth_8U:
                            pixelFormat = PixelFormat.Format8bppIndexed;
                            break;
                        case IplDepth.IplDepth16U:
                            pixelFormat = PixelFormat.Format16bppGrayScale;
                            break;
                        default:
                            throw new NotImplementedException(unsupportedDepth);
                    }
                    break;
                case 3:
                    switch (mi.Depth)
                    {
                        case IplDepth.IplDepth_8U:
                            pixelFormat = PixelFormat.Format24bppRgb;
                            break;
                        case IplDepth.IplDepth16U:
                            pixelFormat = PixelFormat.Format48bppRgb;
                            break;
                        default:
                            throw new NotImplementedException(unsupportedDepth);
                    }
                    break;
                case 4:
                    switch (mi.Depth)
                    {
                        case IplDepth.IplDepth_8U:
                            pixelFormat = PixelFormat.Format32bppArgb;
                            break;
                        case IplDepth.IplDepth16U:
                            pixelFormat = PixelFormat.Format64bppArgb;
                            break;
                        default:
                            throw new NotImplementedException(unsupportedDepth);
                    }
                    break;
                default:
                    throw new NotImplementedException(unsupportedChannels);

            }
            Bitmap bitmap = new Bitmap(mi.Width, mi.Height, mi.WidthStep, pixelFormat, mi.ImageData);
            //对于灰度图像，还要修改调色板
            if (pixelFormat == PixelFormat.Format8bppIndexed)
                SetColorPaletteOfGrayscaleBitmap(bitmap);
            return bitmap;
        }

        /// <summary>
        /// 将位图转换成IplImage指针
        /// </summary>
        /// <param name="bitmap">位图对象</param>
        /// <returns>返回IplImage指针</returns>
        public static IntPtr BitmapToIplImagePointer(Bitmap bitmap)
        {
            IImage iimage = null;
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    iimage = new Image<Gray, Byte>(bitmap);
                    break;
                case PixelFormat.Format16bppGrayScale:
                    iimage = new Image<Gray, UInt16>(bitmap);
                    break;
                case PixelFormat.Format24bppRgb:
                    iimage = new Image<Bgr, Byte>(bitmap);
                    break;
                case PixelFormat.Format32bppArgb:
                    iimage = new Image<Bgra, Byte>(bitmap);
                    break;
                case PixelFormat.Format48bppRgb:
                    iimage = new Image<Bgr, UInt16>(bitmap);
                    break;
                case PixelFormat.Format64bppArgb:
                    iimage = new Image<Bgra, UInt16>(bitmap);
                    break;
                default:
                    Image<Bgra, Byte> tmp1 = new Image<Bgra, Byte>(bitmap.Size);
                    Byte[,,] data = tmp1.Data;
                    for (int i = 0; i < bitmap.Width; i++)
                    {
                        for (int j = 0; j < bitmap.Height; j++)
                        {
                            Color color = bitmap.GetPixel(i, j);
                            data[j, i, 0] = color.B;
                            data[j, i, 1] = color.G;
                            data[j, i, 2] = color.R;
                            data[j, i, 3] = color.A;
                        }
                    }
                    iimage = tmp1;
                    break;
            }
            return iimage.Ptr;
        }

        /// <summary>
        /// 设置256级灰度位图的调色板
        /// </summary>
        /// <param name="bitmap"></param>
        public static void SetColorPaletteOfGrayscaleBitmap(Bitmap bitmap)
        {
            PixelFormat pixelFormat = bitmap.PixelFormat;
            if (pixelFormat == PixelFormat.Format8bppIndexed)
            {
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i < palette.Entries.Length; i++)
                    palette.Entries[i] = Color.FromArgb(255, i, i, i);
                bitmap.Palette = palette;
            }
        }
    }
    public static class BitmapHelper
    {
        public static Image<Bgr, byte> AsImage(this Bitmap bmp)
        {
            return new Image<Bgr, byte>(bmp);
        }


        public static List<Bitmap> Cut(this Bitmap bmp)
        {

            var result = new List<Bitmap>();
            for (int i = 0; i < 4; i++)
            {

                var beginSplitWidthHeight = new Tuple<int, int>((i * 20 + 10) - 3, 0);
                var lengthWidthHeight = new Tuple<int, int>(20 + 6, 32);
                var charBmp = bmp.Split(beginSplitWidthHeight, lengthWidthHeight);
                result.Add(charBmp);
            }
            return result;
        }

        /// <summary>
        /// 分割图片
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="beginSplitWidthHeight"></param>
        /// <param name="lengthWidthHeight"></param>
        /// <returns></returns>
        public static Bitmap Split(this Bitmap bmp, Tuple<int, int> beginSplitWidthHeight, Tuple<int, int> lengthWidthHeight)
        {
            Rectangle rect = new Rectangle(beginSplitWidthHeight.Item1, beginSplitWidthHeight.Item2,
                lengthWidthHeight.Item1, lengthWidthHeight.Item2);
            var splitBmp = bmp.Clone(rect, bmp.PixelFormat);
            return splitBmp;
        }
        /// <summary>
        /// 分割图片并保存
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="beginSplitWidthHeight"></param>
        /// <param name="lengthWidthHeight"></param>
        /// <param name="targetPath"></param>
        public static void Split(this Bitmap bmp, Tuple<int, int> beginSplitWidthHeight, Tuple<int, int> lengthWidthHeight, string targetPath)
        {
            var result = bmp.Split(beginSplitWidthHeight, lengthWidthHeight);
            result.Save(targetPath);
        }

        /// <summary>
        /// 调用此函数即可实现提取图像骨架
        /// </summary>
        /// <param name="imageSrcPath"></param>
        /// <param name="imageDestPath"></param>
        public static void getThinPicture(this Bitmap bmp, string imageDestPath)
        {
            var bmpThin = bmp.getThinPicture();

            bmpThin.Save(imageDestPath, System.Drawing.Imaging.ImageFormat.Jpeg);
        }


        /// <summary>
        /// 调用此函数即可实现提取图像骨架
        /// </summary>
        /// <param name="imageSrcPath"></param>
        /// <param name="imageDestPath"></param>
        public static Bitmap getThinPicture(this Bitmap bmp)
        {
            //Bitmap bmp = new Bitmap(imageSrcPath);

            int Threshold = 0;

            Byte[,] m_SourceImage = bmp.ToBinaryArray(out Threshold);

            Byte[,] m_DesImage = m_SourceImage.ThinPicture();

            Bitmap bmpThin = m_DesImage.ToBinaryBitmap();

            return bmpThin;
        }

        //public static string ToOCRString(this Bitmap bmp)
        //{

        //    tessnet2.Tesseract ocr = new tessnet2.Tesseract();//声明一个OCR类   
        //    string txt = "";
        //    List<tessnet2.Word> result = new List<tessnet2.Word>();
        //    var visibleChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        //    try
        //    {//当前识别变量设置：数字与大写字母，这种写法会导致无法识别小写字母，加上小写字母即可
        //        ocr.SetVariable("tessedit_char_whitelist", visibleChars);
        //        //应用当前语言包。注，Tessnet2是支持多国语的。语言包下载链接：http://code.google.com/p/tesseract-ocr/downloads/list
        //        //D:\CSharp\TessnetTesttessdata是语言包在电脑中的路径
        //        ocr.Init(@"H:\tessdata", "eng", false);

        //        result = ocr.DoOCR(bmp, Rectangle.Empty);
        //        foreach (tessnet2.Word word in result)
        //        {
        //            foreach (var c in word.CharList)
        //            {
        //                if (visibleChars.Contains(c.Value.ToString()))
        //                    txt += c.Value;
        //            }
        //        }
        //        return txt;

        //    }
        //    catch (Exception ex)
        //    {
        //        return string.Empty;
        //    }
        //}

        /// <summary>
        /// 全局阈值图像二值化
        /// </summary>
        /// <param name="bmp">原始图像</param>
        /// <param name="method">二值化方法</param>
        /// <param name="threshold">输出：全局阈值</param>
        /// <returns>二值化后的图像数组</returns>       
        public static Byte[,] ToBinaryArray(this Bitmap bmp, out Int32 threshold)
        {   // 位图转换为灰度数组
            Byte[,] GrayArray = bmp.ToGrayArray();

            // 计算全局阈值
            threshold = GrayArray.OtsuThreshold();

            // 根据阈值进行二值化
            Int32 PixelHeight = bmp.Height;
            Int32 PixelWidth = bmp.Width;
            Byte[,] BinaryArray = new Byte[PixelHeight, PixelWidth];
            for (Int32 i = 0; i < PixelHeight; i++)
            {
                for (Int32 j = 0; j < PixelWidth; j++)
                {
                    BinaryArray[i, j] = Convert.ToByte((GrayArray[i, j] > threshold) ? 255 : 0);
                }
            }

            return BinaryArray;
        }

        /// <summary>
        /// 将位图转换为灰度数组（256级灰度）
        /// </summary>
        /// <param name="bmp">原始位图</param>
        /// <returns>灰度数组</returns>
        public static Byte[,] ToGrayArray(this Bitmap bmp)
        {
            Int32 PixelHeight = bmp.Height; // 图像高度
            Int32 PixelWidth = bmp.Width;   // 图像宽度
            Int32 Stride = ((PixelWidth * 3 + 3) >> 2) << 2;    // 跨距宽度
            Byte[] Pixels = new Byte[PixelHeight * Stride];

            // 锁定位图到系统内存
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, PixelWidth, PixelHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            Marshal.Copy(bmpData.Scan0, Pixels, 0, Pixels.Length);  // 从非托管内存拷贝数据到托管内存
            bmp.UnlockBits(bmpData);    // 从系统内存解锁位图

            // 将像素数据转换为灰度数组
            Byte[,] GrayArray = new Byte[PixelHeight, PixelWidth];
            for (Int32 i = 0; i < PixelHeight; i++)
            {
                Int32 Index = i * Stride;
                for (Int32 j = 0; j < PixelWidth; j++)
                {
                    GrayArray[i, j] = Convert.ToByte((Pixels[Index + 2] * 19595 + Pixels[Index + 1] * 38469 + Pixels[Index] * 7471 + 32768) >> 16);
                    Index += 3;
                }
            }

            return GrayArray;
        }
    }
    public static class ByteArrayHelper
    {
        public static int B(this Byte[,] picture, int x, int y)
        {
            return picture[x, y - 1] + picture[x + 1, y - 1] + picture[x + 1, y] + picture[x + 1, y + 1] +
                   picture[x, y + 1] + picture[x - 1, y + 1] + picture[x - 1, y] + picture[x - 1, y - 1];
        }

        public static int A(this Byte[,] picture, int x, int y)
        {
            int counter = 0;
            if ((picture[x, y - 1] == 0) && (picture[x + 1, y - 1] == 1))
            {
                counter++;
            }
            if ((picture[x + 1, y - 1] == 0) && (picture[x + 1, y] == 1))
            {
                counter++;
            }
            if ((picture[x + 1, y] == 0) && (picture[x + 1, y + 1] == 1))
            {
                counter++;
            }
            if ((picture[x + 1, y + 1] == 0) && (picture[x, y + 1] == 1))
            {
                counter++;
            }
            if ((picture[x, y + 1] == 0) && (picture[x - 1, y + 1] == 1))
            {
                counter++;
            }
            if ((picture[x - 1, y + 1] == 0) && (picture[x - 1, y] == 1))
            {
                counter++;
            }
            if ((picture[x - 1, y] == 0) && (picture[x - 1, y - 1] == 1))
            {
                counter++;
            }
            if ((picture[x - 1, y - 1] == 0) && (picture[x, y - 1] == 1))
            {
                counter++;
            }
            return counter;
        }

        public static Byte[,] ThinPicture(this Byte[,] newPicture)
        {

            Byte[,] picture = new Byte[newPicture.GetLength(0) + 2, newPicture.GetLength(1) + 2];
            Byte[,] pictureToRemove = new Byte[newPicture.GetLength(0) + 2, newPicture.GetLength(1) + 2];
            bool hasChanged;
            for (int i = 0; i < picture.GetLength(1); i++)
            {
                for (int j = 0; j < picture.GetLength(0); j++)
                {
                    picture[j, i] = 255;
                    pictureToRemove[j, i] = 0;
                }
            }

            for (int i = 0; i < newPicture.GetLength(1); i++)
            {
                for (int j = 0; j < newPicture.GetLength(0); j++)
                {
                    picture[j + 1, i + 1] = newPicture[j, i];
                }
            }

            for (int i = 0; i < picture.GetLength(1); i++)
            {
                for (int j = 0; j < picture.GetLength(0); j++)
                {
                    picture[j, i] = picture[j, i] == 0 ? picture[j, i] = 1 : picture[j, i] = 0;
                }
            }
            do
            {
                hasChanged = false;
                for (int i = 0; i < newPicture.GetLength(1); i++)
                {
                    for (int j = 0; j < newPicture.GetLength(0); j++)
                    {
                        //if ((picture[j, i] == 1) && (2 <= B(picture, j, i)) && (B(picture, j, i) <= 6) && (A(picture, j, i) == 1) &&
                        //    (picture[j, i - 1] * picture[j + 1, i] * picture[j, i + 1] == 0) &&
                        //    (picture[j + 1, i] * picture[j, i + 1] * picture[j - 1, i] == 0))
                        if ((picture[j, i] == 1) && (2 <= picture.B(j, i)) && (picture.B(j, i) <= 6) && (picture.A(j, i) == 1) &&
                            (picture[j, i - 1] * picture[j + 1, i] * picture[j, i + 1] == 0) &&
                            (picture[j + 1, i] * picture[j, i + 1] * picture[j - 1, i] == 0))
                        {
                            pictureToRemove[j, i] = 1;
                            hasChanged = true;
                        }
                    }
                }
                for (int i = 0; i < newPicture.GetLength(1); i++)
                {
                    for (int j = 0; j < newPicture.GetLength(0); j++)
                    {
                        if (pictureToRemove[j, i] == 1)
                        {
                            picture[j, i] = 0;
                            pictureToRemove[j, i] = 0;
                        }
                    }
                }
                for (int i = 0; i < newPicture.GetLength(1); i++)
                {
                    for (int j = 0; j < newPicture.GetLength(0); j++)
                    {
                        //if ((picture[j, i] == 1) && (2 <= B(picture, j, i)) && (B(picture, j, i) <= 6) &&
                        //    (A(picture, j, i) == 1) &&
                        //    (picture[j, i - 1] * picture[j + 1, i] * picture[j - 1, i] == 0) &&
                        //    (picture[j, i - 1] * picture[j, i + 1] * picture[j - 1, i] == 0))
                        if ((picture[j, i] == 1) && (2 <= picture.B(j, i)) && (picture.B(j, i) <= 6) &&
                              (picture.A(j, i) == 1) &&
                              (picture[j, i - 1] * picture[j + 1, i] * picture[j - 1, i] == 0) &&
                              (picture[j, i - 1] * picture[j, i + 1] * picture[j - 1, i] == 0))
                        {
                            pictureToRemove[j, i] = 1;
                            hasChanged = true;
                        }
                    }
                }

                for (int i = 0; i < newPicture.GetLength(1); i++)
                {
                    for (int j = 0; j < newPicture.GetLength(0); j++)
                    {
                        if (pictureToRemove[j, i] == 1)
                        {
                            picture[j, i] = 0;
                            pictureToRemove[j, i] = 0;
                        }
                    }
                }
            } while (hasChanged);

            for (int i = 0; i < newPicture.GetLength(1); i++)
            {
                for (int j = 0; j < newPicture.GetLength(0); j++)
                {
                    if ((picture[j, i] == 1) &&
                        (((picture[j, i - 1] * picture[j + 1, i] == 1) && (picture[j - 1, i + 1] != 1)) || ((picture[j + 1, i] * picture[j, i + 1] == 1) && (picture[j - 1, i - 1] != 1)) ||      //Небольшая модификцаия алгоритма для ещё большего утоньшения
                        ((picture[j, i + 1] * picture[j - 1, i] == 1) && (picture[j + 1, i - 1] != 1)) || ((picture[j, i - 1] * picture[j - 1, i] == 1) && (picture[j + 1, i + 1] != 1))))
                    {
                        picture[j, i] = 0;
                    }
                }
            }

            for (int i = 0; i < picture.GetLength(1); i++)
            {
                for (int j = 0; j < picture.GetLength(0); j++)
                {
                    // picture[j, i] = picture[j, i] == 0 ? 255 : 0;      
                    if (0 == picture[j, i])
                    {
                        picture[j, i] = 255;
                    }
                    else
                    {
                        picture[j, i] = 0;
                    }
                }
            }

            Byte[,] outPicture = new Byte[newPicture.GetLength(0), newPicture.GetLength(1)];

            for (int i = 0; i < newPicture.GetLength(1); i++)
            {
                for (int j = 0; j < newPicture.GetLength(0); j++)
                {
                    outPicture[j, i] = picture[j + 1, i + 1];
                }
            }
            return outPicture;
        }

        /// <summary>
        /// 将二值化数组转换为二值化图像
        /// </summary>
        /// <param name="binaryArray">二值化数组</param>
        /// <returns>二值化图像</returns>
        public static Bitmap ToBinaryBitmap(this Byte[,] binaryArray)
        {   // 将二值化数组转换为二值化数据
            Int32 PixelHeight = binaryArray.GetLength(0);
            Int32 PixelWidth = binaryArray.GetLength(1);
            Int32 Stride = ((PixelWidth + 31) >> 5) << 2;
            Byte[] Pixels = new Byte[PixelHeight * Stride];
            for (Int32 i = 0; i < PixelHeight; i++)
            {
                Int32 Base = i * Stride;
                for (Int32 j = 0; j < PixelWidth; j++)
                {
                    if (binaryArray[i, j] != 0)
                    {
                        Pixels[Base + (j >> 3)] |= Convert.ToByte(0x80 >> (j & 0x7));
                    }
                }
            }

            // 创建黑白图像
            Bitmap BinaryBmp = new Bitmap(PixelWidth, PixelHeight, PixelFormat.Format1bppIndexed);

            // 设置调色表
            ColorPalette cp = BinaryBmp.Palette;
            cp.Entries[0] = Color.Black;    // 黑色
            cp.Entries[1] = Color.White;    // 白色
            BinaryBmp.Palette = cp;

            // 设置位图图像特性
            BitmapData BinaryBmpData = BinaryBmp.LockBits(new Rectangle(0, 0, PixelWidth, PixelHeight), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            Marshal.Copy(Pixels, 0, BinaryBmpData.Scan0, Pixels.Length);
            BinaryBmp.UnlockBits(BinaryBmpData);

            return BinaryBmp;
        }


        /// <summary>
        /// 检测非零值
        /// </summary>
        /// <param name="value">要检测的数值</param>
        /// <returns>
        ///     true：非零
        ///     false：零
        /// </returns>
        private static Boolean NonZero(Int32 value)
        {
            return (value != 0) ? true : false;
        }

        /// <summary>
        /// 大津法计算阈值
        /// </summary>
        /// <param name="grayArray">灰度数组</param>
        /// <returns>二值化阈值</returns>
        public static Int32 OtsuThreshold(this Byte[,] grayArray)
        {   // 建立统计直方图
            Int32[] Histogram = new Int32[256];
            Array.Clear(Histogram, 0, 256);     // 初始化
            foreach (Byte b in grayArray)
            {
                Histogram[b]++;                 // 统计直方图
            }

            // 总的质量矩和图像点数
            Int32 SumC = grayArray.Length;    // 总的图像点数
            Double SumU = 0;                  // 双精度避免方差运算中数据溢出
            for (Int32 i = 1; i < 256; i++)
            {
                SumU += i * Histogram[i];     // 总的质量矩               
            }

            // 灰度区间
            Int32 MinGrayLevel = Array.FindIndex(Histogram, NonZero);       // 最小灰度值
            Int32 MaxGrayLevel = Array.FindLastIndex(Histogram, NonZero);   // 最大灰度值

            // 计算最大类间方差
            Int32 Threshold = MinGrayLevel;
            Double MaxVariance = 0.0;       // 初始最大方差
            Double U0 = 0;                  // 初始目标质量矩
            Int32 C0 = 0;                   // 初始目标点数
            for (Int32 i = MinGrayLevel; i < MaxGrayLevel; i++)
            {
                if (Histogram[i] == 0) continue;

                // 目标的质量矩和点数               
                U0 += i * Histogram[i];
                C0 += Histogram[i];

                // 计算目标和背景的类间方差
                Double Diference = U0 * SumC - SumU * C0;
                Double Variance = Diference * Diference / C0 / (SumC - C0); // 方差
                if (Variance > MaxVariance)
                {
                    MaxVariance = Variance;
                    Threshold = i;
                }
            }

            // 返回类间方差最大阈值
            return Threshold;
        }
    }
}
