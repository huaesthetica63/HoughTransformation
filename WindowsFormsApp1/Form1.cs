using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private Bitmap bitmap0; //исходное изображение запоминаем в отдельную переменную
        private int sizeLine = 3;//размер линии для выделения найденного контура
        byte gray(Color col)//формула перевода цветного пикселя в черно-белый
        {
            return (byte)(0.3*col.R + 0.59*col.G + 0.11*col.B);
        }
        public Form1()//конструктор по умолчанию задает фильтр для изображения
        {
            InitializeComponent();
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "JPEG files (*.jpg *.jpeg *.jfif)|*.jpg;*.jpeg;*.jfif"+
                "|PNG files (*.png)|*.png"+
                "|BMP files (*.bmp)|*.bmp"+
                "|TIF files (*.tif *.tiff)|*.tif;*.tiff"+
                "|GIF files (*.gif)|*.gif"+
                "|All files (*.jpg *.jpeg *.jfif *.png *.bmp *.tif *.tiff *.gif)|*.jpg;*.jpeg;*.jfif;*.png;*.bmp;*.tif;*.tiff;*.gif";
        }
        
        
        private void button1_Click(object sender, EventArgs e)//обязательно загруженную картинку переводим в чб
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            Bitmap bitmap = new Bitmap(openFileDialog1.FileName);
            bitmap0 = new Bitmap(bitmap);
            for (int i = 0; i < bitmap.Width; i++)
            {
                for(int j = 0; j < bitmap.Height; j++)
                {
                    Color pix = bitmap.GetPixel(i, j);
                    Color newpix = Color.FromArgb(pix.A, gray(pix), gray(pix), gray(pix));
                    bitmap.SetPixel(i, j, newpix);
                }
            }
            pictureBox1.Image = bitmap;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            button3.Enabled = true;
        }
        public Bitmap binarization(Bitmap bitm, int val)//бинаризация картинки с порогом val - получим картинку только с двумя цветами
        {
            Bitmap bitmap = new Bitmap(bitm);
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    var pix = bitmap.GetPixel(i, j);
                    int r = pix.R;
                    int g = pix.G;
                    int b = pix.B;
                    if (r < val)
                        r = 0;
                    else
                        r = 255;
                    if (g < val)
                        g = 0;
                    else
                        g = 255;
                    if (b < val)
                        b = 0;
                    else
                        b = 255;
                    Color newpix = Color.FromArgb(bitmap.GetPixel(i, j).A, r, g, b);
                    bitmap.SetPixel(i, j, newpix);
                }
            }
            return bitmap;
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {

            pictureBox1.Width = this.Width - 350;
            pictureBox1.Height = this.Height - 70;
        }
        double getRo(double fi, int x, int y)//получаем радиус перпендикуляра
        {
            return x * Math.Cos(fi) + y * Math.Sin(fi);//х и у - координата точки на картинке из котрой проводим пучок прямых
        }
        double radFromDegree(int degree)//радиан из градуса для функций тригонометрии
        {
            return degree * (Math.PI / 180.0);
        }
        struct fiRo//структура одной уникальной прямой
        {
            public int fidegree;//фи
            public int ro;//радиус перпендикуляра к прямой
            public fiRo(int d, int r)
            {
                fidegree = d;//конструктор по умолчанию
                ro = r;
            }
        };

        private void Hough(object sender, EventArgs e)
        {
            try
            {
                Bitmap binarize = new Bitmap(bitmap0);
                binarize = binarization(bitmap0, Int32.Parse(textBox1.Text));
                Bitmap bmp = new Bitmap(binarize);//копируем оригинал картинки
                Dictionary<fiRo, int> map = new Dictionary<fiRo, int>();//словарь - ключу соответствует уникальная прямая из точки, значение по ключу - количество таких прмых полученных из всех точек
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)//обходим все пиксели абсолютно
                    {
                        if (bmp.GetPixel(i, j).R == 0)//если это пиксель черный - это точка для рассмотрения
                        {
                            for (int fiDegree = 0; fiDegree < 180; fiDegree++)//обход половины полного оборота ввиду того, что повторяться два раза нет смысла
                            {
                                double fi = radFromDegree(fiDegree);//перевод градуса в радиан для удобства
                                double ro = getRo(fi, i, j);//получение длины перпендикуляра
                                fiRo temp = new fiRo(fiDegree, (int)ro);//уникальная прямая 
                                //Console.WriteLine(fiDegree.ToString() + "  " + ((int)(ro)).ToString());
                                if (!map.ContainsKey(temp))//если такой прямой еще не было
                                {
                                    map.Add(temp, 0);//добавляем ее
                                }
                                map[temp]++;//увеличиваем частоту встречи такой прямой на единицу
                            }
                        }
                    }
                }
                int maxval = map.Values.Max();//максимальное количество одинаковых прмяых
                var keyOfMax = map.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;//выражение для получения ключа по значению
                MessageBox.Show("MaxVal: " + maxval.ToString() + "; fi: " + keyOfMax.fidegree + "; ro: " + keyOfMax.ro);//выводим данные этой прямой
                //рисование линии
                draw(keyOfMax.fidegree, keyOfMax.ro);
            }
            
            catch (Exception ex)
            {
                MessageBox.Show("Error!");
            }
        }
        public void draw(int degree, int ro)
        {
            try
            {
                
                Graphics gr = Graphics.FromImage(pictureBox1.Image);
                
                int y0 = (int)((ro) / Math.Sin(radFromDegree(degree)));
                int y1 = (int)((ro - pictureBox1.Image.Width * Math.Cos(radFromDegree(degree)) / Math.Sin(radFromDegree(degree))));
                gr.DrawLine(new Pen(Color.Red, sizeLine), new Point(0, y0), new Point(pictureBox1.Image.Width, y1));
                
                pictureBox1.Refresh();
                pictureBox1.Update();
                this.Invalidate();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error!");
            }
          
        }
    }
}
