using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuintenOCR
{
    class Program
    {
        /// <summary>
        /// 검출된 세그먼트의 영역 정보
        /// </summary>
        class Segment
        {
            public double x;
            public double y;
            public double width;
            public double height;
        }

        /// <summary>
        /// Seven Segment Display 숫자별 표현
        /// </summary>
        static int[,] numberSegments = new int[10, 7] {
                {1, 1, 1, 0, 1, 1, 1}, //0
                {0, 0, 1, 0, 0, 1, 0}, //1
                {1, 0, 1, 1, 1, 0, 1}, //2
                {1, 0, 1, 1, 0, 1, 1}, //3
                {0, 1, 1, 1, 0, 1, 0}, //4
                {1, 1, 0, 1, 0, 1, 1}, //5
                {1, 1, 0, 1, 1, 1, 1}, //6
                {1, 1, 1, 0, 0, 1, 0}, //7
                {1, 1, 1, 1, 1, 1, 1}, //8
                {1, 1, 1, 1, 0, 1, 1}  //9
                };
               

        static int Main(string[] args)
        {
            int result = 0; //성공 여부 0:실패 1:성공
            string resultOCR = ""; //Tesseract 결과
            //string numeric = ""; //필터링된 숫자

            List<string> resultTesser = new List<string>();

            Mat mtSrc = new Mat();


            try
            {
                int.TryParse(ConfigurationManager.AppSettings["RunMaximum"], out int runMaximum);
                int.TryParse(ConfigurationManager.AppSettings["DetectWidth"], out int detectWidth);
                int.TryParse(ConfigurationManager.AppSettings["DetectHeight"], out int detectHeight);
                int.TryParse(ConfigurationManager.AppSettings["ThresholdValue"], out int thresholdValue);
                int.TryParse(ConfigurationManager.AppSettings["DeviceNumber"], out int deviceNumber);
                int.TryParse(ConfigurationManager.AppSettings["DeleteWidth"], out int delWidth);


                if (runMaximum < 1 || detectWidth < 1 || detectHeight < 1 || thresholdValue < 1 ||
                    string.IsNullOrEmpty(ConfigurationManager.AppSettings["PathResult"]) ||
                    string.IsNullOrEmpty(ConfigurationManager.AppSettings["PathSuccess"]) ||
                    string.IsNullOrEmpty(ConfigurationManager.AppSettings["PathFail"]) ||
                    string.IsNullOrEmpty(ConfigurationManager.AppSettings["PathException"])
                    )
                    throw new ConfigurationErrorsException("config 파일 설정값 오류");

                DirectoryInfo diResult = new DirectoryInfo(ConfigurationManager.AppSettings["PathResult"]);
                if (diResult.Exists == false)
                    diResult.Create();

                //result 폴더 하위 파일 지우기
                foreach (var file in diResult.GetFiles())
                {
                    File.Delete(string.Format("{0}\\{1}", diResult.Name, file));
                }

                //OpenCV 캡처 초기화
                VideoCapture capture = VideoCapture.FromCamera(CaptureDevice.Any, deviceNumber);

                for (int i = 1; i <= runMaximum; i++)
                {
                    resultOCR = "";

                    //화상을 Matrix 에 로드
                    capture.Read(mtSrc);

                    //영역 검출
                    MSER mser = MSER.Create();
                    OpenCvSharp.Point[][] contours;
                    OpenCvSharp.Rect[] bboxes;
                    mser.DetectRegions(mtSrc, out contours, out bboxes); //DetectRegion 은 Canny 가 무용지물                    

                    //Smoothing 
                    Cv2.MedianBlur(mtSrc, mtSrc, 3);

                    //색 변환
                    Cv2.CvtColor(mtSrc, mtSrc, ColorConversionCodes.BGR2GRAY);

                    //검출 결과 필터링
                    var filteredBboxes = bboxes.Where(
                            r =>
                            r.Width >= detectWidth - 3 &&
                            r.Width <= detectWidth + 5 &&
                            r.Height >= detectHeight - 5 &&
                            r.Height <= detectHeight + 5
                            );

                    //if (filteredBboxes.Count() < 1)
                    //    Console.WriteLine("none.");

                    //var orderedBboxes = filteredBboxes.OrderBy(r => r.Width);

                    foreach (var rect in filteredBboxes)
                    {
                        resultOCR = "";

                        Rect rectTemp = rect;
                        rectTemp.X = rect.X + delWidth;
                        rectTemp.Width = rect.Width - delWidth;

                        //rect 영역 crop
                        Mat mtCrop = mtSrc[rectTemp];

                        resultOCR = Recognize(mtCrop, thresholdValue, rectTemp);

                        //재시도 ( 앞부분 크롭 영역 -1 조정)
                        if (resultOCR.Length < 6 || resultOCR.Contains("_"))
                        {
                            rectTemp.X = rect.X + delWidth - 1;
                            rectTemp.Width = rect.Width - delWidth + 1;
                            mtCrop = mtSrc[rectTemp];
                            resultOCR = Recognize(mtCrop, thresholdValue, rectTemp);
                        }
                        //3차시도 ( 앞부분 크롭 영역 +1 조정)
                        if (resultOCR.Length < 6 || resultOCR.Contains("_"))
                        {
                            rectTemp.X = rect.X + delWidth + 1;
                            rectTemp.Width = rect.Width - delWidth - 1;
                            mtCrop = mtSrc[rectTemp];
                            resultOCR = Recognize(mtCrop, thresholdValue, rectTemp);
                        }
                        //4차
                        if (resultOCR.Length < 6 || resultOCR.Contains("_"))
                        {
                            rectTemp.X = rect.X + delWidth - 2;
                            rectTemp.Width = rect.Width - delWidth + 2;
                            mtCrop = mtSrc[rectTemp];
                            resultOCR = Recognize(mtCrop, thresholdValue, rectTemp);
                        }
                        //5차시도 ( 앞부분 크롭 영역 +2 조정)
                        if (resultOCR.Length < 6 || resultOCR.Contains("_"))
                        {
                            rectTemp.X = rect.X + delWidth + 2;
                            rectTemp.Width = rect.Width - delWidth - 2;
                            mtCrop = mtSrc[rectTemp];
                            resultOCR = Recognize(mtCrop, thresholdValue, rectTemp);
                        }
                        if (resultOCR.Length == 6 && resultOCR.Contains("_") == false)
                        {
                            result = 1; //성공

                            if (ConfigurationManager.AppSettings["RunMode"].ToLower() == "d")
                            {
                                //Console.WriteLine(string.Format("width : {0} height : {1}", rect.Width, rect.Height));
                                Console.WriteLine(string.Format("{0}\t({1})", resultOCR, i));
                                //Cv2.ImShow("mtCrop", mtCrop);
                                //Cv2.WaitKey(0);
                                //Cv2.DestroyWindow("seg");
                            }
                            break;
                        }
                    } // foreach                    

                    if (result == 1)
                        break;

                    //if (numeric.Length == 0)
                    //{
                    //    foreach (var rect in bboxes)
                    //    {
                    //        Scalar color = Scalar.RandomColor();
                    //        mtSrc.Rectangle(rect, color);
                    //    }
                    //}

                    //Cv2.ImShow(filename, mtSrc);
                    //Cv2.ImShow("clone", mtSrc);
                    //Cv2.WaitKey(0);
                    //Thread.Sleep(300);
                } //for runMaximum


                if (result == 1)
                {
                    //result = 1; //성공 여부

                    //이미지 저장
                    mtSrc.SaveImage(string.Format("{0}\\{1}.png", diResult.Name, resultOCR));

                    //이미지 사본 복사
                    DirectoryInfo diSuccess = new DirectoryInfo(ConfigurationManager.AppSettings["PathSuccess"]);
                    if (diSuccess.Exists == false)
                        diSuccess.Create();

                    string filename = resultOCR;
                    if (File.Exists(string.Format("{0}\\{1}.png", diSuccess.Name, filename)))
                        filename = resultOCR + "_" + DateTime.Now.ToString("yyMMddHHmmss");

                    File.Copy(string.Format("{0}\\{1}.png", diResult.Name, resultOCR), string.Format("{0}\\{1}.png", diSuccess.Name, filename));

                    //Console.WriteLine(numeric);
                }
                else
                {
                    //실패 이미지 저장
                    DirectoryInfo diFail = new DirectoryInfo(ConfigurationManager.AppSettings["PathFail"]);
                    if (diFail.Exists == false)
                        diFail.Create();

                    mtSrc.SaveImage(string.Format("{0}\\{1}.png", diFail.Name, DateTime.Now.ToString("yyMMddHHmmss")));
                }
            }
            catch (Exception ex)
            {
                //Exception 저장
                DirectoryInfo diEx = new DirectoryInfo(ConfigurationManager.AppSettings["PathException"]);

                if (diEx.Exists == false)
                    diEx.Create();

                File.WriteAllText(string.Format("{0}\\{1}.txt", diEx.Name, DateTime.Now.ToString("yyMMddHHmmss")), ex.ToString());

                //담당자에게 alert 전송                
            }
            finally
            {
            }

            //Console.ReadKey();

            return result;
        }

        /// <summary>
        /// 숫자 인식
        /// </summary>
        /// <param name="mtCrop"></param>
        /// <param name="thresholdValue"></param>
        /// <param name="rectTemp"></param>
        /// <returns></returns>
        private static string Recognize(Mat mtCrop, int thresholdValue, Rect rectTemp)
        {
            string resultSeg = "";

            //화살표 길이 확인하여 시간 체크 - 가득 차 있거나 하나도 없을 경우 숫자가 변경되는 순간일 수 있으므로 재시도

            Mat mtThre = new Mat();
            Mat mtSplit = new Mat();

            //Binarize
            Cv2.Threshold(mtCrop, mtThre, thresholdValue, 255, ThresholdTypes.BinaryInv);

            //Morphology
            using (Mat kernel2 = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3))) //10
                Cv2.MorphologyEx(mtThre, mtThre, MorphTypes.Dilate, kernel2);


            int topY = 0; //상단 여백
            for (int z = 0; z < mtThre.Height / 6; z++)
            {
                if (Cv2.CountNonZero(mtThre[new Rect(0, z, mtThre.Width, 1)]) > 0)
                {
                    topY = z;
                    break;
                }
            }

            int bottomY = 0; //하단 여백
            for (int z = mtThre.Height - 1; z > mtThre.Height - (mtThre.Height / 5); z--)
            {
                if (Cv2.CountNonZero(mtThre[new Rect(0, z, mtThre.Width, 1)]) > 0)
                {
                    bottomY = z;
                    break;
                }
            }


            var splitWidth = mtCrop.Width / 6;

            //Cv2.FindContours(mtCrop, out contours, out HierarchyIndex[] hierarchies, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            List<Segment> segments = new List<Segment>();

            //6분할
            for (int k = 0; k < 6; k++)
            {
                Rect rectSplit = new Rect((splitWidth * k), 0, k < 5 ? splitWidth : rectTemp.Width - (splitWidth * k), rectTemp.Height);

                //숫자별 Matrix
                mtSplit = mtThre[rectSplit];

                if (ConfigurationManager.AppSettings["RunMode"].ToLower() == "d")
                {
                    //Cv2.ImShow("seg", mtSplit);
                    //Cv2.WaitKey(0);
                    //Cv2.DestroyWindow("seg");
                }

                //segment horizontal width
                double horWidth = splitWidth * 0.4;
                //segment horizontal height
                double horHeight = (bottomY - topY) * 0.18;
                //segment veritcal width
                double verWidth = horHeight; // widthSplit * 0.25;
                //segment veritcal height
                double verHeight = horWidth;

                //mtSplit.Height - topY - (mtSplit.Height - bottomY);// 순수 높이

                //세그멘트 별 rect 값 저장
                segments.Clear();
                segments.Add(new Segment() { x = (verWidth * 1.3), y = topY, width = horWidth, height = horHeight });
                segments.Add(new Segment() { x = (verWidth * 0.5), y = topY + horHeight, width = verWidth, height = verHeight });
                segments.Add(new Segment() { x = (verWidth * 1.3) + horWidth, y = topY + horHeight, width = verWidth, height = verHeight });
                segments.Add(new Segment() { x = (verWidth * 1.3), y = topY + horHeight + verHeight, width = horWidth, height = horHeight });
                segments.Add(new Segment() { x = (verWidth * 0.5), y = topY + (horHeight * 2) + verHeight, width = verWidth, height = verHeight });
                segments.Add(new Segment() { x = (verWidth * 1.3) + horWidth, y = topY + (horHeight * 2) + verHeight, width = verWidth, height = verHeight });
                segments.Add(new Segment() { x = (verWidth * 1.15), y = bottomY - horHeight, width = horWidth, height = horHeight });

                List<int> rcgSeg = new List<int>();

                //검출 영역 표시용 
                var mtClone = mtSplit.Clone();

                foreach (Segment seg in segments)
                {
                    if (seg.x == 0 || seg.y == 0 || seg.width == 0 || seg.height == 0)
                        continue;

                    Rect segRect = new Rect(
                                Convert.ToInt32(seg.x),
                                Convert.ToInt32(seg.y + seg.height > mtSplit.Height ? mtSplit.Height - seg.height : seg.y),
                                Convert.ToInt32(seg.width),
                                Convert.ToInt32(seg.height));
                    //분할 이미지에서 세그먼트 영역
                    if (seg.width < seg.height && segRect.X + segRect.Width > mtSplit.Width)
                        segRect.X = segRect.X - (segRect.X + segRect.Width - mtSplit.Width);

                    //분할 이미지에서 세그먼트 영역
                    Mat segROI = mtSplit[segRect];

                    //세그먼트 영역 표시 (확인 용도)
                    if (ConfigurationManager.AppSettings["RunMode"].ToLower() == "d")//&& orderedBboxes.ElementAt(0) == rect)
                    {
                        Cv2.CvtColor(mtClone, mtClone, ColorConversionCodes.GRAY2BGR);
                        Cv2.Rectangle(mtClone, segRect, Scalar.Green);
                        if (segments.IndexOf(seg) == 6)
                        {
                            Cv2.ImShow("seg", mtClone);
                            Cv2.WaitKey(0);
                            Cv2.DestroyWindow("seg");
                        }
                        //mtClone.Dispose();
                    }
                    //세그먼트 영역이 채워져있는지 확인
                    int total = Cv2.CountNonZero(segROI);
                    double area = seg.width * seg.height;

                    //비교용 배열 채우기
                    if (total / area > 0.5)
                        rcgSeg.Add(1);
                    else
                        rcgSeg.Add(0);
                }



                bool isFind = false;
                int number = 10;

                for (int j = 0; j < 10; j++)
                {
                    for (int m = 0; m < 7; m++)
                    {
                        if (numberSegments[j, m] == rcgSeg[m])
                        {
                            isFind = m == 6; //세그먼트 7 개 모두 확인되면

                            //continue;
                        }
                        else
                            break;
                    }

                    if (isFind)
                        number = j; //0 ~ 9

                    //1인경우 보정
                    if (rcgSeg.Count == 7 && rcgSeg[0] == 0 && rcgSeg[3] == 0 && rcgSeg[6] == 0)
                        number = 1;

                    if (number < 10)
                        break;
                }

                if (number == 10)
                    return resultSeg;
                //    resultOCR += number.ToString();


                resultSeg += number < 10 ? number.ToString() : "_";
            } //for 6

            return resultSeg;
        }
    }
}
