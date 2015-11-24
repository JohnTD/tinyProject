#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>
#include <opencv2/imgproc.hpp>
#include <iostream>

using namespace std;
using namespace cv;


int main(int argc, char** argv)
{
    Mat img = imread(argv[1], 0);
    Mat filter = (Mat_<uchar>(3, 3) << -1, -1, -1, -1, 8, -1, -1, -1, -1);

    int nr = img.rows;
    int nc = img.cols;

    Mat test(nr + 2, nc + 2, CV_8UC1, Scalar_<uchar>(0));

    for(int i = 0; i < nr; ++i)
    {
        for(int j = 0; j < nc; ++j)
        {
            test.at<uchar>(i+1, j+1) = img.at<uchar>(i, j);
        }
    }

    for(int i = 1; i <= nr; ++i)
    {
        for(int j = 1; j <= nc; ++j)
        {
            int temp = 0;
            for(int m = -1; m <= 1; ++m)
            {
                for(int n = -1; n <= 1; ++n)
                {
                    temp += test.at<uchar>(i + m, j + n) * filter.at<uchar>(m+1, n+1);
                }
            }
            test.at<uchar>(i, j) = temp;
        }
    }
    namedWindow("test", WINDOW_AUTOSIZE);
    imshow("test", test);
    waitKey(0);
}
