using System;
using System.Diagnostics;
using System.IO;



/* для подключения System.Drawing в своем проекте правой в проекте нажать правой кнопкой по Ссылкам -> Добавить ссылку
    отметить галочкой сборку System.Drawing    */
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

public class PhotoshopMini
{
    private Bitmap photo1 { get; set; }
    private Bitmap photo2 { get; set; }
    private Bitmap result { get; set; }
    private Bitmap hist1 { get; set; }
    private Bitmap hist2 { get; set; }
    private Bitmap binar_pic { get; set; }

    public delegate Color Render_func(int r1, int g1, int b1, int r2, int g2, int b2);
    public delegate Color Render_func_solo_pic(int r, int g, int b);

    public Render_func_solo_pic choise_solo_renderDelegate;
    public Render_func choise_renderDelegate;

    private float[] x;
    private float[] y;
    private float[,] integral_matrix;
    private float[,] integral_matrix_sqr;
    int count;

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
        hist1 = DrawGist(photo1);
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
            "6: Наложение маски\n" +
            "7: Градационное преобразование первой картинки\n");
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
            case 7:
                Interpol_points();
                choise_renderDelegate = Grad_transform;
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

        hist2 = DrawGist(result, 1);

        Save_result();

    }


    //Работа с результатом
    protected void Render_solo()
    {
        var w = result.Width;
        var h = result.Height;
        binar_pic = new Bitmap(w, h);
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                var pix1 = result.GetPixel(j, i);

                int r = pix1.R;
                int g = pix1.G;
                int b = pix1.B;

                pix1 = choise_solo_renderDelegate(r, g, b);
                binar_pic.SetPixel(j, i, pix1);

            }
        }
    }



    protected void Interpol_points()
    {
        Console.WriteLine("Введите кол-во точек для интерполирования: (минимум две для 0 и для 255)");
        count = Convert.ToInt32(Console.ReadLine());
        Console.WriteLine("Введите пары значений точек: ");
        x = new float[count];
        y = new float[count];
        Console.WriteLine("Введите значение y для x = 0: ");
        x[0] = 0;
        y[0] = (float)Convert.ToDouble(Console.ReadLine());

        Console.WriteLine("Введите значение y для x = 255: ");
        x[count - 1] = 255;
        y[count - 1] = (float)Convert.ToDouble(Console.ReadLine());
        for (int i = 1; i < count - 1; i++)
        {
            string[] str = Console.ReadLine().Split(' ');
            x[i] = (float)Convert.ToDouble(str[0]);
            y[i] = (float)Convert.ToDouble(str[1]);
        }
        // Array.Sort(x, y);
    }


    // Создание картинки с гистограммой
    public Bitmap DrawGist(Bitmap img, int inp = 0)
    {
        int[] hist = new int[256];
        var histImage = new Bitmap(256, 256);
        int height = img.Height, width = img.Width;
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                var pixel = img.GetPixel(j, i);
                var c = (pixel.R + pixel.G + pixel.B) / 3;
                hist[c]++;
            }
        }

        var maxC = hist.Max();
        var k = (float)height / maxC;

        for (int i = 0; i < 256; i++)
        {
            int x1 = i, y1 = 255;
            int x2 = i, y2 = (int)(255 - hist[i] * k);

            using (var g = Graphics.FromImage(histImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                var pen = Pens.Black.Clone() as Pen;
                pen.Width = 5;
                g.DrawLine(pen, x1, y1, x2, y2);
                var pen2 = Pens.ForestGreen.Clone() as Pen;
                pen2.Width = 3;
                if (inp == 1)
                    for (int z = 0; z < count - 1; z++)
                        g.DrawLine(pen2, x[z], (255 - y[z]), x[z + 1], (255 - y[z + 1]));
            }
        }

        return histImage;
    }


    //Интерполирование функции по точкам,
    //которая позже будет использоваться для градационного преобразования
    protected float Grad_transform_func_interpol(float X)
    {
        if (X <= x[0])
            return y[0];
        else if (X >= x[count - 1])
            return y[count - 1];

        // . в точках?
        for (int i = 0; i < count; ++i)
            if (X == x[i])
                return y[i];

        // . между точками?
        for (int i = 0; i < count - 1; ++i)
            if (X >= x[i] && X <= x[i + 1])
            {
                float k = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);

                float b = y[i] - k * x[i];
                return k * X + b;
            }
        return 0;
    }


    //Градационная трансформация
    protected Color Grad_transform(int r1, int g1, int b1, int r2, int g2, int b2)
    {

        r1 = (int)Clamp(Grad_transform_func_interpol(r1), 0, 255);
        g1 = (int)Clamp(Grad_transform_func_interpol(g1), 0, 255);
        b1 = (int)Clamp(Grad_transform_func_interpol(b1), 0, 255);

        Color pix = Color.FromArgb(r1, g1, b1);

        return pix;
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


    //Приведение картинки к градациям серого
    protected Color Grad_grey(int r, int g, int b)
    {
        r = (int)(r * 0.2125);
        g = (int)(g * 0.7154);
        b = (int)(b * 0.0721);

        Color pix = Color.FromArgb(r, g, b);

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


    //Требование бинаризации
    protected bool Binar_is_required()
    {
        Console.WriteLine("Требуется ли бинаризировать результат? \n" +
            "1: Да \n" +
            "2: Нет \n");
        int ans = Convert.ToInt32(Console.ReadLine());
        bool choise = ans == 1;
        return choise;
    }


    //Выбор Алгоритма бинаризации
    public int Print_binar_variations()
    {
        Console.WriteLine("Выберите алгоритм бинаризации:\n" +
            "1: Метод Gavrega\n" +
            "2: Метод Отсу\n" +
            "3: Метод Ниблека\n" +
            "4: Метод Сауволы\n" +
            "5: Метод Вульфа\n" +
            "6: Метод Бредли-Рота\n");
        int choise = Convert.ToInt32(Console.ReadLine());
        return choise;
    }


    //Вызовы алгоритмов бинаризации
    protected void Choose_bin_alg(int choise)
    {
        choise_solo_renderDelegate = Grad_grey;
        Render_solo();

        switch (choise)
        {
            case 1:
                Gavr_method(binar_pic);
                break;
            case 2:
                Otsu_method(binar_pic);
                break;
            case 3:
                Console.WriteLine("Введите размер окна:");
                int size = Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Введите чувствительность:");
                float sens = (float)Convert.ToDouble(Console.ReadLine());
                Niblek_method(binar_pic, size, sens);
                break;
            case 4:
                Console.WriteLine("Введите размер окна:");
                size = (int)Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Введите R:");
                int R = (int)Convert.ToDouble(Console.ReadLine());
                Console.WriteLine("Введите чувствительность:");
                sens = (float)Convert.ToDouble(Console.ReadLine());
                Sauvol_method(binar_pic, size, R, sens);
                break;
            case 5:
                Console.WriteLine("Введите размер окна:");
                size = (int)Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Введите коэффициент alpha:");
                int alpha = (int)Convert.ToDouble(Console.ReadLine());
                Christian_method(binar_pic, size, alpha);
                break;
            case 6:
                Console.WriteLine("Введите размер окна:");
                size = (int)Convert.ToInt32(Console.ReadLine());
                Console.WriteLine("Введите чувствительность:");
                sens = (int)Convert.ToDouble(Console.ReadLine());
                Bredley_method(binar_pic, size, sens);
                break;
        }
    }


    //Реализации алгоритмов бинаризации

    //Создание интегральной матрицы
    protected void get_integral_matrix(Bitmap pic, bool sqr)
    {
        var w = pic.Width;
        var h = pic.Height;
        if (sqr)
            integral_matrix_sqr = new float[h + 1, w + 1];
        else
            integral_matrix = new float[h + 1, w + 1];
        float bright;
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                if (i == 0 || j == 0)
                {
                    if (sqr)
                        integral_matrix_sqr[i, j] = 0;
                    else
                        integral_matrix[i, j] = 0;
                }

                else
                {
                    var pix1 = pic.GetPixel(j, i);
                    bright = (pix1.R + pix1.G + pix1.B) / 3;
                    if (sqr)
                    {
                        bright *= bright;
                        integral_matrix_sqr[i, j] = bright + integral_matrix_sqr[i - 1, j] + integral_matrix_sqr[i, j - 1] - integral_matrix_sqr[i - 1, j - 1];
                    }
                    else
                        integral_matrix[i, j] = bright + integral_matrix[i - 1, j] + integral_matrix[i, j - 1] - integral_matrix[i - 1, j - 1];
                }
            }
        }
    }

    //Алгоритм Гаврега
    protected void Gavr_method(Bitmap pic)
    {
        var w = pic.Width;
        var h = pic.Height;
        float t = 0;
        float bright;
        int color;
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                var pix1 = pic.GetPixel(j, i);
                t += pix1.GetBrightness();
            }
        }
        t = t / (w * h);
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                var pix1 = pic.GetPixel(j, i);
                bright = pix1.GetBrightness();
                color = bright <= t ? 0 : 255;
                Color pix = Color.FromArgb(color, color, color);
                binar_pic.SetPixel(j, i, pix);
            }
        }
    }


    // Алгоритм Отсу
    protected void Otsu_method(Bitmap pic)
    {
        float[] hist = new float[256];
        int height = pic.Height, width = pic.Width;
        int max_bright = 0;
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                var pixel = pic.GetPixel(j, i);
                var c = (int)((pixel.R +pixel.G + pixel.B) / 3);
                if (c > max_bright)
                    max_bright = c;
                hist[c]++;
            }
        }
        int total_pix = height * width;
        float m_t = 0;
        for (int i = 0; i <= max_bright; ++i)
        {
            hist[i] = hist[i] / total_pix;
            m_t += hist[i] * i;
        }
        float m_2, m_1, m_3 = 0;
        float g_2, g_1 = 0;
        float sigma_2, sigma_max = 0;
        float t = 0;
        for (int i = 1; i <= max_bright + 1; ++i)
        {
            g_1 += hist[i - 1];
            m_3 += hist[i - 1] * i;
            g_2 = 1 - g_1;
            m_1 = m_3 / g_1;
            m_2 = (m_t - m_1 * g_1) / g_2;
            sigma_2 = g_1 * g_2 * (float)Math.Pow((m_1 - m_2), 2);
            if (sigma_2 > sigma_max)
            {
                sigma_max = sigma_2;
                t = (float)i;
                Console.WriteLine(t);
            }
        }
        float bright;
        int color;
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                var pix1 = pic.GetPixel(j, i);
                bright = (pix1.R + pix1.G + pix1.B);
                Console.WriteLine(bright);
                color = bright <= t ? 0 : 255;
                Color pix = Color.FromArgb(color, color, color);
                binar_pic.SetPixel(j, i, pix);
            }
        }
    }


    //Алгоритм Критерий Ниблека
    protected void Niblek_method(Bitmap pic, int size, float sens)
    {
        Bitmap pic_temp = pic;
        sens = sens * (-1);
        float bright;
        int color;
        get_integral_matrix(pic_temp, false);
        get_integral_matrix(pic_temp, true);
        int h = pic.Height, w = pic.Width;
        int step = (size - 1) / 2;
        float m_o_2, m_2_o, m_o,dispersion, std_divr, t;
        int x1, x2, y1, y2;
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                if (i - step < 0)
                    x1 = 0;
                else
                    x1 = i - step;
                
                if (i + step >= h)
                    x2 = h - 1;
                else
                    x2 = i + step;
                
                if (j - step < 0)
                    y1 = 0;
                else
                    y1 = j - step;

                if (j + step >= w)
                    y2 = w - 1;
                else
                    y2 = j + step;

                int win_size = (x2 - x1 + 1) * (y2 - y1 + 1);
                var pix1 = pic.GetPixel(j, i);
                m_o = ((integral_matrix[x2 + 1, y2 + 1] + integral_matrix[x1, y1] - integral_matrix[x1, y2 + 1] - integral_matrix[x2 + 1, y1]) / win_size);
                m_o_2 = (float)Math.Pow(m_o, 2);
                m_2_o = (integral_matrix_sqr[x2 + 1, y2 + 1] + integral_matrix_sqr[x1, y1] - integral_matrix_sqr[x1, y2 + 1] - integral_matrix_sqr[x2 + 1, y1]) / win_size;
                dispersion = m_2_o - m_o_2;
                std_divr = (float)Math.Pow(dispersion, 0.5);
                t = m_o + sens * std_divr;
                bright = (pix1.R + pix1.G + pix1.B) / 3;
                color = bright <= t ? 0 : 255;
                Color pix = Color.FromArgb(color, color, color);
                binar_pic.SetPixel(j, i, pix);
            }
        }
    }


    //Алгоритм Критерий Сауволы
    protected void Sauvol_method(Bitmap pic, int size, int R, float sens)
    {
        Bitmap pic_temp = pic;
        sens = sens * (-1);
        float bright;
        int color;
        get_integral_matrix(pic_temp, false);
        get_integral_matrix(pic_temp, true);
        int h = pic.Height, w = pic.Width;
        int step = (size - 1) / 2;
        float m_o_2, m_2_o, m_o, dispersion, std_divr, t;
        int x1, x2, y1, y2;
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                if (i - step < 0)
                    x1 = 0;
                else
                    x1 = i - step;

                if (i + step >= h)
                    x2 = h - 1;
                else
                    x2 = i + step;

                if (j - step < 0)
                    y1 = 0;
                else
                    y1 = j - step;

                if (j + step >= w)
                    y2 = w - 1;
                else
                    y2 = j + step;

                int win_size = (x2 - x1 + 1) * (y2 - y1 + 1);
                var pix1 = pic.GetPixel(j, i);
                m_o = ((integral_matrix[x2 + 1, y2 + 1] + integral_matrix[x1, y1] - integral_matrix[x1, y2 + 1] - integral_matrix[x2 + 1, y1]) / win_size);
                m_o_2 = (float)Math.Pow(m_o, 2);
                m_2_o = (integral_matrix_sqr[x2 + 1, y2 + 1] + integral_matrix_sqr[x1, y1] - integral_matrix_sqr[x1, y2 + 1] - integral_matrix_sqr[x2 + 1, y1]) / win_size;
                dispersion = m_2_o - m_o_2;
                std_divr = (float)Math.Pow(dispersion, 0.5);
                t = m_o * (1 + sens * (std_divr / R - 1));
                bright = (pix1.R + pix1.G + pix1.B) / 3;
                color = bright <= t ? 0 : 255;
                Color pix = Color.FromArgb(color, color, color);
                binar_pic.SetPixel(j, i, pix);
            }
        }
    }


    //Алгоритм Кристианна Вульфа
    protected void Christian_method(Bitmap pic, int size, float alpha)
    {
        Bitmap pic_temp = pic;
        float bright;
        int color;
        get_integral_matrix(pic_temp, false);
        get_integral_matrix(pic_temp, true);
        int h = pic.Height, w = pic.Width;
        int step = (size - 1) / 2;
        float m_o_2, m_2_o, m_o, dispersion, std_divr, t;
        int x1, x2, y1, y2;
        float min_pix = 257, max_std_divr = 0;
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                if (i - step < 0)
                    x1 = 0;
                else
                    x1 = i - step;

                if (i + step >= h)
                    x2 = h - 1;
                else
                    x2 = i + step;

                if (j - step < 0)
                    y1 = 0;
                else
                    y1 = j - step;

                if (j + step >= w)
                    y2 = w - 1;
                else
                    y2 = j + step;

                int win_size = (x2 - x1 + 1) * (y2 - y1 + 1);
                var pix1 = pic.GetPixel(j, i);
                m_o = ((integral_matrix[x2 + 1, y2 + 1] + integral_matrix[x1, y1] - integral_matrix[x1, y2 + 1] - integral_matrix[x2 + 1, y1]) / win_size);
                m_o_2 = (float)Math.Pow(m_o, 2);
                m_2_o = (integral_matrix_sqr[x2 + 1, y2 + 1] + integral_matrix_sqr[x1, y1] - integral_matrix_sqr[x1, y2 + 1] - integral_matrix_sqr[x2 + 1, y1]) / win_size;
                dispersion = m_2_o - m_o_2;
                std_divr = (float)Math.Pow(dispersion, 0.5);
                bright = (pix1.R + pix1.G + pix1.B) / 3;
                if (bright < min_pix)
                    min_pix = bright;
                if (std_divr > max_std_divr)
                    max_std_divr = std_divr;

            }
        }
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                if (i - step < 0)
                    x1 = 0;
                else
                    x1 = i - step;

                if (i + step >= h)
                    x2 = h - 1;
                else
                    x2 = i + step;

                if (j - step < 0)
                    y1 = 0;
                else
                    y1 = j - step;

                if (j + step >= w)
                    y2 = w - 1;
                else
                    y2 = j + step;

                int win_size = (x2 - x1 + 1) * (y2 - y1 + 1);
                var pix1 = pic.GetPixel(j, i);
                m_o = ((integral_matrix[x2 + 1, y2 + 1] + integral_matrix[x1, y1] - integral_matrix[x1, y2 + 1] - integral_matrix[x2 + 1, y1]) / win_size);
                m_o_2 = (float)Math.Pow(m_o, 2);
                m_2_o = (integral_matrix_sqr[x2 + 1, y2 + 1] + integral_matrix_sqr[x1, y1] - integral_matrix_sqr[x1, y2 + 1] - integral_matrix_sqr[x2 + 1, y1]) / win_size;
                dispersion = m_2_o - m_o_2;
                std_divr = (float)Math.Pow(dispersion, 0.5);
                t = (1 - alpha) * m_o + alpha * min_pix + alpha * (std_divr / max_std_divr) * (m_o - min_pix);
                bright = (pix1.R + pix1.G + pix1.B) / 3;
                color = bright <= t ? 0 : 255;
                Color pix = Color.FromArgb(color, color, color);
                binar_pic.SetPixel(j, i, pix);
            }
        }
    }


    //Алгоритм Бредли-Рота
    protected void Bredley_method(Bitmap pic, int size, float sense)
    {
        Bitmap pic_temp = pic;
        float bright;
        int color;
        get_integral_matrix(pic_temp, false);
        get_integral_matrix(pic_temp, true);
        int h = pic.Height, w = pic.Width;
        int step = (size - 1) / 2;
        float m_o, sum, t;
        int x1, x2, y1, y2;
        for (int i = 0; i < h; ++i)
        {
            for (int j = 0; j < w; ++j)
            {
                if (i - step < 0)
                    x1 = 0;
                else
                    x1 = i - step;

                if (i + step >= h)
                    x2 = h - 1;
                else
                    x2 = i + step;

                if (j - step < 0)
                    y1 = 0;
                else
                    y1 = j - step;

                if (j + step >= w)
                    y2 = w - 1;
                else
                    y2 = j + step;

                int win_size = (x2 - x1 + 1) * (y2 - y1 + 1);
                var pix1 = pic.GetPixel(j, i);
                bright = (pix1.R + pix1.G + pix1.B) / 3;
                m_o = integral_matrix[x2 + 1, y2 + 1] + integral_matrix[x1, y1] - integral_matrix[x1, y2 + 1] - integral_matrix[x2 + 1, y1];
                t = m_o * (1 - sense);
                color = bright * win_size < t ? 0 : 255;
                Color pix = Color.FromArgb(color, color, color);
                binar_pic.SetPixel(j, i, pix);

            }
        }
    }


    // Сохранение результата
    protected void Save_result()
    {
        int binar_choise;
        //Требуется ли бинаризация
        if (Binar_is_required())
        {
            binar_choise = Print_binar_variations();
            Choose_bin_alg(binar_choise);
        }


        Console.WriteLine("Введите название результата без разрешения:");
        string result_name = Console.ReadLine();
        Console.WriteLine("Введите путь сохранения");
        string result_path = Console.ReadLine();

        result.Save(result_path + result_name + ".jpg");

        if (hist1 != null)
            hist1.Save(result_path + result_name + "_in_hist" + ".jpg");
        if (hist2 != null)
            hist2.Save(result_path + result_name + "_out_hist" + ".jpg");
        if (binar_pic != null)
            binar_pic.Save(result_path + result_name + "_binarized" + ".jpg");
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
