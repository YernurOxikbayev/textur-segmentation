//Дисциплина: Методы интеллектуальной обработки и анализа изображения
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace lab5
{
    public partial class Form1 : Form
    {
        private Bitmap bmp;
        private byte[] grayIm;
        private bool[] binIm;
        private int maskSize = 3;
        private int threshold;
        SegmentationFormula sf;

        public Form1()
        {
            InitializeComponent();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            getFile(pictureBox1);
        }
        public void getFile(PictureBox pictureBox1)
        {

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files (*.BMP, *.JPG, *.GIF, *.TIF, *.PNG, *.ICO, *.EMF, *.WMF)|*.bmp;*.jpg;*.gif; *.tif; *.png; *.ico; *.emf; *.wmf";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Image image = Image.FromFile(dialog.FileName);
                int width = pictureBox1.Width;
                int height = pictureBox1.Height;

                bmp = new Bitmap(image, width, height);
                Rgb2Gray();
                pictureBox1.Image = bmp;
            }
        }

        private void Rgb2Gray()
        {//Получаем высоту и ширину bitmap
            int width = bmp.Width;
            int height = bmp.Height;
            //Блокируем биты исходного изображения в системной памяти
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            // общее количество байтов в изображении
            int bytes = data.Stride * data.Height;
            //Создание массивов байтов для хранения информации о пикселях изображения
            byte[] pixelBuffer = new byte[bytes];
            grayIm = new byte[bytes/4];
            // Получаем адрес первых пиксельных данных
            IntPtr first_pixel = data.Scan0;
            // Копирование данных изображения в один из массивов байтов
            Marshal.Copy(first_pixel, pixelBuffer, 0, bytes);
            // Разблокируем бит из системной памяти так как у нас есть вся необходимая информация в массиве
            bmp.UnlockBits(data);
            // Перевод изображения в оттенки серого
            float rgb = 0;
            for (int i = 0; i < pixelBuffer.Length; i += 4)
            {
                rgb = pixelBuffer[i] * .21f;
                rgb += pixelBuffer[i + 1] * .71f;
                rgb += pixelBuffer[i + 2] * .071f;

                grayIm[i/4] = (byte)rgb;
            }
        }

        private Bitmap DefineTexture(int kernelSize)
        {//Получаем высоту и ширину bitmap
            int width = bmp.Width;
            int height = bmp.Height;
            //Создание массивов байтов для хранения информации о пикселях изображения
            bool[] resultBw = new bool[width * height];
            byte[] resultBuffer = new byte[width * height * 4];
            //центральный пиксель смещен от границы ядра 
            int filterOffset = (kernelSize - 1) / 2;
            int calcOffset = 0;
            int byteOffset = 0;
            
            float mean;
            byte[] z = new byte[kernelSize * kernelSize];
            int k;
            double mu2;
            double mu3;
            double sigma;

            for (int offsetY = filterOffset; offsetY < height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < width - filterOffset; offsetX++)
                {
                    byteOffset = offsetY * width + offsetX;
                    mean = 0;
                    k = 0;
                    mu2 = 0;
                    mu3 = 0;
                    sigma = 0;
                    int[] hist = new int[256];
                    int brightness;

                    for (int filterY = -filterOffset;filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset;
                            filterX <= filterOffset; filterX++)
                        {
                            calcOffset = (offsetY + filterY) * width + offsetX + filterX;

                            brightness = grayIm[calcOffset];
                            hist[brightness]++;//гистограмма
                            z[k++] = grayIm[calcOffset];//яркость
                            mean += grayIm[calcOffset];//суммируем яркостей каждого пикселя.
                        }
                    }

                    mean = mean / (kernelSize * kernelSize);// среднее значение яркости изображения

                    if (sf.Equals(SegmentationFormula.ThirdMoment))//Третий момент
                    {
                        for (int i = 0; i < k; i++)
                        {
                            mu3 += Math.Pow((z[i] - mean), 3) * hist[z[i]];
                        }

                        resultBw[byteOffset] = mu3 > threshold;
                    }
                    else if (sf.Equals(SegmentationFormula.Sigma))// стандартное отклонение
                    {
                        for (int i = 0; i < k; i++)
                        {
                            mu2 += Math.Sqrt((Math.Pow((z[i] - mean), 2)));
                        }

                        sigma = Math.Sqrt(mu2) / kernelSize;
                        resultBw[byteOffset] = sigma > threshold;
                    }

                    resultBuffer[byteOffset * 4] = resultBw[byteOffset] ? (byte)255 : (byte)0;
                    resultBuffer[byteOffset * 4 + 1] = resultBuffer[byteOffset * 4];
                    resultBuffer[byteOffset * 4 + 2] = resultBuffer[byteOffset * 4];
                    resultBuffer[byteOffset * 4 + 3] = 255;
                }
            }

            binIm = resultBw;

            Bitmap resultBitmap = new Bitmap(width, height);
            //Блокируем биты исходного изображения в системной памяти
            BitmapData resultData =
                       resultBitmap.LockBits(new Rectangle(0, 0,
                       resultBitmap.Width, resultBitmap.Height),
                       ImageLockMode.WriteOnly,
                       PixelFormat.Format32bppArgb);
            // Копирование данных изображения в один из массивов байтов
            Marshal.Copy(resultBuffer, 0, resultData.Scan0,
                                       resultBuffer.Length);
            // Разблокируем бит из системной памяти так как у нас есть вся необходимая информация в массиве
            resultBitmap.UnlockBits(resultData);

            return resultBitmap;
        }
        //стандартное отклонение
        private void sigma_Segm_Click(object sender, EventArgs e)
        {
            threshold = 2;
            numericUpDown2.Value = threshold;
            sf = SegmentationFormula.Sigma;
            pictureBox3.Image = DefineTexture(maskSize);
            pictureBox2.Image = ShowSegmentedResult();
        }
        // 3-й момент 
        private void moment_Segm_Click(object sender, EventArgs e)
        {
            threshold = 210;
            numericUpDown2.Value = threshold;
            sf = SegmentationFormula.ThirdMoment;
            pictureBox3.Image = DefineTexture(maskSize);
            pictureBox2.Image = ShowSegmentedResult();
        }

        private Image ShowSegmentedResult()
        {
            int width = bmp.Width;
            int height = bmp.Height;
            //Блокируем биты исходного изображения в системной памяти
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int bytes = data.Stride * data.Height;

            byte[] pixelBuffer = new byte[bytes];
            // Копирование данных изображения в один из массивов байтов
            Marshal.Copy(data.Scan0, pixelBuffer, 0, bytes);
            // Разблокируем бит из системной памяти так как у нас есть вся необходимая информация в массиве
            bmp.UnlockBits(data);

            for (int i = 0; i < pixelBuffer.Length; i += 4)
            {
                if (!binIm[i/4])
                {
                    pixelBuffer[i] = 0;
                    pixelBuffer[i + 1] = 0;
                    pixelBuffer[i + 2] = 0;
                }
            }

            Bitmap resultBitmap = new Bitmap(width, height);
            //Блокируем биты исходного изображения в системной памяти
            BitmapData resultData =resultBitmap.LockBits(new Rectangle(0, 0,resultBitmap.Width, resultBitmap.Height),
                       ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);

            Marshal.Copy(pixelBuffer, 0, resultData.Scan0,pixelBuffer.Length);

            resultBitmap.UnlockBits(resultData);

            return resultBitmap;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            maskSize = (int)numericUpDown1.Value;
            pictureBox3.Image = DefineTexture(maskSize);
            pictureBox2.Image = ShowSegmentedResult();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            threshold = (int)numericUpDown2.Value;
            pictureBox3.Image = DefineTexture(maskSize);
            pictureBox2.Image = ShowSegmentedResult();
        }

        public enum SegmentationFormula
        {
            ThirdMoment,
            Sigma
        }
    }
}
