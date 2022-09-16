using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using BlinkStickDotNet;

namespace BlinkTicku_tray
{
    public partial class BlinkTicku_tray : Form
    {
        PerformanceCounter cpuCounter;
        BlinkStick device;
        float usage = 0.0f;
        float light_value = 0;
        byte red, green, blue = 0;
        int refresh_usage = 0;

        public BlinkTicku_tray()
        {
            InitializeComponent();
            Hide();

            //            Easiest to just "find all" if using one device. If multiple devices exist, you could use e.g. String serial = "BS045800-3.0"
            BlinkStick[] devices = BlinkStick.FindAll();

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            //          var GPU_counters = Get_GPU_counters();

            if (devices.Length == 0)
            {
                Console.WriteLine("Could not find any BlinkStick devices. :E");
                return;
            }

            else
            {
                device = devices[0];
                device.OpenDevice();
                Thread.Sleep(500);
                Notify_icon.Visible = true;
                Hide();
                loop_init();
            }
        }

        private async void loop_init()
        {
            int loopResult = await Task.Run(() => loop());
        }

        private int loop()
        {
            refresh_usage = 0;
            while (true)
            {
                refresh_usage++;
                if (refresh_usage > 4)
                {
                    usage = cpuCounter.NextValue();
                    label3.Text = usage.ToString("0") + "%";
                    refresh_usage = 0;
                }
                if (light_value < usage)
                {
                    light_value += 1;
                    if (light_value > 100) light_value = 100;
                }
                if (light_value > usage)
                {
                    light_value -= 1;
                    if (light_value < 10) light_value = 10;
                }
                red = (byte)Math.Round((light_value / 100) * 255);
                green = (byte)Math.Round((255 - (light_value / 100) * 255) * 0.4);
                blue = (byte)Math.Round((255 - (light_value / 100) * 255) * 0.4);
                device.SetColor(red, green, blue);
                Thread.Sleep(75);
            }
            return 1;
        }

        private void Form1_Shown(Object sender, EventArgs e) // doesn't work for some reason
        {
            Notify_icon.Visible = true;
            Hide();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            Notify_icon.Visible = true;
            Hide();
        }

        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            device.TurnOff();
            device.CloseDevice();
        }


        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            Notify_icon.Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Hide();
            Notify_icon.Visible = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            device.TurnOff();
            device.CloseDevice();
            Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            device.TurnOff();
            device.CloseDevice();
            Close();
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void showForm(object sender, EventArgs e)
        {
            Show();
        }

        public static List<PerformanceCounter> Get_GPU_counters()   // unused at the moment
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();
            var all_GPU_counters = counterNames
                                .Where(counterName => counterName.EndsWith("engtype_3D"))
                                .SelectMany(counterName => category.GetCounters(counterName))
                                .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                .ToList();
            return all_GPU_counters;
        }

        public static float Get_GPU_usage(List<PerformanceCounter> useful_GPU_counters)
        {
            float result = 0;
            foreach (PerformanceCounter c in useful_GPU_counters)
            {
                result += c.NextValue();
            }
            return result;
        }
    }
}
