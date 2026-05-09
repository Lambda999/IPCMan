namespace MainClient
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupBox1 = new GroupBox();
            taskInfoListView = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();
            columnHeader6 = new ColumnHeader();
            groupBox2 = new GroupBox();
            label38 = new Label();
            label39 = new Label();
            numericUpDown_IpTtl = new NumericUpDown();
            linkLabel2 = new LinkLabel();
            textBox_DevApiUrl = new TextBox();
            label24 = new Label();
            linkLabel1 = new LinkLabel();
            checkBox_EnableUserData = new CheckBox();
            checkBox_IPAreaCheck = new CheckBox();
            checkBox_OnceClick = new CheckBox();
            checkBox_DisableLoadImage = new CheckBox();
            label8 = new Label();
            numericUpDown_RestartComputerInterval = new NumericUpDown();
            label16 = new Label();
            label19 = new Label();
            label18 = new Label();
            numericUpDown_SubResetInterval = new NumericUpDown();
            label17 = new Label();
            numericUpDown_MainResetTimeout = new NumericUpDown();
            label26 = new Label();
            checkBox_IsProxyMode = new CheckBox();
            checkBox_IsHiddenMode = new CheckBox();
            checkBox_IsOsrMode = new CheckBox();
            checkBox_AutoStart = new CheckBox();
            checkBox_CheckIp = new CheckBox();
            groupBox5 = new GroupBox();
            button2 = new Button();
            label23 = new Label();
            textBox_SmsPhone = new TextBox();
            label22 = new Label();
            numericUpDown_SendSmsTimeout = new NumericUpDown();
            label21 = new Label();
            label20 = new Label();
            textBox_SmsName = new TextBox();
            checkBox_SendSms = new CheckBox();
            label15 = new Label();
            checkBox_RealIp = new CheckBox();
            textBox_UpdateApiUrl = new TextBox();
            label14 = new Label();
            label11 = new Label();
            numericUpDown_Multiple = new NumericUpDown();
            button1 = new Button();
            textBox_TaskApiUrl = new TextBox();
            label10 = new Label();
            label7 = new Label();
            label_request = new Label();
            label13 = new Label();
            numericUpDown_FetchTaskInterval = new NumericUpDown();
            btnStartStop = new Button();
            label12 = new Label();
            numericUpDown_MaximumConcurrency = new NumericUpDown();
            label9 = new Label();
            numericUpDown_UVInterval = new NumericUpDown();
            label6 = new Label();
            label4 = new Label();
            textBox_TaskName = new TextBox();
            label3 = new Label();
            label2 = new Label();
            textBox_ProxyIpUrl = new TextBox();
            label1 = new Label();
            groupBox6 = new GroupBox();
            radioButton_UsingRealDev = new RadioButton();
            radioButton_UsingRandomDev = new RadioButton();
            checkBox_UsingSystemDevs = new CheckBox();
            groupBox3 = new GroupBox();
            LogDetailTextBox = new TextBox();
            groupBox4 = new GroupBox();
            statusStrip1 = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel4 = new ToolStripStatusLabel();
            toolStripStatusLabel5 = new ToolStripStatusLabel();
            toolStripStatusLabel6 = new ToolStripStatusLabel();
            toolStripProgressBarDownload = new ToolStripProgressBar();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_IpTtl).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_RestartComputerInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SubResetInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainResetTimeout).BeginInit();
            groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SendSmsTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_FetchTaskInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumConcurrency).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UVInterval).BeginInit();
            groupBox6.SuspendLayout();
            groupBox4.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(taskInfoListView);
            groupBox1.Dock = DockStyle.Left;
            groupBox1.Location = new Point(0, 558);
            groupBox1.Margin = new Padding(5, 4, 5, 4);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(5, 4, 5, 4);
            groupBox1.Size = new Size(610, 426);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "任务列表";
            // 
            // taskInfoListView
            // 
            taskInfoListView.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4, columnHeader5, columnHeader6 });
            taskInfoListView.Dock = DockStyle.Fill;
            taskInfoListView.FullRowSelect = true;
            taskInfoListView.GridLines = true;
            taskInfoListView.Location = new Point(5, 27);
            taskInfoListView.Margin = new Padding(5, 4, 5, 4);
            taskInfoListView.Name = "taskInfoListView";
            taskInfoListView.Size = new Size(600, 395);
            taskInfoListView.TabIndex = 0;
            taskInfoListView.UseCompatibleStateImageBehavior = false;
            taskInfoListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "任务名称";
            columnHeader1.Width = 120;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "IP地址";
            columnHeader2.Width = 100;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "真实IP";
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "延迟";
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "归属地";
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "状态";
            columnHeader6.Width = 120;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label38);
            groupBox2.Controls.Add(label39);
            groupBox2.Controls.Add(numericUpDown_IpTtl);
            groupBox2.Controls.Add(linkLabel2);
            groupBox2.Controls.Add(textBox_DevApiUrl);
            groupBox2.Controls.Add(label24);
            groupBox2.Controls.Add(linkLabel1);
            groupBox2.Controls.Add(checkBox_EnableUserData);
            groupBox2.Controls.Add(checkBox_IPAreaCheck);
            groupBox2.Controls.Add(checkBox_OnceClick);
            groupBox2.Controls.Add(checkBox_DisableLoadImage);
            groupBox2.Controls.Add(label8);
            groupBox2.Controls.Add(numericUpDown_RestartComputerInterval);
            groupBox2.Controls.Add(label16);
            groupBox2.Controls.Add(label19);
            groupBox2.Controls.Add(label18);
            groupBox2.Controls.Add(numericUpDown_SubResetInterval);
            groupBox2.Controls.Add(label17);
            groupBox2.Controls.Add(numericUpDown_MainResetTimeout);
            groupBox2.Controls.Add(label26);
            groupBox2.Controls.Add(checkBox_IsProxyMode);
            groupBox2.Controls.Add(checkBox_IsHiddenMode);
            groupBox2.Controls.Add(checkBox_IsOsrMode);
            groupBox2.Controls.Add(checkBox_AutoStart);
            groupBox2.Controls.Add(checkBox_CheckIp);
            groupBox2.Controls.Add(groupBox5);
            groupBox2.Controls.Add(label15);
            groupBox2.Controls.Add(checkBox_RealIp);
            groupBox2.Controls.Add(textBox_UpdateApiUrl);
            groupBox2.Controls.Add(label14);
            groupBox2.Controls.Add(label11);
            groupBox2.Controls.Add(numericUpDown_Multiple);
            groupBox2.Controls.Add(button1);
            groupBox2.Controls.Add(textBox_TaskApiUrl);
            groupBox2.Controls.Add(label10);
            groupBox2.Controls.Add(label7);
            groupBox2.Controls.Add(label_request);
            groupBox2.Controls.Add(label13);
            groupBox2.Controls.Add(numericUpDown_FetchTaskInterval);
            groupBox2.Controls.Add(btnStartStop);
            groupBox2.Controls.Add(label12);
            groupBox2.Controls.Add(numericUpDown_MaximumConcurrency);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(numericUpDown_UVInterval);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(textBox_TaskName);
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(textBox_ProxyIpUrl);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(groupBox6);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Location = new Point(0, 0);
            groupBox2.Margin = new Padding(5, 4, 5, 4);
            groupBox2.Name = "groupBox2";
            groupBox2.Padding = new Padding(5, 4, 5, 4);
            groupBox2.Size = new Size(1519, 558);
            groupBox2.TabIndex = 2;
            groupBox2.TabStop = false;
            groupBox2.Text = "设置";
            // 
            // label38
            // 
            label38.AutoSize = true;
            label38.Location = new Point(984, 396);
            label38.Margin = new Padding(6, 0, 6, 0);
            label38.Name = "label38";
            label38.Size = new Size(28, 24);
            label38.TabIndex = 144;
            label38.Text = "秒";
            // 
            // label39
            // 
            label39.AutoSize = true;
            label39.Location = new Point(769, 396);
            label39.Margin = new Padding(6, 0, 6, 0);
            label39.Name = "label39";
            label39.Size = new Size(102, 24);
            label39.TabIndex = 142;
            label39.Text = "IP有效时长:";
            // 
            // numericUpDown_IpTtl
            // 
            numericUpDown_IpTtl.Location = new Point(874, 390);
            numericUpDown_IpTtl.Margin = new Padding(6, 5, 6, 5);
            numericUpDown_IpTtl.Maximum = new decimal(new int[] { 1800, 0, 0, 0 });
            numericUpDown_IpTtl.Name = "numericUpDown_IpTtl";
            numericUpDown_IpTtl.Size = new Size(104, 30);
            numericUpDown_IpTtl.TabIndex = 143;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new Point(1386, 392);
            linkLabel2.Margin = new Padding(5, 0, 5, 0);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new Size(82, 24);
            linkLabel2.TabIndex = 77;
            linkLabel2.TabStop = true;
            linkLabel2.Text = "应用目录";
            // 
            // textBox_DevApiUrl
            // 
            textBox_DevApiUrl.Location = new Point(122, 146);
            textBox_DevApiUrl.Margin = new Padding(5, 4, 5, 4);
            textBox_DevApiUrl.Name = "textBox_DevApiUrl";
            textBox_DevApiUrl.Size = new Size(591, 30);
            textBox_DevApiUrl.TabIndex = 76;
            textBox_DevApiUrl.Text = "http://117.21.200.18:9000/api/getdev.php";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(38, 151);
            label24.Margin = new Padding(5, 0, 5, 0);
            label24.Name = "label24";
            label24.Size = new Size(79, 24);
            label24.TabIndex = 74;
            label24.Text = "设备API:";
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new Point(1386, 344);
            linkLabel1.Margin = new Padding(4, 0, 4, 0);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new Size(82, 24);
            linkLabel1.TabIndex = 73;
            linkLabel1.TabStop = true;
            linkLabel1.Text = "开机启动";
            // 
            // checkBox_EnableUserData
            // 
            checkBox_EnableUserData.AutoSize = true;
            checkBox_EnableUserData.Location = new Point(1010, 342);
            checkBox_EnableUserData.Margin = new Padding(5, 6, 5, 6);
            checkBox_EnableUserData.Name = "checkBox_EnableUserData";
            checkBox_EnableUserData.Size = new Size(144, 28);
            checkBox_EnableUserData.TabIndex = 72;
            checkBox_EnableUserData.Text = "使用本地缓存";
            checkBox_EnableUserData.UseVisualStyleBackColor = true;
            // 
            // checkBox_IPAreaCheck
            // 
            checkBox_IPAreaCheck.AutoSize = true;
            checkBox_IPAreaCheck.Location = new Point(1010, 265);
            checkBox_IPAreaCheck.Margin = new Padding(5, 6, 5, 6);
            checkBox_IPAreaCheck.Name = "checkBox_IPAreaCheck";
            checkBox_IPAreaCheck.Size = new Size(124, 28);
            checkBox_IPAreaCheck.TabIndex = 71;
            checkBox_IPAreaCheck.Text = "IP地区校验";
            checkBox_IPAreaCheck.UseVisualStyleBackColor = true;
            // 
            // checkBox_OnceClick
            // 
            checkBox_OnceClick.AutoSize = true;
            checkBox_OnceClick.Location = new Point(1193, 342);
            checkBox_OnceClick.Margin = new Padding(5, 6, 5, 6);
            checkBox_OnceClick.Name = "checkBox_OnceClick";
            checkBox_OnceClick.Size = new Size(152, 28);
            checkBox_OnceClick.TabIndex = 70;
            checkBox_OnceClick.Text = "点击视频/广告";
            checkBox_OnceClick.UseVisualStyleBackColor = true;
            // 
            // checkBox_DisableLoadImage
            // 
            checkBox_DisableLoadImage.AutoSize = true;
            checkBox_DisableLoadImage.Location = new Point(1010, 304);
            checkBox_DisableLoadImage.Margin = new Padding(5, 6, 5, 6);
            checkBox_DisableLoadImage.Name = "checkBox_DisableLoadImage";
            checkBox_DisableLoadImage.Size = new Size(144, 28);
            checkBox_DisableLoadImage.TabIndex = 69;
            checkBox_DisableLoadImage.Text = "禁止加载图片";
            checkBox_DisableLoadImage.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(677, 400);
            label8.Margin = new Padding(5, 0, 5, 0);
            label8.Name = "label8";
            label8.Size = new Size(46, 24);
            label8.TabIndex = 68;
            label8.Text = "分钟";
            // 
            // numericUpDown_RestartComputerInterval
            // 
            numericUpDown_RestartComputerInterval.Location = new Point(566, 389);
            numericUpDown_RestartComputerInterval.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_RestartComputerInterval.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            numericUpDown_RestartComputerInterval.Name = "numericUpDown_RestartComputerInterval";
            numericUpDown_RestartComputerInterval.Size = new Size(104, 30);
            numericUpDown_RestartComputerInterval.TabIndex = 67;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(455, 400);
            label16.Margin = new Padding(5, 0, 5, 0);
            label16.Name = "label16";
            label16.Size = new Size(86, 24);
            label16.TabIndex = 66;
            label16.Text = "机器重启:";
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(677, 298);
            label19.Margin = new Padding(5, 0, 5, 0);
            label19.Name = "label19";
            label19.Size = new Size(99, 24);
            label19.TabIndex = 65;
            label19.Text = "分钟±30秒";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(677, 350);
            label18.Margin = new Padding(5, 0, 5, 0);
            label18.Name = "label18";
            label18.Size = new Size(99, 24);
            label18.TabIndex = 64;
            label18.Text = "分钟±30秒";
            // 
            // numericUpDown_SubResetInterval
            // 
            numericUpDown_SubResetInterval.Location = new Point(566, 288);
            numericUpDown_SubResetInterval.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_SubResetInterval.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            numericUpDown_SubResetInterval.Name = "numericUpDown_SubResetInterval";
            numericUpDown_SubResetInterval.Size = new Size(104, 30);
            numericUpDown_SubResetInterval.TabIndex = 63;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(433, 298);
            label17.Margin = new Padding(5, 0, 5, 0);
            label17.Name = "label17";
            label17.Size = new Size(104, 24);
            label17.TabIndex = 62;
            label17.Text = "子进程重置:";
            // 
            // numericUpDown_MainResetTimeout
            // 
            numericUpDown_MainResetTimeout.Location = new Point(566, 340);
            numericUpDown_MainResetTimeout.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_MainResetTimeout.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
            numericUpDown_MainResetTimeout.Name = "numericUpDown_MainResetTimeout";
            numericUpDown_MainResetTimeout.Size = new Size(104, 30);
            numericUpDown_MainResetTimeout.TabIndex = 61;
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Location = new Point(433, 350);
            label26.Margin = new Padding(5, 0, 5, 0);
            label26.Name = "label26";
            label26.Size = new Size(104, 24);
            label26.TabIndex = 60;
            label26.Text = "主进程重置:";
            // 
            // checkBox_IsProxyMode
            // 
            checkBox_IsProxyMode.AutoSize = true;
            checkBox_IsProxyMode.Location = new Point(810, 270);
            checkBox_IsProxyMode.Margin = new Padding(5, 6, 5, 6);
            checkBox_IsProxyMode.Name = "checkBox_IsProxyMode";
            checkBox_IsProxyMode.Size = new Size(108, 28);
            checkBox_IsProxyMode.TabIndex = 59;
            checkBox_IsProxyMode.Text = "使用代理";
            checkBox_IsProxyMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_IsHiddenMode
            // 
            checkBox_IsHiddenMode.AutoSize = true;
            checkBox_IsHiddenMode.Location = new Point(810, 232);
            checkBox_IsHiddenMode.Margin = new Padding(5, 6, 5, 6);
            checkBox_IsHiddenMode.Name = "checkBox_IsHiddenMode";
            checkBox_IsHiddenMode.Size = new Size(108, 28);
            checkBox_IsHiddenMode.TabIndex = 58;
            checkBox_IsHiddenMode.Text = "隐藏模式";
            checkBox_IsHiddenMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_IsOsrMode
            // 
            checkBox_IsOsrMode.AutoSize = true;
            checkBox_IsOsrMode.Location = new Point(947, 232);
            checkBox_IsOsrMode.Margin = new Padding(5, 6, 5, 6);
            checkBox_IsOsrMode.Name = "checkBox_IsOsrMode";
            checkBox_IsOsrMode.Size = new Size(109, 28);
            checkBox_IsOsrMode.TabIndex = 78;
            checkBox_IsOsrMode.Text = "OSR模式";
            checkBox_IsOsrMode.UseVisualStyleBackColor = true;
            // 
            // checkBox_AutoStart
            // 
            checkBox_AutoStart.AutoSize = true;
            checkBox_AutoStart.Location = new Point(810, 347);
            checkBox_AutoStart.Margin = new Padding(5, 6, 5, 6);
            checkBox_AutoStart.Name = "checkBox_AutoStart";
            checkBox_AutoStart.Size = new Size(144, 28);
            checkBox_AutoStart.TabIndex = 57;
            checkBox_AutoStart.Text = "开机自动运行";
            checkBox_AutoStart.UseVisualStyleBackColor = true;
            // 
            // checkBox_CheckIp
            // 
            checkBox_CheckIp.AutoSize = true;
            checkBox_CheckIp.Location = new Point(1010, 227);
            checkBox_CheckIp.Margin = new Padding(5, 6, 5, 6);
            checkBox_CheckIp.Name = "checkBox_CheckIp";
            checkBox_CheckIp.Size = new Size(142, 28);
            checkBox_CheckIp.TabIndex = 51;
            checkBox_CheckIp.Text = "检测IP有效性";
            checkBox_CheckIp.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(button2);
            groupBox5.Controls.Add(label23);
            groupBox5.Controls.Add(textBox_SmsPhone);
            groupBox5.Controls.Add(label22);
            groupBox5.Controls.Add(numericUpDown_SendSmsTimeout);
            groupBox5.Controls.Add(label21);
            groupBox5.Controls.Add(label20);
            groupBox5.Controls.Add(textBox_SmsName);
            groupBox5.Controls.Add(checkBox_SendSms);
            groupBox5.Location = new Point(1116, 35);
            groupBox5.Margin = new Padding(5, 6, 5, 6);
            groupBox5.Name = "groupBox5";
            groupBox5.Padding = new Padding(5, 6, 5, 6);
            groupBox5.Size = new Size(366, 191);
            groupBox5.TabIndex = 45;
            groupBox5.TabStop = false;
            // 
            // button2
            // 
            button2.Location = new Point(280, 34);
            button2.Margin = new Padding(5, 6, 5, 6);
            button2.Name = "button2";
            button2.Size = new Size(70, 44);
            button2.TabIndex = 51;
            button2.Text = "测试";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(17, 95);
            label23.Margin = new Padding(5, 0, 5, 0);
            label23.Name = "label23";
            label23.Size = new Size(50, 24);
            label23.TabIndex = 50;
            label23.Text = "电话:";
            // 
            // textBox_SmsPhone
            // 
            textBox_SmsPhone.Location = new Point(85, 83);
            textBox_SmsPhone.Margin = new Padding(5, 6, 5, 6);
            textBox_SmsPhone.Name = "textBox_SmsPhone";
            textBox_SmsPhone.Size = new Size(262, 30);
            textBox_SmsPhone.TabIndex = 49;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(172, 143);
            label22.Margin = new Padding(5, 0, 5, 0);
            label22.Name = "label22";
            label22.Size = new Size(122, 24);
            label22.TabIndex = 48;
            label22.Text = "分钟,发送短信";
            // 
            // numericUpDown_SendSmsTimeout
            // 
            numericUpDown_SendSmsTimeout.Location = new Point(85, 131);
            numericUpDown_SendSmsTimeout.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_SendSmsTimeout.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numericUpDown_SendSmsTimeout.Name = "numericUpDown_SendSmsTimeout";
            numericUpDown_SendSmsTimeout.Size = new Size(82, 30);
            numericUpDown_SendSmsTimeout.TabIndex = 47;
            numericUpDown_SendSmsTimeout.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(17, 143);
            label21.Margin = new Padding(5, 0, 5, 0);
            label21.Name = "label21";
            label21.Size = new Size(50, 24);
            label21.TabIndex = 46;
            label21.Text = "超时:";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(17, 47);
            label20.Margin = new Padding(5, 0, 5, 0);
            label20.Name = "label20";
            label20.Size = new Size(50, 24);
            label20.TabIndex = 44;
            label20.Text = "名称:";
            // 
            // textBox_SmsName
            // 
            textBox_SmsName.Location = new Point(85, 35);
            textBox_SmsName.Margin = new Padding(5, 6, 5, 6);
            textBox_SmsName.Name = "textBox_SmsName";
            textBox_SmsName.Size = new Size(180, 30);
            textBox_SmsName.TabIndex = 43;
            // 
            // checkBox_SendSms
            // 
            checkBox_SendSms.AutoSize = true;
            checkBox_SendSms.Location = new Point(11, -1);
            checkBox_SendSms.Margin = new Padding(5, 6, 5, 6);
            checkBox_SendSms.Name = "checkBox_SendSms";
            checkBox_SendSms.Size = new Size(108, 28);
            checkBox_SendSms.TabIndex = 45;
            checkBox_SendSms.Text = "短信服务";
            checkBox_SendSms.UseVisualStyleBackColor = true;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(914, 143);
            label15.Margin = new Padding(5, 0, 5, 0);
            label15.Name = "label15";
            label15.Size = new Size(97, 24);
            label15.TabIndex = 36;
            label15.Text = "进程数量:0";
            // 
            // checkBox_RealIp
            // 
            checkBox_RealIp.AutoSize = true;
            checkBox_RealIp.Location = new Point(810, 308);
            checkBox_RealIp.Margin = new Padding(5, 6, 5, 6);
            checkBox_RealIp.Name = "checkBox_RealIp";
            checkBox_RealIp.Size = new Size(88, 28);
            checkBox_RealIp.TabIndex = 35;
            checkBox_RealIp.Text = "真实IP";
            checkBox_RealIp.UseVisualStyleBackColor = true;
            // 
            // textBox_UpdateApiUrl
            // 
            textBox_UpdateApiUrl.Location = new Point(122, 35);
            textBox_UpdateApiUrl.Margin = new Padding(5, 4, 5, 4);
            textBox_UpdateApiUrl.Name = "textBox_UpdateApiUrl";
            textBox_UpdateApiUrl.Size = new Size(591, 30);
            textBox_UpdateApiUrl.TabIndex = 34;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(38, 40);
            label14.Margin = new Padding(5, 0, 5, 0);
            label14.Name = "label14";
            label14.Size = new Size(79, 24);
            label14.TabIndex = 33;
            label14.Text = "更新API:";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(455, 250);
            label11.Margin = new Padding(5, 0, 5, 0);
            label11.Name = "label11";
            label11.Size = new Size(86, 24);
            label11.TabIndex = 31;
            label11.Text = "任务倍速:";
            // 
            // numericUpDown_Multiple
            // 
            numericUpDown_Multiple.Location = new Point(566, 239);
            numericUpDown_Multiple.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_Multiple.Name = "numericUpDown_Multiple";
            numericUpDown_Multiple.Size = new Size(104, 30);
            numericUpDown_Multiple.TabIndex = 32;
            numericUpDown_Multiple.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // button1
            // 
            button1.Font = new Font("宋体", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            button1.ForeColor = Color.Red;
            button1.Location = new Point(739, 136);
            button1.Margin = new Padding(5, 6, 5, 6);
            button1.Name = "button1";
            button1.Size = new Size(163, 77);
            button1.TabIndex = 22;
            button1.Text = "清除";
            // 
            // textBox_TaskApiUrl
            // 
            textBox_TaskApiUrl.Location = new Point(122, 109);
            textBox_TaskApiUrl.Margin = new Padding(5, 4, 5, 4);
            textBox_TaskApiUrl.Name = "textBox_TaskApiUrl";
            textBox_TaskApiUrl.Size = new Size(591, 30);
            textBox_TaskApiUrl.TabIndex = 21;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(38, 114);
            label10.Margin = new Padding(5, 0, 5, 0);
            label10.Name = "label10";
            label10.Size = new Size(79, 24);
            label10.TabIndex = 20;
            label10.Text = "任务API:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(914, 106);
            label7.Margin = new Padding(5, 0, 5, 0);
            label7.Name = "label7";
            label7.Size = new Size(97, 24);
            label7.TabIndex = 19;
            label7.Text = "运行时间:0";
            // 
            // label_request
            // 
            label_request.AutoSize = true;
            label_request.Location = new Point(914, 71);
            label_request.Margin = new Padding(5, 0, 5, 0);
            label_request.Name = "label_request";
            label_request.Size = new Size(97, 24);
            label_request.TabIndex = 16;
            label_request.Text = "提交数量:0";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(320, 352);
            label13.Margin = new Padding(5, 0, 5, 0);
            label13.Name = "label13";
            label13.Size = new Size(46, 24);
            label13.TabIndex = 15;
            label13.Text = "毫秒";
            // 
            // numericUpDown_FetchTaskInterval
            // 
            numericUpDown_FetchTaskInterval.Location = new Point(209, 288);
            numericUpDown_FetchTaskInterval.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_FetchTaskInterval.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            numericUpDown_FetchTaskInterval.Name = "numericUpDown_FetchTaskInterval";
            numericUpDown_FetchTaskInterval.Size = new Size(104, 30);
            numericUpDown_FetchTaskInterval.TabIndex = 14;
            numericUpDown_FetchTaskInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // btnStartStop
            // 
            btnStartStop.Location = new Point(739, 35);
            btnStartStop.Margin = new Padding(5, 6, 5, 6);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(163, 92);
            btnStartStop.TabIndex = 13;
            btnStartStop.Text = "开始";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(97, 400);
            label12.Margin = new Padding(5, 0, 5, 0);
            label12.Name = "label12";
            label12.Size = new Size(86, 24);
            label12.TabIndex = 9;
            label12.Text = "并发数量:";
            // 
            // numericUpDown_MaximumConcurrency
            // 
            numericUpDown_MaximumConcurrency.Location = new Point(209, 389);
            numericUpDown_MaximumConcurrency.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_MaximumConcurrency.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            numericUpDown_MaximumConcurrency.Name = "numericUpDown_MaximumConcurrency";
            numericUpDown_MaximumConcurrency.Size = new Size(104, 30);
            numericUpDown_MaximumConcurrency.TabIndex = 10;
            numericUpDown_MaximumConcurrency.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(97, 350);
            label9.Margin = new Padding(5, 0, 5, 0);
            label9.Name = "label9";
            label9.Size = new Size(93, 24);
            label9.TabIndex = 0;
            label9.Text = "单UV间隔:";
            // 
            // numericUpDown_UVInterval
            // 
            numericUpDown_UVInterval.Location = new Point(209, 340);
            numericUpDown_UVInterval.Margin = new Padding(5, 4, 5, 4);
            numericUpDown_UVInterval.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            numericUpDown_UVInterval.Name = "numericUpDown_UVInterval";
            numericUpDown_UVInterval.Size = new Size(104, 30);
            numericUpDown_UVInterval.TabIndex = 3;
            numericUpDown_UVInterval.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(914, 35);
            label6.Margin = new Padding(5, 0, 5, 0);
            label6.Name = "label6";
            label6.Size = new Size(63, 24);
            label6.TabIndex = 4;
            label6.Text = "label6";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(54, 250);
            label4.Margin = new Padding(5, 0, 5, 0);
            label4.Name = "label4";
            label4.Size = new Size(122, 24);
            label4.TabIndex = 0;
            label4.Text = "独立任务标识:";
            // 
            // textBox_TaskName
            // 
            textBox_TaskName.Location = new Point(209, 239);
            textBox_TaskName.Margin = new Padding(5, 4, 5, 4);
            textBox_TaskName.Name = "textBox_TaskName";
            textBox_TaskName.Size = new Size(196, 30);
            textBox_TaskName.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(320, 302);
            label3.Margin = new Padding(5, 0, 5, 0);
            label3.Name = "label3";
            label3.Size = new Size(46, 24);
            label3.TabIndex = 0;
            label3.Text = "毫秒";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(54, 298);
            label2.Margin = new Padding(5, 0, 5, 0);
            label2.Name = "label2";
            label2.Size = new Size(122, 24);
            label2.TabIndex = 0;
            label2.Text = "获取任务间隔:";
            // 
            // textBox_ProxyIpUrl
            // 
            textBox_ProxyIpUrl.Location = new Point(122, 72);
            textBox_ProxyIpUrl.Margin = new Padding(5, 4, 5, 4);
            textBox_ProxyIpUrl.Name = "textBox_ProxyIpUrl";
            textBox_ProxyIpUrl.Size = new Size(591, 30);
            textBox_ProxyIpUrl.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(38, 77);
            label1.Margin = new Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new Size(79, 24);
            label1.TabIndex = 0;
            label1.Text = "代理API:";
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(radioButton_UsingRealDev);
            groupBox6.Controls.Add(radioButton_UsingRandomDev);
            groupBox6.Controls.Add(checkBox_UsingSystemDevs);
            groupBox6.Location = new Point(1193, 234);
            groupBox6.Margin = new Padding(5, 6, 5, 6);
            groupBox6.Name = "groupBox6";
            groupBox6.Padding = new Padding(5, 6, 5, 6);
            groupBox6.Size = new Size(288, 96);
            groupBox6.TabIndex = 52;
            groupBox6.TabStop = false;
            // 
            // radioButton_UsingRealDev
            // 
            radioButton_UsingRealDev.AutoSize = true;
            radioButton_UsingRealDev.Location = new Point(150, 47);
            radioButton_UsingRealDev.Margin = new Padding(5, 6, 5, 6);
            radioButton_UsingRealDev.Name = "radioButton_UsingRealDev";
            radioButton_UsingRealDev.Size = new Size(89, 28);
            radioButton_UsingRealDev.TabIndex = 55;
            radioButton_UsingRealDev.TabStop = true;
            radioButton_UsingRealDev.Text = "真机库";
            radioButton_UsingRealDev.UseVisualStyleBackColor = true;
            // 
            // radioButton_UsingRandomDev
            // 
            radioButton_UsingRandomDev.AutoSize = true;
            radioButton_UsingRandomDev.Checked = true;
            radioButton_UsingRandomDev.Location = new Point(32, 47);
            radioButton_UsingRandomDev.Margin = new Padding(5, 6, 5, 6);
            radioButton_UsingRandomDev.Name = "radioButton_UsingRandomDev";
            radioButton_UsingRandomDev.Size = new Size(89, 28);
            radioButton_UsingRandomDev.TabIndex = 54;
            radioButton_UsingRandomDev.TabStop = true;
            radioButton_UsingRandomDev.Text = "随机库";
            radioButton_UsingRandomDev.UseVisualStyleBackColor = true;
            // 
            // checkBox_UsingSystemDevs
            // 
            checkBox_UsingSystemDevs.AutoSize = true;
            checkBox_UsingSystemDevs.Location = new Point(11, -1);
            checkBox_UsingSystemDevs.Margin = new Padding(5, 6, 5, 6);
            checkBox_UsingSystemDevs.Name = "checkBox_UsingSystemDevs";
            checkBox_UsingSystemDevs.Size = new Size(180, 28);
            checkBox_UsingSystemDevs.TabIndex = 53;
            checkBox_UsingSystemDevs.Text = "使用系统设备信息";
            checkBox_UsingSystemDevs.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Location = new Point(610, 558);
            groupBox3.Margin = new Padding(5, 4, 5, 4);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new Padding(5, 4, 5, 4);
            groupBox3.Size = new Size(909, 426);
            groupBox3.TabIndex = 4;
            groupBox3.TabStop = false;
            groupBox3.Text = "日志";
            // 
            // LogDetailTextBox
            // 
            LogDetailTextBox.Dock = DockStyle.Fill;
            LogDetailTextBox.Location = new Point(5, 27);
            LogDetailTextBox.Margin = new Padding(5, 4, 5, 4);
            LogDetailTextBox.Multiline = true;
            LogDetailTextBox.Name = "LogDetailTextBox";
            LogDetailTextBox.ScrollBars = ScrollBars.Both;
            LogDetailTextBox.Size = new Size(1509, 203);
            LogDetailTextBox.TabIndex = 3;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(statusStrip1);
            groupBox4.Controls.Add(LogDetailTextBox);
            groupBox4.Dock = DockStyle.Bottom;
            groupBox4.Location = new Point(0, 984);
            groupBox4.Margin = new Padding(5, 4, 5, 4);
            groupBox4.Name = "groupBox4";
            groupBox4.Padding = new Padding(5, 4, 5, 4);
            groupBox4.Size = new Size(1519, 234);
            groupBox4.TabIndex = 4;
            groupBox4.TabStop = false;
            groupBox4.Text = "详细日志";
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblStatus, toolStripStatusLabel1, toolStripStatusLabel4, toolStripStatusLabel5, toolStripStatusLabel6, toolStripProgressBarDownload });
            statusStrip1.Location = new Point(5, 199);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(4, 0, 22, 0);
            statusStrip1.Size = new Size(1509, 31);
            statusStrip1.TabIndex = 8;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(63, 24);
            lblStatus.Text = "Status";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(61, 24);
            toolStripStatusLabel1.Text = "CPU:0";
            // 
            // toolStripStatusLabel4
            // 
            toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            toolStripStatusLabel4.Size = new Size(97, 24);
            toolStripStatusLabel4.Text = "执行总量:0";
            // 
            // toolStripStatusLabel5
            // 
            toolStripStatusLabel5.Name = "toolStripStatusLabel5";
            toolStripStatusLabel5.Size = new Size(97, 24);
            toolStripStatusLabel5.Text = "曝光总量:0";
            // 
            // toolStripStatusLabel6
            // 
            toolStripStatusLabel6.Name = "toolStripStatusLabel6";
            toolStripStatusLabel6.Size = new Size(97, 24);
            toolStripStatusLabel6.Text = "点击总量:0";
            // 
            // toolStripProgressBarDownload
            // 
            toolStripProgressBarDownload.Name = "toolStripProgressBarDownload";
            toolStripProgressBarDownload.Size = new Size(120, 28);
            toolStripProgressBarDownload.Visible = false;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1519, 1218);
            Controls.Add(groupBox3);
            Controls.Add(groupBox1);
            Controls.Add(groupBox2);
            Controls.Add(groupBox4);
            Margin = new Padding(5, 4, 5, 4);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "曝光点击-";
            Load += MainForm_Load;
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_IpTtl).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_RestartComputerInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SubResetInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MainResetTimeout).EndInit();
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_SendSmsTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_Multiple).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_FetchTaskInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_MaximumConcurrency).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDown_UVInterval).EndInit();
            groupBox6.ResumeLayout(false);
            groupBox6.PerformLayout();
            groupBox4.ResumeLayout(false);
            groupBox4.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView taskInfoListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBox_ProxyIpUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_TaskName;
        private System.Windows.Forms.TextBox LogDetailTextBox;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numericUpDown_UVInterval;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.NumericUpDown numericUpDown_MaximumConcurrency;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.NumericUpDown numericUpDown_FetchTaskInterval;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label_request;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox_TaskApiUrl;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.NumericUpDown numericUpDown_Multiple;
        private System.Windows.Forms.TextBox textBox_UpdateApiUrl;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox checkBox_RealIp;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label23;
        private System.Windows.Forms.TextBox textBox_SmsPhone;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.NumericUpDown numericUpDown_SendSmsTimeout;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox textBox_SmsName;
        private System.Windows.Forms.CheckBox checkBox_SendSms;
        private System.Windows.Forms.CheckBox checkBox_CheckIp;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.RadioButton radioButton_UsingRealDev;
        private System.Windows.Forms.RadioButton radioButton_UsingRandomDev;
        private System.Windows.Forms.CheckBox checkBox_UsingSystemDevs;
        private System.Windows.Forms.CheckBox checkBox_AutoStart;
        private System.Windows.Forms.CheckBox checkBox_IsProxyMode;
        private System.Windows.Forms.CheckBox checkBox_IsHiddenMode;
        private System.Windows.Forms.CheckBox checkBox_IsOsrMode;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numericUpDown_RestartComputerInterval;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.NumericUpDown numericUpDown_SubResetInterval;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.NumericUpDown numericUpDown_MainResetTimeout;
        private System.Windows.Forms.Label label26;
        private System.Windows.Forms.CheckBox checkBox_DisableLoadImage;
        private System.Windows.Forms.CheckBox checkBox_OnceClick;
        private System.Windows.Forms.CheckBox checkBox_IPAreaCheck;
        private System.Windows.Forms.CheckBox checkBox_EnableUserData;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label label24;
        private System.Windows.Forms.TextBox textBox_DevApiUrl;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel4;
        private ToolStripStatusLabel toolStripStatusLabel5;
        private ToolStripStatusLabel toolStripStatusLabel6;
        private ToolStripProgressBar toolStripProgressBarDownload;
        private Label label38;
        private Label label39;
        private NumericUpDown numericUpDown_IpTtl;
    }
}

