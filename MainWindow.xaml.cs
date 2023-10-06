using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using InspectorApp.Models;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.IO;
using System.Windows.Media.Imaging;
using OpenQA.Selenium;
using InspectorApp.ThemedSplashScreen;


namespace InspectorApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            GetApps();

        }
        XDocument xdoc = new XDocument();
        Dictionary<string, string> processes = new Dictionary<string, string>();
        private string WinAppDriverUrl = "http://127.0.0.1:4723";
        Bitmap bmp;

        private XDocument GetXDocument(string name)
        {
            WindowsDriver<WindowsElement> driver = GetDriver();
            WindowsDriver<WindowsElement> session = GetSession(name, driver);

            GetScreenShot(session);
            string appSource = session.PageSource;
            SetTreeList(appSource);

            return XDocument.Parse(appSource);
        }
        private WindowsDriver<WindowsElement> GetSession(string name, WindowsDriver<WindowsElement> driver)
        {
            var app = driver.FindElementByName(name);
            var windowHandle = app.GetAttribute("NativeWindowHandle");
            Console.WriteLine($"Window Handle: {windowHandle}");
            var handleInt = (int.Parse(windowHandle)).ToString("x");
            AppiumOptions windowOptions = new AppiumOptions();
            windowOptions.AddAdditionalCapability("appTopLevelWindow", handleInt);
            WindowsDriver<WindowsElement> session = new WindowsDriver<WindowsElement>(new Uri(WinAppDriverUrl), windowOptions);
            return session;
        }
        private WindowsDriver<WindowsElement> GetDriver()
        {
            AppiumOptions options = new AppiumOptions();
            options.AddAdditionalCapability("app", "Root");
            WindowsDriver<WindowsElement> driver = new WindowsDriver<WindowsElement>(new Uri(WinAppDriverUrl), options);
            return driver;
        }
        private void SetTreeList(string appSource)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(appSource);

            TreeListNode treeListNode = CreateRootNode(new ProjectObject() { Name = xmlDoc.DocumentElement.Name });

            CreateTreeView(xmlDoc.DocumentElement, treeListNode);
            treeView.Nodes.Add(treeListNode);
            treeView.ParentFieldName = xmlDoc.DocumentElement.Name;
        }
        private void GetScreenShot(WindowsDriver<WindowsElement> driver)
        {
            Screenshot sc = ((ITakesScreenshot)driver).GetScreenshot();
            bmp = Image.FromStream(new MemoryStream(sc.AsByteArray)) as Bitmap;
            BitmapImage img = BitmapToImageSource(bmp);
            ssImage.Source = img;
        }
        private TreeListNode CreateRootNode(object dataObject)
        {
            TreeListNode rootNode = new TreeListNode(dataObject);
            return rootNode;
        }
        private TreeListNode CreateChildNode(TreeListNode parentNode, object dataObject)
        {
            TreeListNode childNode = new TreeListNode(dataObject);
            parentNode.Nodes.Add(childNode);
            return childNode;
        }
        private void GetApps()
        {
            try
            {
                Process[] allApps = Process.GetProcesses();
                foreach (Process app in allApps)
                {
                    if (app.MainWindowTitle.Length > 0)
                    {
                        processes.Add(app.Id.ToString(), app.MainWindowTitle);
                        cbxApps.Items.Add(app.MainWindowTitle);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                string message = "A exception with a null response was thrown sending an HTTP request to the remote WebDriver server for URL http://127.0.0.1:4723/session. The status of the exception was UnknownError, and the message was: No connection could be made because the target machine actively refused it. [::ffff:127.0.0.1]:4723 (127.0.0.1:4723)";
                if (ex.Message.Contains(message))
                {
                    MessageBox.Show("Please Run Windows Application Driver and Try Again");
                }
                else
                {
                    MessageBox.Show(ex.Message);
                }
                Application.Current.Shutdown();
            }
        }
        private void CreateTreeView(XmlNode xmlElement, TreeListNode treeList)
        {
            foreach (XmlNode element in xmlElement.ChildNodes)
            {
                TreeListNode tree = CreateChildNode(treeList, new StageObject() { Name = element.Name });

                if (element.ChildNodes != null)
                {
                    CreateTreeView(element, tree);
                }

                treeList.Nodes.Add(tree);
            }
        }
        private string GetXPath(TreeListNode node)
        {
            int index = GetIndex(node);
            string xpath = "/" + ((ProjectObject)node.Content).Name;

            if (index != -1) xpath += "[" + index + "]";

            if (node.ParentNode != null)
            {
                TreeListNode parent = node.ParentNode;

                while (parent != null)
                {
                    int parentIndex = GetIndex(parent);

                    if (parentIndex != -1) xpath = "/" + ((ProjectObject)parent.Content).Name + "[" + parentIndex + "]" + xpath;
                    else xpath = "/" + ((ProjectObject)parent.Content).Name + xpath;

                    if (parent.ParentNode == null)
                    {
                        break;
                    }
                    parent = parent.ParentNode;
                }
            }
            xpathLabel.Text = xpath;

            return xpath;
        }
        private int GetIndex(TreeListNode item)
        {
            int index = 1;
            if (item.ParentNode != null)
            {
                TreeListNode parent = item.ParentNode;
                foreach (TreeListNode child in parent.Nodes)
                {
                    if (child.Id == item.Id) return index;
                    else if (((ProjectObject)child.Content).Name == ((ProjectObject)item.Content).Name && child.Id != item.Id) index++;
                }
                return -1;
            }

            return -1;
        }
        private void btnCopyXpath_Click(object sender, RoutedEventArgs e)
        {
            if (xpathLabel.Text.ToString()?.Length > 0)
            {
                string? labelText = xpathLabel.Text.ToString();

                Clipboard.SetText(labelText);
            }
        }
        private void btnAttach_Click(object sender, RoutedEventArgs e)
        {      
            try
            {                
                treeView.Nodes.Clear();
                if (cbxApps.SelectedItem != null)
                {
                    var loadingViewModel = new DXSplashScreenViewModel() { Title = "InspectorApp" };
                    SplashScreenManager.Create(() =>new Loading(), loadingViewModel).Show(
                        startupLocation: WindowStartupLocation.CenterScreen
                        );

                    string? path = cbxApps.SelectedItem.ToString();
                    string value = processes.FirstOrDefault(x => x.Value == path).Value;
                    xdoc = GetXDocument(value);
                }
                else
                {
                    MessageBox.Show("Please Select An App");
                }

            }
            catch (Exception ex)
            {
                if (ex.TargetSite.DeclaringType.Name == "HttpCommandExecutor") MessageBox.Show("Please Run WinAppDriver and Try Again");
                else if (ex.TargetSite.DeclaringType.Name == "RemoteWebDriver") MessageBox.Show("Please Restart The Selected App and Try Again");
                else MessageBox.Show(ex.Message);
            }
            SplashScreenManager.CloseAll();

        }
        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                List<PropertyModel> properties = new List<PropertyModel>();
                TreeListNode node = treeView.FocusedNode;
                
                
                if (node != null)
                {
                    TreeListNode parent = FindParent(node);
                    string xpath = GetXPath(node);
                    XElement? selectedElement = xdoc.XPathSelectElement(xpath);
                    if (selectedElement != null)
                    {
                        MarkElement(selectedElement);
                        foreach (XAttribute atr in selectedElement.Attributes())
                        {
                            properties.Add(new PropertyModel
                            {
                                Name = atr.Name.ToString(),
                                Value = atr.Value != null ? atr.Value.ToString() : ""
                            });

                        }
                        propertiesTable.ItemsSource = properties;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            cbxApps.Items.Clear();
            cbxApps.SelectedText = string.Empty;
            ssImage.Source = null;
            treeView.Nodes.Clear();
            processes.Clear();
            propertiesTable.ItemsSource = null;
            xpathLabel.Text = string.Empty;
            GetApps();
        }        
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
        private TreeListNode FindParent(TreeListNode node)
        {
            if (node == null)
            {
                return null;
            }

            // Eğer bu düğümün bir üst düğümü yoksa, bu düğüm en üst parent'tır (root düğümüdür)
            if (node.ParentNode == null)
            {
                return node;
            }

            // Üst düğümü varsa, bir üst düğüme gitmek için FindRootNode metodunu tekrar çağırın
            return FindParent(node.ParentNode);
        }
        private void MarkElement(XElement element)
        {
            Bitmap bitmap = new Bitmap(bmp);

            int x = Convert.ToInt32(element.Attribute("x").Value.ToString());
            int y = Convert.ToInt32(element.Attribute("y").Value.ToString());
            int height = Convert.ToInt32(element.Attribute("height").Value.ToString());
            int width = Convert.ToInt32(element.Attribute("width").Value.ToString());


            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                int x1 = x;
                int y1 = y;

                using (Pen pen = new Pen(Color.Red, 4))
                {
                    graphics.DrawRectangle(pen, x1, y1, width, height);
                }
            }
            BitmapImage img = BitmapToImageSource(bitmap);
            ssImage.Source = img;

        }
    }
}
