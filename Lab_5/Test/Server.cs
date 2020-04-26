using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Shared;

namespace Test
{
    public partial class Server : Form
    {
        private State state = new State();

        private RichTextBox _logComponent;

        private List<string> _logs = new List<string>();


        public Server()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Init
        /// </summary>
        private void Init()
        {
            var e = new IPEndPoint(IPAddress.Loopback, 9696);
            var u = new UdpClient(e);
            _logComponent = LogComponent;
            BeginSend(u);
            label1.Text = $"Server started on {IPAddress.Loopback} with port {9696}";
        }

        private void BeginSend(UdpClient udpClient, IPEndPoint e = null, byte[] source = null, int index = 0)
        {
            if (e == null) e = new IPEndPoint(IPAddress.Loopback, 9669);

            var sendNewImg = source == null || index == 0;

            var image = sendNewImg
                ? ImageToByteArray(GrabDesktop())
                : source;

            if (sendNewImg)
            {
                WriteLog($"Image sent. Size: {image.Length}");
                var info = new ImageInfo
                {
                    Size = image.Length
                };
                var message = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(info));
                udpClient.BeginSend(message, message.Length, e, ar =>
                {

                }, state);
            }

            var nextIndex = index + State.bufSize;
            var reset = false;

            var take = State.bufSize;
            if (nextIndex > image.Length)
            {
                nextIndex = take;
                reset = true;
            }

            var data = image.Skip(index).Take(take).ToArray();

            udpClient.BeginSend(data, data.Length, e, ar =>
            {
                if (reset) nextIndex = 0;
                BeginSend(udpClient, e, image, nextIndex);
            }, state);
        }

        private int _screenN = 0;

        private Image GrabDesktop()
        {
            var bound = Screen.PrimaryScreen.Bounds;
            var screenShot = new Bitmap(bound.Width, bound.Height, PixelFormat.Format32bppArgb);
            var graphics = Graphics.FromImage(screenShot);
            graphics.CopyFromScreen(bound.X, bound.Y, 0, 0, bound.Size, CopyPixelOperation.SourceCopy);

            var curSize = new Size(32, 32);
            Cursors.Default.Draw(graphics, new Rectangle(Cursor.Position, curSize));


            var drawFont = new Font("Arial", 40);
            var drawBrush = new SolidBrush(Color.LawnGreen);

            var x = 1100.0F;
            var y = bound.Height - 100;

            var drawFormat = new StringFormat
            {
                FormatFlags = StringFormatFlags.DisplayFormatControl
            };
            graphics.DrawString("Stream by Lupei Nicolae", drawFont, drawBrush, x, y, drawFormat);

            graphics.DrawString($"Screen {++_screenN}", new Font("Arial", 24), drawBrush, bound.Width - 400, 100, drawFormat);
            return screenShot;
        }

        public byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public void WriteLog(string log)
        {
            Task.Run(() =>
            {
                _logs = _logs.Prepend(log).ToList();
                ThreadHelperClass.SetText(this, _logComponent, string.Join("\n", _logs));
            });
        }

        private void Server_Load(object sender, EventArgs e)
        {
            Init();
        }
    }


    public static class ThreadHelperClass
    {
        delegate void AppendTextCallback(Form f, Control ctrl, string text);
        /// <summary>
        /// Set text property of various controls
        /// </summary>
        /// <param name="form">The calling form</param>
        /// <param name="ctrl"></param>
        /// <param name="text"></param>
        public static void SetText(Form form, Control ctrl, string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctrl.InvokeRequired)
            {
                var d = new AppendTextCallback(SetText);
                form.Invoke(d, new object[] { form, ctrl, text });
            }
            else
            {
                ctrl.Text = text;
            }
        }
    }
}