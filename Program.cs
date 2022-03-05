using System;
using System.Diagnostics;
using System.IO;



/* для подключения System.Drawing в своем проекте правой в проекте нажать правой кнопкой по Ссылкам -> Добавить ссылку
    отметить галочкой сборку System.Drawing    */
using System.Drawing;
using System.Drawing.Drawing2D;

public class PhotoshopMini
{
    private Bitmap photo1 { get; set; }
    private Bitmap photo2 { get; set; }
    private Bitmap result { get; set; }

    public delegate Color Render_func(int r1, int g1, int b1, int r2, int g2, int b2);

    public Render_func choise_renderDelegate;


    // Конструктор
    public PhotoshopMini()
    {
        Console.WriteLine("Введите название первого файла без разрешения");
        string file1 = Console.ReadLine();
        photo1 = new Bitmap("..\\..\\" + file1 + ".jpg");
        Console.WriteLine("Открываю изображение " + Directory.GetParent("..\\..\\") + "\\" + file1 + ".jpg\n");
        Console.WriteLine("Введите название второго файла без разрешения");
        string file2 = Console.ReadLine();
        photo2 = new Bitmap("..\\..\\" + file2 + ".jpg");
        Console.WriteLine("Открываю изображение " + Directory.GetParent("..\\..\\") + "\\" + file2 + ".jpg\n");
        int choise = Print_actions();
        Choose_action(choise);
        Render();
    }


    // Выбор маски
    protected int Choose_mask_form()
    {
        Console.WriteLine("Выберите форму маски:\n" +
            "1: Круг\n" +
            "2: Квадрат\n" +
            "3: Прямоугольник\n");
        int choise = Convert.ToInt32(Console.ReadLine());
        return choise;
    }


    //Выбор действия
    protected int Print_actions()
    {
        Console.WriteLine("Выберите действие с файлами:\n" +
            "1: Сумма\n" +
            "2: Произведение\n" +
            "3: Среднее-арифметическое\n" +
            "4: Минимум\n" +
            "5: Максимум\n" +
            "6: Наложение маски\n");
        int choise = Convert.ToInt32(Console.ReadLine());
        return choise;
    }


    // Выполнение действия
    protected void Choose_action(int choise)
    {
        switch (choise)
        {
            case 1:
                choise_renderDelegate = Sum_pix;
                break;
            case 2:
                choise_renderDelegate = Prod_pix;
                break;
            case 3:
                choise_renderDelegate = Mean_pix;
                break;
            case 4:
                choise_renderDelegate = Min_pix;
                break;
            case 5:
                choise_renderDelegate = Max_pix;
                break;
            case 6:
                int mask_choise = Choose_mask_form();
                Mask_drawer(mask_choise);
                choise_renderDelegate = Prod_pix;
                break;
        }
    }


    // Ну типа моя функция (Нет)
    public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0) return min;
        else if (val.CompareTo(max) > 0) return max;
        else return val;
    }


    // Работа с пикселями
    protected void Render()
    {
        var w = photo1.Width;
        var h = photo1.Height;
        result = new Bitmap(w, h);
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                var pix1 = photo1.GetPixel(j, i);
                var pix2 = photo2.GetPixel(j, i);

                int r1 = pix1.R;
                int g1 = pix1.G;
                int b1 = pix1.B;

                int r2 = pix2.R;
                int g2 = pix2.G;
                int b2 = pix2.B;

                pix1 = choise_renderDelegate(r1, g1, b1, r2, g2, b2);
                result.SetPixel(j, i, pix1);

            }

        }
        Save_result();

    }


    // Попиксельная сумма
    protected Color Sum_pix(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        r1 = (int)Clamp(r1 + r2, 0, 255);
        g1 = (int)Clamp(g1 + g2, 0, 255);
        b1 = (int)Clamp(b1 + b2, 0, 255);

        Color pix = Color.FromArgb(r1, g1, b1);

        return pix;
    }


    // Умножение на цвет пикселя
    protected Color Prod_pix(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        r1 = (int)Clamp(r1 * r2 / 255, 0, 255);
        g1 = (int)Clamp(g1 * g2 / 255, 0, 255);
        b1 = (int)Clamp(b1 * b2 / 255, 0, 255);

        Color pix = Color.FromArgb(r1, g1, b1);

        return pix;
    }


    // Среднее арифметическое
    protected Color Mean_pix(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        r1 = (int)Clamp((r1 + r2) / 2, 0, 255);
        g1 = (int)Clamp((g1 + g2) / 2, 0, 255);
        b1 = (int)Clamp((b1 + b2) / 2, 0, 255);

        Color pix = Color.FromArgb(r1, g1, b1);

        return pix;
    }


    // Минимум
    protected Color Min_pix(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        r1 = (int)Clamp(r1 <= r2 ? r1 : r2, 0, 255);
        g1 = (int)Clamp(g1 <= g2 ? g1 : g2, 0, 255);
        b1 = (int)Clamp(b1 <= b2 ? b1 : b2, 0, 255);

        Color pix = Color.FromArgb(r1, g1, b1);

        return pix;
    }


    // Максимум
    protected Color Max_pix(int r1, int g1, int b1, int r2, int g2, int b2)
    {
        r1 = (int)Clamp(r1 > r2 ? r1 : r2, 0, 255);
        g1 = (int)Clamp(g1 > g2 ? g1 : g2, 0, 255);
        b1 = (int)Clamp(b1 > b2 ? b1 : b2, 0, 255);

        Color pix = Color.FromArgb(r1, g1, b1);

        return pix;
    }


    // Создание маски как второй пикчи
    protected void Mask_drawer(int choise)
    {
        var w = photo1.Width;
        var h = photo2.Height;

        using (var g = Graphics.FromImage(photo2))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            int x;
            int y;

            switch (choise)
            {
                case 1:
                    g.FillRectangle(Brushes.Black, 0, 0, w, h);
                    x = 400;
                    var rect = new RectangleF((w - x) / 2, (h - x) / 2, x, x);
                    g.FillEllipse(Brushes.White, rect);
                    break;
                case 2:
                    x = 400;
                    g.FillRectangle(Brushes.Black, 0, 0, w, h);
                    g.FillRectangle(Brushes.White, (w - x) / 2, (h - x) / 2, x, x);
                    break;
                case 3:
                    x = 600;
                    y = 400;
                    g.FillRectangle(Brushes.Black, 0, 0, w, h);
                    g.FillRectangle(Brushes.White, (w - x) / 2, (h - y) / 2, x, y);
                    break;
            }
            
        }  
        
    }


    // Сохранение результата
    protected void Save_result()
    {
        Console.WriteLine("Введите название результата без разрешения:");
        string result_name = Console.ReadLine();
        Console.WriteLine("Введите путь сохранения");
        string result_path = Console.ReadLine();

        result.Save(result_path + result_name + ".jpg");

        Console.WriteLine("Выходное изображение было сохренено по пути " + result_path + result_name + ".jpg");
        Console.ReadKey();
    }

}

namespace IMGapp
{

    class Program
    {
        static void Main(string[] args)
        {

            var poprobuem = new PhotoshopMini();

        }

    }

}
