using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

using CutImage;


using Newtonsoft.Json;

using Image = System.Drawing.Image;
using Size = System.Drawing.Size;


namespace SpriteSheetsUnpacker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string[] paths = new string[1];
        private List<ImageFileInfor> _files = new List<ImageFileInfor>();
        private BitmapImage bitmap = new BitmapImage();
        private int _count;
        private int _max;
        string fileName = "";
        string fileExtention = "";
        string folderName = "";
        private string xmlPath = "";
        public MainWindow()
        {
            InitializeComponent();
            GridView.Visibility = Visibility.Collapsed;
            Progess.Visibility = Visibility.Collapsed;
            ProgessView.Value = 0;
        }
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Lay duong dan
                paths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                Thread process = new Thread(Process);
                process.Start();
            }
        }
        private void Process()
        {
            _files = new List<ImageFileInfor>();
            foreach (var path in paths)
            {
                fileExtention = Path.GetExtension(path);
                if (fileExtention == ".png")
                {
                    if (!Try1(path))
                        if (!Try2(path))
                            if (!Try3(path))
                                if (!Try4(path))
                                    if (!Try5(path))
                                    {
                                        if (Try6(path))
                                        {
                                            if (Try7(path))
                                            {

                                            }
                                        }
                                    }
                }
            }

            Dispatcher.Invoke(() =>
            {
                GridView.Visibility = Visibility.Visible;
                Progess.Visibility = Visibility.Visible;
                ProgessView.Value = 0;
            });

            _count = 0;
            _max = _files.Count;
            if (_max <= 0)
            {
                return;
            }
            Bitmap bitmapSource = null;
            string rootName = _files[0].RootName;
            bitmapSource = new Bitmap(_files[0].SourcePath);
            foreach (var imageFileInfor in _files)
            {
                if (imageFileInfor.RootName != rootName)
                {
                    bitmapSource.Dispose();
                    rootName = imageFileInfor.RootName;
                    bitmapSource = new Bitmap(imageFileInfor.SourcePath);
                }

                Bitmap crop;
                if (imageFileInfor.Rotate)
                {
                    crop = bitmapSource.Clone(new RectangleF(
                    imageFileInfor.X, imageFileInfor.Y, imageFileInfor.Height, imageFileInfor.Width
                    ), bitmapSource.PixelFormat);
                    crop.RotateFlip(RotateFlipType.Rotate270FlipNone);
                }
                else
                {
                    Debug.WriteLine("{0}, {1}", imageFileInfor.Width, imageFileInfor.Height);
                    crop = bitmapSource.Clone(new RectangleF(
                    imageFileInfor.X, imageFileInfor.Y, imageFileInfor.Width, imageFileInfor.Height
                    ), bitmapSource.PixelFormat);
                }
                if (!imageFileInfor.ResultPath.Contains(".png"))
                {
                    imageFileInfor.ResultPath += ".png";
                }
                var saveFolder = System.IO.Path.GetDirectoryName(imageFileInfor.ResultPath);
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(folderName);
                }
                crop.Save(imageFileInfor.ResultPath);
                crop.Dispose();
                var name = imageFileInfor.Name;
                Dispatcher.Invoke(() =>
                {
                    _count++;
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imageFileInfor.ResultPath);
                    bitmap.EndInit();
                    Status.Content = imageFileInfor.ResultPath;
                    ImageView.Source = bitmap;
                    ProgessView.Value = (float)_count / _max;
                    Progess.Content = _count + "/" + _max;
                });
            }
            bitmapSource.Dispose();
            Dispatcher.Invoke(() =>
            {
                Status.Content = "Done";
                GridView.Visibility = Visibility.Collapsed;
                Progess.Visibility = Visibility.Collapsed;
            });
        }

        private bool Try1(string path)
        {
            try
            {
                xmlPath = path.Replace(".png", ".xml");
                if (File.Exists(xmlPath))
                {
                    fileName = Path.GetFileName(path);
                    folderName = Path.GetDirectoryName(path) + "/Result" + fileName.Replace(".png", "");

                    if (!Directory.Exists(folderName))
                        Directory.CreateDirectory(folderName);

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(new StreamReader(xmlPath));

                    var subTextures = xmlDocument.DocumentElement.SelectNodes("SubTexture");
                    foreach (XmlNode subTexture in subTextures)
                    {
                        _files.Add(new ImageFileInfor
                        {
                            X = Convert.ToInt32(subTexture.Attributes["x"].Value),
                            Y = Convert.ToInt32(subTexture.Attributes["y"].Value),
                            Width = Convert.ToInt32(subTexture.Attributes["width"].Value),
                            Height = Convert.ToInt32(subTexture.Attributes["height"].Value),
                            Name = subTexture.Attributes["name"].Value,
                            SourcePath = path,
                            FolderPath = folderName,
                            ResultPath = folderName + "/" + subTexture.Attributes["name"].Value.Replace("/", "") + ".png",
                            RootName = fileName,
                            Rotate = false,
                        });
                    }
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool Try2(string path)
        {
            try
            {
                xmlPath = path.Replace(".png", ".plist");
                if (File.Exists(xmlPath))
                {
                    fileName = Path.GetFileName(path);
                    folderName = Path.GetDirectoryName(path) + "/" + fileName.Replace(".png", "");

                    if (!Directory.Exists(folderName))
                        Directory.CreateDirectory(folderName);

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(new StreamReader(xmlPath));

                    var dict = xmlDocument.DocumentElement.SelectSingleNode("dict").SelectNodes("dict")[0];
                    var subDicts = dict.SelectNodes("dict");
                    var keys = dict.SelectNodes("key");
                    var leng = keys.Count;
                    string patten = "([0-9]+)";
                    Regex regex = new Regex(patten);
                    MatchCollection match;
                    for (int i = 0; i < leng; i++)
                    {
                        if (subDicts[i].SelectNodes("string")[0] == null)
                        {
                            return false;
                        }
                        string str = subDicts[i].SelectNodes("string")[0].InnerText;
                        match = regex.Matches(str);
                        ImageFileInfor img = new ImageFileInfor()
                        {
                            X = Convert.ToInt32(match[0].Value),
                            Y = Convert.ToInt32(match[1].Value),
                            Width = Convert.ToInt32(match[2].Value),
                            Height = Convert.ToInt32(match[3].Value),
                            Name = keys[i].InnerText,
                            SourcePath = path,
                            FolderPath = folderName,
                            ResultPath = folderName + "/" + keys[i].InnerText.Replace("/", ""),
                            Rotate = subDicts[i].SelectSingleNode("true") != null,
                            RootName = fileName,
                        };
                        _files.Add(img);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool Try3(string path)
        {
            try
            {
                xmlPath = path.Replace(".png", ".plist");
                if (File.Exists(xmlPath))
                {
                    fileName = Path.GetFileName(path);
                    folderName = Path.GetDirectoryName(path) + "/" + fileName.Replace(".png", "");

                    if (!Directory.Exists(folderName))
                        Directory.CreateDirectory(folderName);

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(new StreamReader(xmlPath));

                    var dict = xmlDocument.DocumentElement.SelectSingleNode("dict").SelectNodes("dict")[0];
                    var subDicts = dict.SelectNodes("dict");
                    var keys = dict.SelectNodes("key");
                    var leng = keys.Count;
                    string patten = "([0-9]+)";
                    Regex regex = new Regex(patten);
                    MatchCollection match;
                    for (int i = 0; i < leng; i++)
                    {
                        if (subDicts[i].SelectNodes("string")[0] == null)
                        {
                            return false;
                        }
                        string str = subDicts[i].SelectNodes("string")[0].InnerText;
                        match = regex.Matches(str);
                        ImageFileInfor img = new ImageFileInfor()
                        {
                            X = Convert.ToInt32(match[0].Value),
                            Y = Convert.ToInt32(match[1].Value),
                            Width = Convert.ToInt32(match[2].Value),
                            Height = Convert.ToInt32(match[3].Value),
                            Name = keys[i].InnerText,
                            SourcePath = path,
                            FolderPath = folderName,
                            ResultPath = folderName + "/" + keys[i].InnerText.Replace("/", ""),
                            Rotate = subDicts[i].SelectSingleNode("true") != null,
                            RootName = fileName,
                        };
                        _files.Add(img);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool Try4(string path)
        {
            try
            {
                xmlPath = path.Replace(".png", ".atlas");
                if (File.Exists(xmlPath))
                {
                    fileName = Path.GetFileName(path);
                    folderName = Path.GetDirectoryName(path) + "/" + fileName.Replace(".png", "");

                    if (!Directory.Exists(folderName))
                        Directory.CreateDirectory(folderName);

                    string data = File.ReadAllText(xmlPath);
                    string[] array = data.Split('\n');
                    string patten = "([0-9]+)";
                    Regex regex = new Regex(patten);
                    MatchCollection match;
                    for (int i = 6; i < array.Length - 1; i += 7)
                    {

                        ImageFileInfor img = new ImageFileInfor();
                        img.Name = array[i];
                        img.Rotate = array[i + 1].Contains("false") ? false : true;
                        img.SourcePath = path;
                        img.FolderPath = folderName;
                        img.ResultPath = folderName + "/" + img.Name;
                        img.RootName = fileName;

                        match = regex.Matches(array[i + 2]);
                        img.X = Convert.ToInt32(match[0].Value);
                        img.Y = Convert.ToInt32(match[1].Value);

                        match = regex.Matches(array[i + 3]);
                        img.Width = Convert.ToInt32(match[0].Value);
                        img.Height = Convert.ToInt32(match[1].Value);

                        _files.Add(img);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool Try5(string path)
        {
            try
            {
                xmlPath = path.Replace(".png", ".txt");
                if (File.Exists(xmlPath))
                {
                    fileName = Path.GetFileName(path);
                    folderName = Path.GetDirectoryName(path) + "/" + fileName.Replace(".png", "");

                    if (!Directory.Exists(folderName))
                        Directory.CreateDirectory(folderName);

                    string data = File.ReadAllText(xmlPath);



                    string[] array = data.Split('\n');
                    string patten = "([0-9]+)";
                    Regex regex = new Regex(patten);
                    MatchCollection match;
                    for (int i = 2; i < array.Length - 12; i += 8)
                    {
                        ImageFileInfor img = new ImageFileInfor();
                        img.Name = array[i].Replace("/", string.Empty).Replace(":", string.Empty).Replace("\"", string.Empty).Replace("\r", string.Empty);
                        img.Rotate = array[i + 3].Contains("false") ? false : true;
                        img.SourcePath = path;
                        img.FolderPath = folderName;
                        img.ResultPath = folderName + "/" + img.Name;
                        img.RootName = fileName;

                        match = regex.Matches(array[i + 2]);
                        img.X = Convert.ToInt32(match[0].Value);
                        img.Y = Convert.ToInt32(match[1].Value);

                        //match = regex.Matches(array[i + 3]);
                        img.Width = Convert.ToInt32(match[2].Value);
                        img.Height = Convert.ToInt32(match[3].Value);
                        //Debug.WriteLine(i);
                        _files.Add(img);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private bool Try6(string path)
        {
            try
            {
                xmlPath = path.Replace(".png", ".plist");
                if (File.Exists(xmlPath))
                {
                    fileName = Path.GetFileName(path);
                    folderName = Path.GetDirectoryName(path) + "/" + fileName.Replace(".png", "");

                    if (!Directory.Exists(folderName))
                        Directory.CreateDirectory(folderName);

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(new StreamReader(xmlPath));

                    var dict = xmlDocument.DocumentElement.SelectSingleNode("dict").SelectNodes("dict")[0];
                    var subDicts = dict.SelectNodes("dict");
                    var keys = dict.SelectNodes("key");
                    var leng = keys.Count;
                    string patten = "([0-9]+)";
                    Regex regex = new Regex(patten);
                    MatchCollection match;
                    for (int i = 0; i < leng; i++)
                    {
                        var nodes = subDicts[i].ChildNodes;
                        ImageFileInfor img = new ImageFileInfor()
                        {
                            X = Convert.ToInt32(nodes[13].InnerText),
                            Y = Convert.ToInt32(nodes[15].InnerText),
                            Width = Convert.ToInt32(nodes[11].InnerText),
                            Height = Convert.ToInt32(nodes[1].InnerText),
                            Name = keys[i].InnerText,
                            SourcePath = path,
                            FolderPath = folderName,
                            ResultPath = folderName + "/" + keys[i].InnerText.Replace("/", ""),
                            Rotate = subDicts[i].SelectSingleNode("true") != null,
                            RootName = fileName,
                        };
                        _files.Add(img);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private bool Try7(string path)
        {
            try
            {
                xmlPath = path.Replace(".png", ".atlas");
                if (File.Exists(xmlPath))
                {
                    fileName = Path.GetFileName(path);
                    folderName = Path.GetDirectoryName(path) + "/" + fileName.Replace(".png", "");

                    if (!Directory.Exists(folderName))
                        Directory.CreateDirectory(folderName);

                    string data = File.ReadAllText(xmlPath);
                    string[] array = data.Split('\n');
                    string patten = "([0-9]+)";
                    Regex regex = new Regex(patten);
                    MatchCollection match;
                    for (int i = 6; i < array.Length - 1; i += 7)
                    {

                        ImageFileInfor img = new ImageFileInfor();
                        img.Name = array[i];
                        img.Rotate = array[i + 1].Contains("false") ? false : true;
                        img.SourcePath = path;
                        img.FolderPath = folderName;
                        img.ResultPath = folderName + "/" + img.Name;
                        img.RootName = fileName;

                        match = regex.Matches(array[i + 2]);
                        img.X = Convert.ToInt32(match[0].Value);
                        img.Y = Convert.ToInt32(match[1].Value);

                        match = regex.Matches(array[i + 3]);
                        img.Width = Convert.ToInt32(match[0].Value);
                        img.Height = Convert.ToInt32(match[1].Value);

                        _files.Add(img);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

}


[Serializable]
public class P5
{
    public P52[] frames = new P52[1];
    //public meta meta = new meta();
}

[Serializable]
public class P52
{
    //public MyRect frame = new MyRect();
    //public bool rotated;
    //public bool trimmed;
    //public MyRect spriteSourceSize = new MyRect();
    //public sourceSize sourceSize = new sourceSize();
}

[Serializable]
public class MyRect
{
    public int x;
    public int y;
    public int w;
    public int h;
}
[Serializable]
public class sourceSize
{

    public int w;
    public int h;
}