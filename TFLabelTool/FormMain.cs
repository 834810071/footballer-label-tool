﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Drawing.Imaging;

namespace TFLabelTool
{
    public partial class FormMain : Form
    {
        string filename_ = "groundtruth.txt";    // 标框文件
        string labelFile_ = "label.txt";         // 遮挡label文件
        string ocFile_ = "oc.txt";               // 球员中心坐标文件
        string outputPath_ = "";                 // 输出路径
        string imagePath_ = "";                  // 标注图像路径

        int zoom_ = 1;                           // 缩放比例  

        //string currentJPGName_ = "";             // 当前图片名称

        bool isFormMain_KeyDown_ = false;           // 判断是否通过键盘调整标框位置
        bool AdjustChange_ = false;
        bool isOcfunc_ = false;
        bool isreset_ = false;
        int prelistBoxFileIndex_ = -1;

        // 构造函数
        public FormMain()
        {
            InitializeComponent();
        }

        // 主窗口加载
        private void FormMain_Load(object sender, EventArgs e)
        {
            outputPath_ = String.Format("{0}\\{1}", Application.StartupPath, "output");
            imagePath_ = String.Format("{0}\\{1}\\", outputPath_, "image");


            if (!Directory.Exists(outputPath_))
            {
                Directory.CreateDirectory(outputPath_);
            }
            if (!Directory.Exists(imagePath_))
            {
                Directory.CreateDirectory(imagePath_);
            }

            LoadFiles();            // 加载图片
            LoadGroundTruthFiles(); // 加载groundtruth.txt
            LoadLabelFiles();       // 加载label.txt
            LoadOcFiles();          // 加载Oc.txt
            textBoxImport.Text = Properties.Settings.Default.imagePath;

        }

        // 加载image路径下的所有图像
        void LoadFiles()
        {
            listBoxFiles.Items.Clear();
            var dir = new DirectoryInfo(imagePath_);
            foreach (var file in dir.GetFiles())
            {
                if (file.Extension == ".jpg")
                    listBoxFiles.Items.Add(file.FullName);
            }
        }

        // 加载groundtruth.txt
        private void LoadGroundTruthFiles()
        {
            var txt = imagePath_ + filename_;

            if (File.Exists(txt))   // 如果文件存在，直接返回
            {
                return;
            }

            string context = "0 0 0 0";

            // 初始化文件
            FileStream _file = new FileStream(@txt, FileMode.Create, FileAccess.ReadWrite);
            using (StreamWriter writer1 = new StreamWriter(_file))
            {
                for (int i = 0; i < listBoxFiles.Items.Count; ++i)
                {
                    writer1.WriteLine(context);
                }
                writer1.Flush();
                writer1.Close();

                _file.Close();
            }
        }

        // 加载label.txt
        private void LoadLabelFiles()
        {
            var txt = imagePath_ + labelFile_;

            if (File.Exists(txt))   // 如果文件存在，直接返回
            {
                return;
            }

            // 初始化文件
            FileStream _file = new FileStream(@txt, FileMode.Create, FileAccess.ReadWrite);
            using (StreamWriter writer1 = new StreamWriter(_file))
            {
                for (int i = 0; i < listBoxFiles.Items.Count; ++i)
                {
                    writer1.WriteLine(Convert.ToString(0));
                }
                writer1.Flush();
                writer1.Close();

                _file.Close();
            }
        }

        // 加载Oc.txt
        private void LoadOcFiles()
        {
            var txt = imagePath_ + ocFile_;

            if (File.Exists(txt))   // 如果文件存在，直接返回
            {
                return;
            }
            string context = "0 0";

            // 初始化文件
            FileStream _file = new FileStream(@txt, FileMode.Create, FileAccess.ReadWrite);
            using (StreamWriter writer1 = new StreamWriter(_file))
            {
                for (int i = 0; i < listBoxFiles.Items.Count; ++i)
                {
                    writer1.WriteLine(context);
                }
                writer1.Flush();
                writer1.Close();

                _file.Close();
            }
        }

        // 保存groundtruth.txt
        void SaveGroundTruthFile()
        {
            var txt = imagePath_ + filename_;
            File.Delete(txt);
            var content = "";
            foreach (var item in listBoxLable.Items)    // 从listBoxLabel控件中加载信息
            {
                content += item + "\n";
            }
            File.AppendAllText(txt, content.Trim());
        }

        private Bitmap ZoomImage(Bitmap bitmap, int destHeight, int destWidth)
        {
            try
            {
                System.Drawing.Image sourImage = bitmap;
                int width = 0, height = 0;
                //按比例缩放             
                int sourWidth = sourImage.Width;
                int sourHeight = sourImage.Height;
                if (sourHeight > destHeight || sourWidth > destWidth)
                {
                    if ((sourWidth * destHeight) > (sourHeight * destWidth))
                    {
                        width = destWidth;
                        height = (destWidth * sourHeight) / sourWidth;
                    }
                    else
                    {
                        height = destHeight;
                        width = (sourWidth * destHeight) / sourHeight;
                    }
                }
                else
                {
                    width = sourWidth;
                    height = sourHeight;
                }
                Bitmap destBitmap = new Bitmap(destWidth, destHeight);
                Graphics g = Graphics.FromImage(destBitmap);
                g.Clear(Color.Transparent);
                //设置画布的描绘质量           
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                g.DrawImage(sourImage, new Rectangle((destWidth - width) / 2, (destHeight - height) / 2, width, height), 0, 0, sourImage.Width, sourImage.Height, GraphicsUnit.Pixel);
                g.Dispose();
                //设置压缩质量       
                System.Drawing.Imaging.EncoderParameters encoderParams = new System.Drawing.Imaging.EncoderParameters();
                long[] quality = new long[1];
                quality[0] = 100;
                System.Drawing.Imaging.EncoderParameter encoderParam = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                encoderParams.Param[0] = encoderParam;
                sourImage.Dispose();
                return destBitmap;
            }
            catch
            {
                return bitmap;
            }
        }

        private void flushOcPoints()
        {
            OcPoints.Clear();
            int index = this.listBoxFiles.SelectedIndex;
            var txt = imagePath_ + ocFile_;
            if (File.Exists(txt))
            {
                string mitem = "";
                int cnt = 0;
                foreach (var item in File.ReadAllLines(txt.Trim()))
                {
                    mitem = item;
                    if (cnt++ == index)
                    {
                        break;
                    }
                }

                if (mitem != null && mitem != "")
                {
                    var items = mitem.Trim().Split(' ');
                    if (items.Length == 2 && Convert.ToInt32(items[0]) == 0 && Convert.ToInt32(items[1]) == 0)
                    {
                        return;
                    }
                    for (int i = 0; i < items.Length; )
                        OcPoints.Add(new Point(Convert.ToInt32(items[i++]), Convert.ToInt32(items[i++])));
                }
            }
            else
            {

            }
        }

        // listBoxFiles控件选择文件时触发
        private void listBoxFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 如果是FormMain_KeyDown函数调用 (通过按键调整标框位置大小) 
            // 表明是同一张图片
            if (!isFormMain_KeyDown_ && !AdjustChange_ && !isOcfunc_ && prelistBoxFileIndex_ != this.listBoxFiles.SelectedIndex)
            {
                zoom_ = 1;
                prelistBoxFileIndex_ = this.listBoxFiles.SelectedIndex;
            }

            if (isreset_)
            {
                zoom_ = 1;
                prelistBoxFileIndex_ = this.listBoxFiles.SelectedIndex;
            }
            isreset_ = false;
            isFormMain_KeyDown_ = false;
            AdjustChange_ = false;
            isOcfunc_ = false;
            label7.Text = "";

            if (listBoxFiles.SelectedItem != null)
            {
                var jpgPath = listBoxFiles.SelectedItem.ToString();
                pictureBox1.ImageLocation = jpgPath;
                pictureBox1.Load(jpgPath);

                pictureBox1.Width = pictureBox1.Image.Width * zoom_;
                pictureBox1.Height = pictureBox1.Image.Height * zoom_;

                // 刷新List OcPoints;
                flushOcPoints();

                // 刷新listBoxLabel
                listBoxLable.Items.Clear();
                var txt = imagePath_ + filename_;
                if (File.Exists(txt))
                {
                    int cnt = 0;
                    foreach (var item in File.ReadAllLines(txt.Trim()))
                    {
                        var cur = item.ToString().Split();
                        if (cnt++ == listBoxFiles.SelectedIndex && listBoxFiles.SelectedIndex != 0
                            && (Convert.ToInt32(cur[2]) == 0 || Convert.ToInt32(cur[3]) == 0))
                        {
                            var preitem = listBoxLable.Items[listBoxFiles.SelectedIndex - 1];
                            var it = preitem.ToString().Split();
                            if (Convert.ToInt32(it[2]) != 0 || Convert.ToInt32(it[3]) != 0)
                            {
                                listBoxLable.Items.Add(listBoxLable.Items[listBoxFiles.SelectedIndex - 1]);
                            }
                            else
                            {
                                listBoxLable.Items.Add(item);
                            }
                        }
                        else
                        {
                            listBoxLable.Items.Add(item);
                        } 
                    }

                    SaveGroundTruthFile();

                    //if (listBoxLable.Items.Count > 0 && listBoxFiles.SelectedIndex == listBoxLable.Items.Count)
                    //{
                    //    listBoxLable.Items.Add(listBoxLable.Items[listBoxLable.Items.Count-1]);
                    //    SaveGroundTruthFile();
                    //}
                }

                ClearSelect();  // 删除之前选中的 重新画图
                if (listBoxLable.Items.Count > listBoxFiles.SelectedIndex)
                {
                    listBoxLable.SelectedIndex = listBoxFiles.SelectedIndex;
                    listBoxLable_MouseClick(null, null);
                }

                LoadLabel(listBoxFiles.SelectedIndex);
            }
        }


        private Point RectStartPoint;
        private Rectangle Rect = new Rectangle();
        private Brush selectionBrush = new SolidBrush(Color.FromArgb(128, 72, 145, 220));

        private List<Point> OcPoints = new List<Point>();

        private void pictureBox1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // 画框
            if (this.GroundTruthBox1.Checked)
            {
                RectStartPoint = e.Location;
                Invalidate();
            }

            // 画点
            if (this.OCBox1.Checked)
            {
                Graphics g = ((PictureBox)sender).CreateGraphics();
                g.FillEllipse(Brushes.Red, e.X / zoom_, e.Y / zoom_, 4 * zoom_, 4 * zoom_);
                Point pt = new Point(e.X / zoom_, e.Y / zoom_);
                OcPoints.Add(pt);
                g.Dispose();
                SaveOc_Button_Click(null, null);
                //isOcfunc_ = true;
                //listBoxFiles_SelectedIndexChanged(null, null);
            }
          
        }

        private void pictureBox1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (this.GroundTruthBox1.Checked)
            {
                if (e.Button != MouseButtons.Left)
                    return;

                Debug.WriteLine(e.Location.ToString());
                Point tempEndPoint = e.Location;
                Rect.Location = new Point(
                    Math.Min(RectStartPoint.X, tempEndPoint.X),
                    Math.Min(RectStartPoint.Y, tempEndPoint.Y));
                Rect.Size = new Size(
                    Math.Abs(RectStartPoint.X - tempEndPoint.X),
                    Math.Abs(RectStartPoint.Y - tempEndPoint.Y));
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (this.GroundTruthBox1.Checked || (Rect != null && Rect.Width > 0 && Rect.Height > 0))
                {
                    if (Rect != null && Rect.Width > 0 && Rect.Height > 0)
                    {
                        e.Graphics.FillRectangle(selectionBrush, Rect);
                    }
                }
                
                if (this.OCBox1.Checked || OcPoints.Count != 0)
                {
                    Graphics g = e.Graphics;
                    foreach (Point pt in OcPoints)
                    {
                        g.FillEllipse(Brushes.Red, pt.X * zoom_, pt.Y * zoom_, 4 * zoom_, 4 * zoom_);
                    }
                   // g.Dispose();
                }
                
            }
        }


        void ClearSelect()
        {
            Rect.Size = new Size(0, 0);
            pictureBox1.Refresh();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.GroundTruthBox1.Checked)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (RectStartPoint == e.Location)
                    {
                        return;
                    }

                    if (RectStartPoint.X > e.Location.X || RectStartPoint.Y > e.Location.Y)
                    {
                        MessageBox.Show("亲,只能从左上向右下选");
                        ClearSelect();
                        return;
                    }

                    var height = e.Location.Y - RectStartPoint.Y;
                    var width = e.Location.X - RectStartPoint.X;

                    if (listBoxLable.Items.Count > 0)
                    {
                        if (listBoxLable.Items.Count < listBoxFiles.SelectedIndex)
                        {
                            MessageBox.Show("需要按图片顺序进行标注！");
                            ClearSelect();
                            return;
                        }
                        if (listBoxLable.Items.Count > listBoxFiles.SelectedIndex)
                            listBoxLable.Items.RemoveAt(listBoxFiles.SelectedIndex);
                        // TODO
                        if (listBoxLable.Items.Count >= listBoxFiles.SelectedIndex)
                            listBoxLable.Items.Insert(listBoxFiles.SelectedIndex, String.Format("{0} {1} {2} {3}", RectStartPoint.X / zoom_, RectStartPoint.Y / zoom_, width / zoom_, height / zoom_));
                    }

                    SaveGroundTruthFile();
                }
            }
        }

        private void listBoxFiles_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (listBoxFiles.SelectedItems != null)
                {
                    if (listBoxFiles.SelectedItems.Count == 1)
                    {
                        FileInfo fi = new FileInfo(listBoxFiles.SelectedItem.ToString());
                        if (fi.IsReadOnly)
                        {
                            fi.IsReadOnly = false;
                        }
                        File.Delete(listBoxFiles.SelectedItem.ToString());
                        File.Delete(listBoxFiles.SelectedItem.ToString().Replace(".jpg", ".txt"));
                        listBoxLable.Items.Clear();
                        int i = listBoxFiles.SelectedIndex;
                        listBoxFiles.Items.RemoveAt(listBoxFiles.SelectedIndex);
                        if (i < listBoxFiles.Items.Count)
                        {
                            listBoxFiles.SelectedIndex = i;
                        }
                    }
                    else
                    if (listBoxFiles.SelectedItems.Count == 0)
                    {
                        return;
                    }
                    else
                    {

                        foreach (var item in listBoxFiles.SelectedItems)
                        {
                            FileInfo fi = new FileInfo(item.ToString());
                            if (fi.IsReadOnly)
                            {
                                fi.IsReadOnly = false;
                            }
                            File.Delete(item.ToString());
                            File.Delete(item.ToString().Replace(".jpg", ".txt"));
                            listBoxLable.Items.Clear();
                        }
                        listBoxFiles.Items.Clear();
                        var dir = new DirectoryInfo(outputPath_);
                        foreach (var file in dir.GetFiles())
                        {
                            if (file.Extension == ".jpg")
                            {
                                listBoxFiles.Items.Add(file.FullName);
                            }
                        }
                    }

                }
            }
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            MessageBox.Show("导入功能无法使用");
            //ImportFiles(textBoxImport.Text);
            //Properties.Settings.Default.imagePath = textBoxImport.Text;
            //Properties.Settings.Default.Save();
        }


        void ImportFiles(string path)
        {
            if (Directory.Exists(path))
            {
                var importDir = new DirectoryInfo(path);
                foreach (var file in importDir.GetFiles())
                {
                    if (file.Extension == ".jpg")
                    {
                        string desFilePath = imagePath_ + file.Name;
                        if (!File.Exists(desFilePath))
                        {
                            var image = Bitmap.FromFile(file.FullName);
                            var f = ZoomImage(new Bitmap(image), (int)numericUpDownImportHeight.Value, (int)numericUpDownImportHeight.Value);
                            f.Save(imagePath_ + file.Name, System.Drawing.Imaging.ImageFormat.Jpeg);
                            image.Dispose();

                            listBoxFiles.Items.Add(imagePath_ + file.Name);
                        }

                    }
                }
            }

        }

        private void listBoxLable_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listBoxLable.SelectedIndex != -1)
            {
                var line = listBoxLable.SelectedItem.ToString();
                if (line != "")
                {
                    var items = line.Split(' ');
                    MessageBox.Show(String.Format("TopLeft_X={0} \nTopLeft_Y={1} \nwidth={2}\nheight={3}",
                        items[0], items[1], items[2], items[3]), "样本标注信息");
                }
            }
        }

        private void listBoxLable_MouseClick(object sender, MouseEventArgs e)
        {
            if (listBoxLable.SelectedIndex != -1)
            {
                var line = listBoxLable.SelectedItem.ToString();
                if (line != "")
                {
                    var items = line.Split(' ');
                    var TopLeft_X = Convert.ToDouble(items[0]);
                    var TopLeft_Y = Convert.ToDouble(items[1]);
                    var width = Convert.ToDouble(items[2]);
                    var height = Convert.ToDouble(items[3]);

                    Rect.Location = new Point((int)TopLeft_X * zoom_, (int)TopLeft_Y * zoom_);
                    Rect.Size = new Size((int)width * zoom_, (int)height * zoom_);

                    pictureBox1.Invalidate();

                }
            }
        }

       
        private void buttonOpenImageFolder_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", imagePath_);
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                return;
            }

            if (e.KeyValue != 100 && e.KeyValue != 104 && e.KeyValue != 102 && e.KeyValue != 101
                && e.KeyValue != 87 && e.KeyValue != 83 && e.KeyValue != 65 && e.KeyValue != 68)
            {
                return;
            }

            if (listBoxLable.SelectedIndex != -1)
            {
                var line = listBoxLable.SelectedItem.ToString();
                if (line != "")
                {
                    var items = line.Split(' ');
                    //MessageBox.Show(String.Format("TopLeft_X={0} \nTopLeft_Y={1} \nwidth={2}\nheight={3}\nfilename_={4}\nimage_format={5}",
                    //    items[0], items[1], items[2], items[3], items[4], items[5]), "样本标注信息");

                    int TopLeft_X = Convert.ToInt32(items[0]);
                    int TopLeft_Y = Convert.ToInt32(items[1]);
                    int width = Convert.ToInt32(items[2]);
                    int height = Convert.ToInt32(items[3]);

                    switch (e.KeyValue)
                    {
                        case 104: { TopLeft_Y -= Convert.ToInt32(AdjustY.Value); } break;     // 上
                        case 101: { TopLeft_Y += Convert.ToInt32(AdjustY.Value); } break;     // 下
                        case 100: { TopLeft_X -= Convert.ToInt32(AdjustX.Value); } break;      // 左 
                        case 102: { TopLeft_X += Convert.ToInt32(AdjustX.Value); } break;      // 右
                        case 87: { height -= Convert.ToInt32(AdjustHeight.Value); } break;     // w
                        case 83: { height += Convert.ToInt32(AdjustHeight.Value); } break;     // s
                        case 65: { width -= Convert.ToInt32(AdjustWidth.Value); } break;      // a 
                        case 68: { width += Convert.ToInt32(AdjustWidth.Value); } break;      // d
                    }

                    if (listBoxLable.Items.Count > listBoxFiles.SelectedIndex)
                        listBoxLable.Items.RemoveAt(listBoxFiles.SelectedIndex);
                    listBoxLable.Items.Insert(listBoxFiles.SelectedIndex, String.Format("{0} {1} {2} {3}", TopLeft_X, TopLeft_Y, width, height));
                    //listBoxLable.Items.Add(String.Format("{0} {1} {2} {3} {4} {5}", RectStartPoint.X, RectStartPoint.Y, width, height, filename_, image_format));
                    SaveGroundTruthFile();
                    isFormMain_KeyDown_ = true;
                    listBoxFiles_SelectedIndexChanged(null, null);
                }
            }

        }
        
        private void scaleRect()
        {
            if (listBoxFiles.SelectedIndex != -1)
            {
                this.listBoxLable.SelectedIndex = this.listBoxFiles.SelectedIndex;
                var line = listBoxLable.SelectedItem.ToString();
                if (line != "")
                {
                    var items = line.Split(' ');
                    var TopLeft_X = Convert.ToDouble(items[0]);
                    var TopLeft_Y = Convert.ToDouble(items[1]);
                    var width = Convert.ToDouble(items[2]);
                    var height = Convert.ToDouble(items[3]);
                    Rect.Location = new Point((int)TopLeft_X * zoom_, (int)TopLeft_Y * zoom_);
                    Rect.Size = new Size((int)width * zoom_, (int)height * zoom_);

                    pictureBox1.Invalidate();

                }
            }
        }

        private void enlarge_Click(object sender, EventArgs e)
        {
            zoom_ *= 2;
            pictureBox1.Width = pictureBox1.Width * 2;
            pictureBox1.Height = pictureBox1.Height * 2;
            ClearSelect();  // 删除之前选中的   
            scaleRect();
            //MessageBox.Show(Convert.ToString(pictureBox1.Image.Width), Convert.ToString(pictureBox1.Image.Height));
        }

        private void reduce_Click(object sender, EventArgs e)
        {
            if (zoom_ > 1)
            {
                pictureBox1.Width = pictureBox1.Width / 2;
                pictureBox1.Height = pictureBox1.Height / 2;
                zoom_ /= 2;
                scaleRect();
                //MessageBox.Show(Convert.ToString(pictureBox1.Image.Width), Convert.ToString(pictureBox1.Image.Height));    
            }
            if (zoom_ < 1)
            {
                zoom_ = 1;
                scaleRect();
            }
        }

        void SaveLabelFile(string s)
        {
            var txt = imagePath_ + labelFile_;
            if (!File.Exists(txt))
            {
                File.Create(txt);
            }
            int counter = 0;
            string context = "";
            string line;

            // Read the file and display it line by line.  
            System.IO.StreamReader file =
                new System.IO.StreamReader(txt);
            while ((line = file.ReadLine()) != null)
            {
                //System.Console.WriteLine(line);
                if (counter == listBoxFiles.SelectedIndex)
                    context += s + "\r\n";
                else
                    context += line + "\r\n";
                counter++;
            }

            while (counter++ < listBoxFiles.Items.Count)
            {
                context += "0\r\n";
            }

            file.Close();
            if (context == "")
            {
                File.WriteAllText(txt, s);
            }
            else
            {
                File.WriteAllText(txt, context);
            }

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                return;
            }
                
            if (this.radioButton1.Checked)
            {
                SaveLabelFile(this.radioButton1.Text);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                return;
            }
            if (this.radioButton2.Checked)
            {
                SaveLabelFile(this.radioButton2.Text);
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                return;
            }
            if (this.radioButton3.Checked)
            {
                SaveLabelFile(this.radioButton3.Text);
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                return;
            }
            if (this.radioButton4.Checked)
            {
                SaveLabelFile(this.radioButton4.Text);
            }
        }

        // 加载Label选项
        private void LoadLabel(int index)
        {
            var txt = imagePath_ + labelFile_;
            if (!File.Exists(txt))
            {
                File.Create(txt);
            }

            string line = "";
            int counter = 0;
            System.IO.StreamReader file =
               new System.IO.StreamReader(txt);
            while ((line = file.ReadLine()) != null)
            {
                if (counter == index)
                {
                    break;
                }
                counter++;
            }

            file.Close();
            if (line != null && line != "")
            {
                SaveLabelFile(line);
                int select = Convert.ToInt32(line);
                switch (select)
                {
                    case 0:
                        this.radioButton1.Select();
                        break;
                    case 1:
                        this.radioButton2.Select();
                        break;
                    case 2:
                        this.radioButton3.Select();
                        break;
                    case 3:
                        this.radioButton4.Select();
                        break;
                    default:
                        SaveLabelFile("1");
                        this.radioButton1.Select();
                        break;
                }
            }
            else
            {
                this.radioButton1.Select();
                SaveLabelFile("0");
            }
        }

        
        private void reset_Click(object sender, EventArgs e)
        {
            isreset_ = true;
            listBoxFiles_SelectedIndexChanged(null, null);
        }

        private void SaveOc_Button_Click(object sender, EventArgs e)
        {
            var txt = imagePath_ + ocFile_;
            if (!File.Exists(txt))
            {
                File.Create(txt);
            }

            string context = "";

            for (int i = 0; i < OcPoints.Count; ++i)
            {
                context += OcPoints[i].X + " " + OcPoints[i].Y + " ";
            }

            if (OcPoints.Count == 0)
            {
                context = "0 0";
            }

            int index = listBoxFiles.SelectedIndex;
            int cnt = 0;
            string allcontext = "";
            foreach (var item in File.ReadAllLines(txt.Trim()))
            {
                if (cnt++ == index)
                {
                    allcontext += context + "\r\n";
                }
                else
                {
                    if (item != null && item != "")
                        allcontext += item.ToString() + "\r\n";
                }
            }
           

            using (FileStream _file = new FileStream(@txt, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamWriter writer1 = new StreamWriter(_file))
            {
 
                writer1.WriteLine(allcontext);
                writer1.Flush();
                writer1.Close();

                _file.Close();
            }
        }

        private void AdjustX_ValueChanged(object sender, EventArgs e)
        {
            AdjustChange_ = true;
            listBoxFiles_SelectedIndexChanged(null, null);
        }

        private void AdjustY_ValueChanged(object sender, EventArgs e)
        {
            AdjustChange_ = true;
            listBoxFiles_SelectedIndexChanged(null, null);
        }

        private void AdjustHeight_ValueChanged(object sender, EventArgs e)
        {
            AdjustChange_ = true;
            listBoxFiles_SelectedIndexChanged(null, null);
        }

        private void AdjustWidth_ValueChanged(object sender, EventArgs e)
        {
            AdjustChange_ = true;
            listBoxFiles_SelectedIndexChanged(null, null);
        }

        private void preListBoxFile_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedIndex > 0 && listBoxFiles.SelectedIndex < listBoxFiles.Items.Count)
            {
                this.listBoxFiles.SelectedIndex = this.listBoxFiles.SelectedIndex - 1;
            }
            else
            {
                this.listBoxFiles.SelectedIndex = 0;
            }
            listBoxFiles_SelectedIndexChanged(null, null);
        }

        private void nextListBoxFile_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedIndex >= 0 && listBoxFiles.SelectedIndex < listBoxFiles.Items.Count-1)
            {
                this.listBoxFiles.SelectedIndex = this.listBoxFiles.SelectedIndex + 1;
            }
            else
            {
                this.listBoxFiles.SelectedIndex = listBoxFiles.Items.Count-1;
            }
            listBoxFiles_SelectedIndexChanged(null, null);
        }

        private void OCBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.GroundTruthBox1.Checked)
            {
                this.GroundTruthBox1.CheckState = CheckState.Unchecked;
            }
            //this.OCBox1.CheckState = CheckState.Checked;
        }

        private void GroundTruthBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (this.OCBox1.Checked)
            {
                this.OCBox1.CheckState = CheckState.Unchecked;
            }
            //this.GroundTruthBox1.CheckState = CheckState.Checked;
        }

        private void DeleteOc_Button_Click(object sender, EventArgs e)
        {
            isOcfunc_ = true;
            OcPoints.Clear();
            SaveOc_Button_Click(null, null);
            listBoxFiles_SelectedIndexChanged(null, null);
        }
    }
}