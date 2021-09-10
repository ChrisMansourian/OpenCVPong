using FRC.CameraServer;
using FRC.CameraServer.OpenCvSharp;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpenCVDay3
{
    class Program
    {
        static void Main(string[] args)
        {
            //Fix the right paddle bounce tomorrow

            Console.WriteLine("Hello World!");

            HttpCamera camera = new HttpCamera("Camera", "http://192.168.1.121:1181/stream.mjpg");
            CvSink sink = new CvSink("Sink");
            sink.Source = camera;

            Cv2.NamedWindow("Display", WindowMode.AutoSize);

            int x = 0;
            int y = 0;
            int sigmaX = 0;

            bool goingUp = false;
            bool goingRight = false;

            double ballX = 100;
            double ballY = 100;
            double ballRadius = 10;
            double xSpeed = 10;
            double ySpeed = 10;
            double ballAngle = 70;
            double ballVelocity = 10;
            bool start = true;

            //Cv2.CreateTrackbar("X", "Display", ref x, 8);
            //Cv2.CreateTrackbar("Y", " Display", ref y, 30);
            //Cv2.CreateTrackbar("SigmaX", "Display", ref sigmaX, 8);

            Mat image = new Mat();
            Mat hsv = new Mat();
            Mat mask = new Mat();
            Mat erode = new Mat();
            Mat dilate = new Mat();
            Mat combined = new Mat();
            Mat blur = new Mat();
            Stopwatch sw = new Stopwatch();


            int leftScore = 0;

            int rightScore = 0;


            while (true)
            {
                if (sink.GrabFrame(image) == 0)
                {
                    continue;
                }
                sw.Restart();

                Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);
                Cv2.InRange(hsv, new Scalar(69, 97, 137), new Scalar(103, 255, 255), mask);

                //Cv2.Blur(mask, blur, new Size(2 * x + 1, 2 * y + 1));
                //Cv2.GaussianBlur(mask, blur, new Size(2 * x + 1, 2 * y + 1), sigmaX);
                using Mat structure = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2 * 3 + 1, 2 * 3 + 1), new Point(2, 2));

                Cv2.MorphologyEx(mask, combined, MorphTypes.Dilate, structure);

                Cv2.FindContours(combined, out Point[][] contours, out var _, RetrievalModes.List, ContourApproximationModes.ApproxTC89KCOS);

                //Left bounding box in Red, with text having X, Y, Angle
                //Right bounding box in blue, same text


                //Point[][] circPoints = new Point[ballRadius][];

                //for (int i = ballX; i < ballX + ballRadius; i++)
                //{
                //    circPoints[i - ballX] = new Point[ballRadius];
                //    for (int j = ballY; j < ballY + ballRadius; j++)
                //    {
                //        circPoints[i - ballX][j-ballY] = new Point(i,j);
                //    }
                //}

                //GET THE BORDERS
                //Mat whiteMask = image.InRange(new Scalar(0, 60, 25), new Scalar(255, 255, 255));
                //Cv2.FindContours(whiteMask, out Point[][] borders, out var _, RetrievalModes.List, ContourApproximationModes.ApproxTC89L1);
                //var whiteboxes = borders.Where(x => Cv2.ContourArea(x) > 100000);
                //Cv2.DrawContours(image, whiteboxes.Where(x => Cv2.BoundingRect(x).X < image.Width / 2), -1, Scalar.White, 3);//left 

                int borderWidth = 10;
                Rect borders = new Rect(85, 30, image.Width - 150, image.Height - 150);
                int goalHeight = 200;
                int goalWidth = 40;
                Rect leftGoal = new Rect(borders.Left, (borders.Bottom - borders.Top) / 2 - goalHeight / 2, goalWidth, goalHeight);
                Rect rightGoal = new Rect(borders.Right - goalWidth, (borders.Bottom - borders.Top) / 2 - goalHeight / 2, goalWidth, goalHeight);

                Cv2.Rectangle(image, leftGoal, Scalar.Gold, 20);
                Cv2.Rectangle(image, rightGoal, Scalar.Gold, 20);
                Cv2.Rectangle(image, borders, Scalar.White, borderWidth);

                //Draw borders and goals


                if (start)
                {
                    ballAngle = new Random().Next(1, 181);
                    ballY = (borders.Bottom - borders.Top) / 2;
                    ballX = (borders.Right - borders.Left) / 2;
                    start = false;
                }

                //GET THE PADDLES
                var hitboxes = contours.Where(x => Cv2.ContourArea(x) > 1000);

                Cv2.DrawContours(image, hitboxes.Where(x => Cv2.BoundingRect(x).X < image.Width / 2), -1, Scalar.Red, -1);//left
                Cv2.DrawContours(image, hitboxes.Where(x => Cv2.BoundingRect(x).X > image.Width / 2), -1, Scalar.Blue, -1);//red

                var right = hitboxes.Where(x => Cv2.BoundingRect(x).X > image.Width / 2);
                var left = hitboxes.Where(x => Cv2.BoundingRect(x).X < image.Width / 2);

                bool bounceY = false;
                bool isLeftCollision = false;
                bool isRightCollision = false;

                var rightRect = contours.Where(x => Cv2.ContourArea(x) > 1000).Select(x => Cv2.MinAreaRect(x)).Where(x => x.BoundingRect().X >= borders.Width / 2);
                Cv2.FillPoly(image, rightRect.Select(x => x.Points().Select(z => new Point(z.X, z.Y))), Scalar.CornflowerBlue);

                var leftRect = contours.Where(x => Cv2.ContourArea(x) > 1000).Select(x => Cv2.MinAreaRect(x)).Where(x => x.BoundingRect().X < borders.Width / 2);
                Cv2.FillPoly(image, leftRect.Select(x => x.Points().Select(z => new Point(z.X, z.Y))), Scalar.Red);

                RotatedRect ball = new RotatedRect(new Point(ballX, ballY), new Size2f(ballRadius, ballRadius), 0);


                foreach (Point[] c in right)
                {
                    Point[] a = c;
                    a = a.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();
                    Point bottomP = a[a.Length - 1];

                    a = a.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();

                    Point leftP = a[0];

                    double paddleAngle = 0;

                    paddleAngle = Math.Asin(Math.Abs(leftP.Y - bottomP.Y) / (Math.Sqrt(((leftP.X - bottomP.X) * (leftP.X - bottomP.X)) + (leftP.Y - bottomP.Y) * (leftP.Y - bottomP.Y))));
                    paddleAngle = (180 / (Math.PI * paddleAngle));



                    //Point[] d = a.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
                    //double lineLength = Math.Sqrt((a[0].X * a[a.Length - 1].X) + (a[0].Y * a[a.Length - 1].Y));
                    //double angle = Math.Atan2(lineLength, 90)
                    {/*foreach (Point p in a)
                    {
                        if (ballX + ballRadius >= p.X && ballX - ballRadius <= p.X)
                        {
                            if (ballY + ballRadius >= p.Y && ballY - ballRadius <= p.Y)
                            {
                                if (ballY <= a[0].Y && ySpeed > 0)
                                {
                                    bounceY = true;
                                }
                                if (ballY + ballRadius >= a[a.Length - 1].Y && ySpeed < 0)
                                {
                                    bounceY = true;
                                }

                                if (ballX <= a[0].Y && ballY <= a[0].Y)
                                {
                                    isLeftCollision = true;
                                }
                                isFound = true;
                            }
                        }
                    }*/
                    }
                }
                foreach (Point[] c in left)
                {

                    Point[] a = c;
                    a = a.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray();
                    {/*foreach (Point p in c)
                    {
                        if (ballX + ballRadius >= p.X && ballX - ballRadius <= p.X)
                        {

                            if (ballY + ballRadius >= p.Y && ballY - ballRadius <= p.Y)
                            {
                                isFound = true;
                                if (ballY <= a[0].Y && ySpeed > 0 || ballY + ballRadius >= a[a.Length - 1].Y && ySpeed < 0)
                                {
                                    bounceY = true;
                                }

                                if (ballX <= a[0].Y && ballY <= a[0].Y)
                                {
                                    isLeftCollision = true;
                                }
                            }
                        }
                    }*/
                    }
                }

                {/*if (isFound)
                {
                    if (bounceY)
                    {
                        ySpeed = -ySpeed;
                    }
                    else if (isLeftCollision)
                    {
                        xSpeed = -Math.Abs(ySpeed);
                    }
                    else
                    {
                        xSpeed = Math.Abs(ySpeed);
                    }
                }

                if (ballY + ballRadius >= borders.Bottom - borderWidth)
                {
                    ySpeed = -Math.Abs(ySpeed);
                }
                if (ballY - ballRadius <= borders.Top + borderWidth)
                {
                    ySpeed = Math.Abs(ySpeed);
                }
                if (ballX + ballRadius >= borders.Right - borderWidth)
                {
                    xSpeed = -Math.Abs(xSpeed);
                }
                if (ballX - ballRadius <= borders.Left + borderWidth)
                {
                    xSpeed = Math.Abs(xSpeed);
                }

                ballX += xSpeed;
                ballY += ySpeed;

                Cv2.Circle(image, ballX, ballY, ballRadius, Scalar.Green, 20);
                Rect ballRect = new Rect(ballX - ballRadius, ballY - ballRadius, ballRadius * 2, ballRadius * 2);
                if(ballRect.IntersectsWith(leftGoal))
                {
                    start = true;
                    rightScore++;
                }
                if(ballRect.IntersectsWith(rightGoal))
                {
                    start = true;
                    leftScore++;
                }*/
                }

                //var largeEdge = left.Where(x => Cv2.BoundingRect(x).Size);


                //Cv2.Erode(mask, erode, structure);
                //Cv2.Dilate(erod, dilate, dilateStructure);

                //using Mat combined = mask - dilate;

                //var Elapsed = sw.Elapsed;
                //Cv2.PutText(image, Elapsed.TotalMilliseconds.ToString(), new Point(25, 25), HersheyFonts.HersheyPlain, 1, Scalar.White);

                Cv2.PutText(image, leftScore.ToString(), new Point(borders.Left, borders.Bottom + 60), HersheyFonts.HersheyTriplex, 2, Scalar.Red, 5);
                Cv2.PutText(image, rightScore.ToString(), new Point(borders.Right - 100, borders.Bottom + 60), HersheyFonts.HersheyTriplex, 2, Scalar.Blue, 5);

                if (ballAngle >= 360)
                {
                    ballAngle -= 360;
                }
                if (ballAngle < 0)
                {
                    ballAngle = 360 - ballAngle;
                }

                double xAddition = ballVelocity * Math.Cos(ballAngle);
                double yAddition = ballVelocity * Math.Sin(ballAngle);

                if (xAddition >= 0)
                {
                    goingRight = true;
                }
                else
                {
                    goingRight = false;
                }
                if (yAddition >= 0)
                {
                    goingUp = false;
                }
                else
                {
                    goingUp = true;
                }

                if (ballX + (ballRadius) >= borders.Right - borderWidth)
                {
                    if (goingRight || xAddition == 0)
                    {
                        ballAngle = 180-ballAngle ;
                    }
                }
                if (ballX <= borders.Left)
                {
                    if (!goingRight || xAddition == 0)
                    {
                        ballAngle = 180-ballAngle;
                    }
                }
                if (ballY + (ballRadius) >= borders.Bottom - borderWidth)
                {
                    if (!goingUp || yAddition == 0)
                    {
                        //double tempAngle = Math.Abs(ballAngle);
                        //while (!(tempAngle >= 0 && tempAngle <= 90))
                        //{
                        //    tempAngle -= 90;
                        //}
                        //if (ballAngle >= 0)
                        //{
                        //    ballAngle = ballAngle - 180 - (2 * tempAngle);
                        //}
                        //else
                        ////{
                        //if (goingRight)
                        //{
                        //    ballAngle = ballAngle - Math.Abs(2 *((360 - ballAngle)));
                        //}
                        //else
                        //{
                        //    ballAngle = ballAngle + Math.Abs(2 *((360 - ballAngle)));
                        //}

                        if (!goingUp)
                        {
                            if (!goingRight)
                            {
                                ballAngle = ballAngle - 2 * (ballAngle - (3 * 90) / 2);
                            }
                            else
                            {
                                ballAngle = ballAngle - 2 * (ballAngle - (3 * 90) / 2);
                            }
                        }
                    }
                }
                if (ballY <= borders.Top)
                {
                    if (goingUp)
                    {
                        if (goingRight)
                        {
                            ballAngle = ballAngle - 2 * (ballAngle - (3 * 90) / 2);
                        }
                        else
                        {
                            ballAngle = ballAngle - 2 * (ballAngle - (3 * 90) / 2);
                        }
                    }

                    //double tempAngle = Math.Abs(ballAngle);
                    //while (!(tempAngle >= 0 && tempAngle <= 90))
                    //{
                    //    tempAngle -= 90;
                    //}
                    /*if (ballAngle >= 0)
                    {
                        ballAngle = 180 + (2 * tempAngle);
                    }
                    else
                    {
                        ballAngle = 180 - (2 * tempAngle);
                    }*/


                    //if (goingRight)
                    //{
                    //    ballAngle = ballAngle - Math.Abs(2*((360-ballAngle)));
                    //}
                    //else
                    //{
                    //    ballAngle = ballAngle + Math.Abs(2*((360 - ballAngle)));
                    //}
                    // ballAngle = ballAngle + (2 * (90 - tempAngle));
                }

                ballX += xAddition;
                ballY += yAddition;
                if (ballX > image.Width || ballX < 0 || ballY < 0 || ballY >= image.Height);

                Cv2.Circle(image, new Point(ballX, ballY), (int)ballRadius, Scalar.Green, -1);


                Cv2.ImShow("Display", image);

                if (Cv2.WaitKey(1) != -1)
                {
                    break;
                }


            }
        }
    }
}
