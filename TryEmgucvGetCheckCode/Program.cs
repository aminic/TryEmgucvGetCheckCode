using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace TryEmgucvGetCheckCode
{
    class Program
    {
        static void Main(string[] args)
        {
            var checkCodePath = @"H:\GuoYuanCheckCode\GuaYuanLoginCheckCodePics";
            var checkCodeFiles = Directory.GetFiles(checkCodePath, "????.jpg");
            var charTemplateFilePath = @"H:\GuoYuanCheckCode\CharTemplatePics_GOOD";
            var charTemplateFiles = Directory.GetFiles(charTemplateFilePath, "?.jpg");
            var charTemplateImages = BuildCharTemplateImages(charTemplateFiles);
            var okCount = 0;
            //循环每一个验证码文件
            foreach (var checkCodeFile in checkCodeFiles)
            {
                var codeImage = new Bitmap(checkCodeFile);
                var fileCode = checkCodeFile.Substring(checkCodeFile.LastIndexOf(@"\") + 1, 4);
                var compareCode = string.Empty;
                var cutCharBmpList = codeImage.Cut();
                //循环每一个切出来的字符图片
                cutCharBmpList.ForEach((charbmp) =>
                {
                    Tuple<char, double> selectedInfo = new Tuple<char, double>(' ', -10);
                    //循环每一个字符模板图片
                    charTemplateImages.ToList().ForEach((kv) =>
                    {
                        //var charResult = charbmp.getThinPicture().AsImage().CompareTemplate(kv.Value);
                        var charResult = charbmp.AsImage().CompareTemplate(kv.Value);
                        if (charResult.MaxValues[0] > selectedInfo.Item2)
                        {
                            selectedInfo = new Tuple<char, double>(kv.Key, charResult.MaxValues[0]);
                        }
                    });
                    compareCode += selectedInfo.Item1;
                });

                if (fileCode == compareCode)
                {
                    //Console.WriteLine("=======================    OK");
                    okCount++;
                }
                else
                {
                    Console.WriteLine($"file    code: {fileCode}");
                    Console.WriteLine($"compare code: {compareCode}");
                    Console.WriteLine("#####################################    ERROR");
                }

            }
            Console.WriteLine($"样本总量: {checkCodeFiles.Length}");
            Console.WriteLine($"成功数量: {okCount}");
            var per = double.Parse(okCount.ToString()) / checkCodeFiles.Length;
            Console.WriteLine($"成功率:{per}");

            Console.WriteLine("====-END-====");
            Console.Read();

        }

        private static Dictionary<char, Image<Bgr, byte>> BuildCharTemplateImages(string[] charTemplateFiles)
        {
            var result = new Dictionary<char, Image<Bgr, byte>>();
            foreach (var filePath in charTemplateFiles)
            {
                //var charImage = new Bitmap(filePath).getThinPicture().AsImage();
                var charImage = new Image<Bgr, byte>(filePath);
                var c = filePath.Substring(filePath.LastIndexOf(@"\") + 1, 1).ToCharArray()[0];
                result.Add(c, charImage);
            }
            return result;
        }
    }
}
