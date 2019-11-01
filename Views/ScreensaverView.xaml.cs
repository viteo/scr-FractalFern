using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Sharpsaver.Views
{
    public partial class ScreensaverView : Window
    {
        private bool isPreviewWindow;
        private Point lastMousePosition = default;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer;
        public WriteableBitmap imageBitmap;

        public ScreensaverView()
        {
            InitializeComponent();
            isPreviewWindow = false;
        }
        public ScreensaverView(IntPtr previewHandle)
        {
            InitializeComponent();
            isPreviewWindow = true;
            Rect parentRect = new Rect();

#if NET30 || NET35
            bool bGetRect = InteropHelper.GetClientRect(previewHandle, ref parentRect);

            HwndSourceParameters sourceParams = new HwndSourceParameters("sourceParams");
            sourceParams.PositionX = 0;
            sourceParams.PositionY = 0;
            sourceParams.Height = parentRect.Height;
            sourceParams.Width = parentRect.Width;
            this.Field.Height = sourceParams.Height;
            this.Field.Width = sourceParams.Width;
            sourceParams.ParentWindow = previewHandle;
            //WS_VISIBLE = 0x10000000; WS_CHILD = 0x40000000; WS_CLIPCHILDREN = 0x02000000;
            sourceParams.WindowStyle = (int)(0x10000000L | 0x40000000L | 0x02000000L);

            //Using HwndSource instead of this.Show() to properly obtain handle of this window
            HwndSource winWPFContent = new HwndSource(sourceParams);
            winWPFContent.Disposed += new EventHandler(this.Dispose);
            winWPFContent.ContentRendered += new EventHandler(this.Window_Loaded);
            winWPFContent.RootVisual = this.Viewbox;
#else
            WindowState = WindowState.Normal;

            IntPtr windowHandle = new WindowInteropHelper(GetWindow(this)).EnsureHandle();

            // Set the preview window as the parent of this window
            InteropHelper.SetParent(windowHandle, previewHandle);

            // Make this window a tool window while preview.
            // A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB.
            // GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x00000080L
            InteropHelper.SetWindowLong(windowHandle, -20, 0x00000080L);
            // Make this a child window so it will close when the parent dialog closes
            // GWL_STYLE = -16, WS_CHILD = 0x40000000
            InteropHelper.SetWindowLong(windowHandle, -16, 0x40000000L);

            // Place the window inside the parent
            InteropHelper.GetClientRect(previewHandle, ref parentRect);

            Width = parentRect.Width;
            Height = parentRect.Height;
#endif
        }

        Int32Rect rect;
        int bytesPerPixel;
        byte[] empty;
        int emptyStride;
        Random r = new Random();
        double x = 0;
        double y = 0;
        byte[] color = { Colors.ForestGreen.B, Colors.ForestGreen.G, Colors.ForestGreen.R, Colors.ForestGreen.A };
        public void Window_Loaded(object sender, EventArgs e)
        {
            imageBitmap = new WriteableBitmap((int)this.Field.Width, (int)this.Field.Height, 96, 96, PixelFormats.Bgr32, null);
            this.Image.Source = imageBitmap;

            rect = new Int32Rect(0, 0, imageBitmap.PixelWidth, imageBitmap.PixelHeight);
            bytesPerPixel = imageBitmap.Format.BitsPerPixel / 8; // typically 4 (BGR32)
            empty = new byte[rect.Width * rect.Height * bytesPerPixel]; // cache this one
            emptyStride = rect.Width * bytesPerPixel;

            //  DispatcherTimer setup
            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(Draw);
            dispatcherTimer.Interval = TimeSpan.FromSeconds(0.02);
            dispatcherTimer.Start();
            //Draw();
        }



        int count;
        private void Draw(object sender, EventArgs e)
        {
            try
            {
                imageBitmap.Lock();
                if (count > 100)
                {
                    imageBitmap.WritePixels(rect, empty, emptyStride, 0);
                    count = 0;
                }
                else count++;
                

                for (int count = 0; count < 5000; count++)
                {
                    var mx = Map(x, -2.1820, 2.6558, 1, this.Field.Width / 2 - 1);
                    var my = Map(y, 0, 9.9983, this.Field.Height - 1, 1);
                    imageBitmap.WritePixels(new Int32Rect((int)mx, (int)my, 1, 1), color, 4, 0);
                    int roll = r.Next(100);
                    double xp = x;
                    if (roll < 1)
                    {
                        x = 0;
                        y = 0.16 * y;
                    }
                    else if (roll < 86)
                    {
                        x = 0.85 * xp + 0.04 * y;
                        y = -0.04 * xp + 0.85 * y + 1.6;
                    }
                    else if (roll < 93)
                    {
                        x = 0.2 * xp - 0.26 * y;
                        y = 0.23 * xp + 0.22 * y + 1.6;
                    }
                    else
                    {
                        x = -0.15 * xp + 0.28 * y;
                        y = 0.26 * xp + 0.24 * y + 0.44;
                    }
                }

            }
            finally
            {
                imageBitmap.Unlock();
            }
        }

        public double Map(double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPreviewWindow) return;

            Point pos = e.GetPosition(this);

            if (lastMousePosition != default)
            {
                if ((lastMousePosition - pos).Length > 3)
                {
                    //Application.Current.Shutdown();
                }
            }
            lastMousePosition = pos;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isPreviewWindow) return;

            //Application.Current.Shutdown();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isPreviewWindow) return;

            //Application.Current.Shutdown();
        }
        internal void Dispose(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
