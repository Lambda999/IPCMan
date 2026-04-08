using CefClient.Common;
using CefClient.Handler;
using CefSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Resolution;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CefClient
{
    public partial class MainFormv2 : Form
    {
        #region Win32
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(int hWnd, int msg, int wParam, ref COPYDATASTRUCT lParam);
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);
        const int WM_COPYDATA = 0x004A;
        const int WM_MYSYMPLE = 0x005A;
        #endregion

        private SynchronizationContext sync;
        private int hMainWnd = 0;
        private bool isHiddenMode = true;
        private string uuid = string.Empty;

        #region  LogWrite
        void LogCallback(params object[] parameters)
        {

            var callee = new StackFrame(1, false).GetMethod();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Callback: ");
            sb.Append(callee.Name);
            sb.Append("(");
            var pm = callee.GetParameters();
            for (var i = 0; i <= pm.Length - 1; i++)
            {
                sb.Append(pm[i].Name);
                if (parameters.Length > i)
                {
                    sb.Append(" = {");
                    if (parameters[i] != null)
                    {
                        sb.Append(parameters[i].ToString());
                    }
                    else
                    {
                        sb.Append("null");
                    }
                    sb.Append("}");
                }
                if (i < pm.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(")");
            LogWriteLine(sb.ToString());
        }


        public void LogWriteLine()
        {
            LogWrite(Environment.NewLine);
        }

        public void LogWriteLine(string msg)
        {
            LogWrite(msg + Environment.NewLine);
        }

        public void LogWriteLine(string msg, params object[] parameters)
        {
            LogWrite(msg + Environment.NewLine, parameters);
        }

        public void LogWrite(string msg, params object[] parameters)
        {
            LogWrite(string.Format(msg, parameters));
        }
        public void LogWrite(string msg)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => { LogWrite(msg); }));
                return;
            }
            LogTextBox.AppendText($"{System.DateTime.Now.ToString("[HH:mm:ss]")} {msg}");
            //LogTextBox.SelectionStart = LogTextBox.TextLength - 1;
            LogTextBox.ScrollToCaret();

        }
        #endregion

        private async Task ResolveMessage(string value)
        {
            var message = (JObject)JsonConvert.DeserializeObject(value);
            if (message["Msg"].ToString().Equals("LOAD"))
            {
                var args = (JObject)JsonConvert.DeserializeObject(message["Data"].ToString());
                //LogWriteLine(value);
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    var w = new WebViewForm(args)
                    {
                        StartPosition = FormStartPosition.Manual,
                        Location = new Point(0, 0),
                        Size = new int[] { 1, 2 }.Contains(Convert.ToInt32(args["os"].ToString())) ? new Size(860, 1000) : new Size(1920, 1080),
                    };

                    w.OnLogEventArgs += (s, arg) =>
                    {
                        LogWriteLine(arg.Data);
                    };

                    w.FormClosed += (s, arg) =>
                    {

                    };
                    w.Show();
                }));
            }
            else if (message["Msg"].ToString().Equals("STOP"))
            {
                await Task.Run(() =>
                {
                    LogWriteLine("5秒后退出该进程");
                    SpinWait.SpinUntil(() => false, 5000);
                    sync.Post((p) =>
                    {
                        System.Environment.Exit(0);
                    }, null);
                });
            }
            else if (message["Msg"].ToString().Equals("SHOW"))
            {
                this.isHiddenMode = false;
            }
            else if (message["Msg"].ToString().Equals("HIDE"))
            {
                this.isHiddenMode = true;
            }
        }
        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case WM_COPYDATA:
                    COPYDATASTRUCT data = new COPYDATASTRUCT();
                    Type myType = data.GetType();
                    data = (COPYDATASTRUCT)m.GetLParam(myType);
                    if (!string.IsNullOrWhiteSpace(data.lpData))
                    {
                        Task.Run(async () => await ResolveMessage(data.lpData));
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        public MainFormv2()
        {
            InitializeComponent();
            this.sync = SynchronizationContext.Current;
            var commandLineArgs = System.Environment.GetCommandLineArgs();
            foreach (var c in commandLineArgs)
            {
                if (c.StartsWith("hwnd="))
                {
                    hMainWnd = Convert.ToInt32(c.Split('=')[1]);
                }
                else if (c.StartsWith("showform="))
                {
                    isHiddenMode = Convert.ToBoolean(c.Split('=')[1]);
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                    SetVisibleCore(false);
                }
                else if (c.StartsWith("uuid="))
                {
                    uuid = c.Split('=')[1];
                }
                else if (c.StartsWith("task="))
                {
                    var task = c.Split('=')[1];
                    LogWriteLine(task);
                }
            }
            SendRegMessage();
            LogWriteLine($"{Process.GetCurrentProcess().Id},{this.Handle},{System.DateTime.Now.ToString("HH:mm:ss")},{this.isHiddenMode}");


        }
        protected override void SetVisibleCore(bool value)
        {
#if DEBUG
            value = true;
#else
            value =false;
#endif
            base.SetVisibleCore(value);
        }

        private void SendRegMessage()
        {
            var currentProcess = Process.GetCurrentProcess();
            var message = JsonConvert.SerializeObject(JObject.FromObject(new
            {
                Msg = "REG",
                WindowHandle = (int)this.Handle,
                uuid = this.uuid,
                ProcessId = currentProcess.Id,
                ProcessPath = currentProcess.MainModule.FileName,
            }));

            byte[] sarr = System.Text.Encoding.Default.GetBytes(message);
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)100;
            cds.lpData = message;
            cds.cbData = sarr.Length + 1;
            SendMessage(hMainWnd, WM_COPYDATA, 0, ref cds);
        }



        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private static async Task<string> GetIp(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        private async Task<string> GetDev()
        {
            var client = new HttpClient();
            try
            {
                HttpResponseMessage response = await client.GetAsync($"http://117.21.200.18:9000/api/getdev.php?type=android&count=1&t={System.DateTime.Now.Ticks}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                else
                {
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }



        private async Task<string> GetTask(string name)
        {
            var client = new HttpClient();
            try
            {

                HttpResponseMessage response = await client.GetAsync($"http://117.21.200.148/client-v5.php?type=1&action=getTask&task={name}&test=0&_t={System.DateTime.Now.Ticks}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                else
                {
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }





        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                return;
            }

            Task.Factory.StartNew(async () =>
                {
                    var content = await GetTask("6-test");
                    var task = JObject.Parse(content)["task"][0];
                    var dev = JObject.Parse((await GetDev()))["data"][0];
                    var args = JObject.Parse(Properties.Resources.JS_ARGS);
                    args["task"] = task;
                    args["dev"] = dev;
                    args["url"] = task["url"];
                    args["referer"] = null;
                    args["click_jump"] = false;
                    args["IsProxyMode"] = false;
                    args["proxy_server"] = null;// proxy_server.Trim();
                    args["IsHiddenMode"] = false;
                    this.BeginInvoke(new MethodInvoker(() =>
                    {
                        new WebViewForm(args)
                        {
                            StartPosition = FormStartPosition.Manual,
                            Location = new Point(0, 0),
                            Size = new int[] { 1, 2 }.Contains(Convert.ToInt32(args["os"].ToString())) ? new Size(860, 1000) : new Size(1920, 1080),
                        }.Show();
                    }));
                });

        }
    }
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }
}
