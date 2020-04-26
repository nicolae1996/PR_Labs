using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Shared;

namespace Client
{
    public partial class ClientForm : Form
    {
        private State state = new State();

        private List<byte> _imageBytes = new List<byte>();

        private bool _started;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, 9669);
            var u = new UdpClient(endPoint);

            BeginReceive(u, endPoint);
        }

        private void BeginReceive(UdpClient udpClient, IPEndPoint endPoint)
        {
            udpClient.BeginReceive(ar =>
            {
                var c = (State)ar.AsyncState;
                var receivedBytes = udpClient.EndReceive(ar, ref endPoint);

                if (receivedBytes.Length == 15)
                {
                    _started = true;
                    if (_imageBytes.Any())
                    {
                        var img = GetImage(_imageBytes.ToArray());
                        streamPicture.Image = img;
                    }

                    try
                    {
                        var str = Encoding.ASCII.GetString(receivedBytes);
                        var info = JsonConvert.DeserializeObject<ImageInfo>(str);
                        //_imageBytes = new byte[info.Size];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    _imageBytes = new List<byte>();
                }
                else
                {
                    if (_started)
                    {
                        if (receivedBytes.Length == State.bufSize)
                        {
                            _imageBytes.AddRange(receivedBytes);
                        }
                        else
                        {
                            //last packet
                            _imageBytes.AddRange(receivedBytes);
                        }
                    }
                }

                BeginReceive(udpClient, endPoint);
            }, state);
        }

        public Image GetImage(byte[] byteArrayIn)
        {
            using (var ms = new MemoryStream(byteArrayIn))
            {
                return Image.FromStream(ms);
            }
        }
    }
}
