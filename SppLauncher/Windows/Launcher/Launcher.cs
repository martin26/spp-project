﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Ionic.Zip;
using MySql.Data.MySqlClient;
using MySQLClass;
using SppLauncher.Class;
using SppLauncher.Properties;
using SppLauncher.Windows;
using SppLauncher.Windows.BugReport;
using WowAccountCreator;

namespace SppLauncher
{
    public partial class Launcher : Form
    {
        #region [ Variable ]

        private readonly XmlReadWrite xmlReadWrite;
        public static string wowExePath,
            ipAdress,online,resetBots,
            randomizeBots,realmListPath,
            realmDialogPath,OLDrealmList,
            UpdLink,importFile, exportfile, 
            Autostart, currProgVer = "1.1.3",
            lang, UpdateUnpack, Status;

        public DateTime Dt;
        private readonly PerformanceCounter cpuCounter, ramCounter;
        public static Process _cmd, _cmd1, _cmd3;
        private DateTime _start1 = DateTime.Now;
        private bool _startStop, _allowtext, _restart, _updateYes, _serverConsoleActive, SysProtect;
        public static bool Updater, OnlyMysqlStart,MysqlON,Sqlimport,sqlexport,dbupdate;
        private string _realm, _mangosdMem, _realmdMem, _sqlMem, _world, _sql, _SqlStartTime, _RealmStartTime, _WorldStartTime;
        private readonly string getTemp = Path.GetTempPath();
        public static double CurrEmuVer, RemoteEmuVer;
        private const string lwPath  = "SingleCore\\";
        private const string sqlpath = "Database\\bin\\";
        #endregion

        #region [ Initialize ]

        public Launcher()
        {
            xmlReadWrite = new XmlReadWrite();
            if(xmlReadWrite.ReadXML()){checklang(false);}
            string exePath = AppDomain.CurrentDomain.FriendlyName;
            File.SetAttributes(exePath, FileAttributes.Normal);
            InitializeComponent();
            checklang(true);
            cpuCounter = new PerformanceCounter();
            GetLocalSrvVer();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName  = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            ramCounter              = new PerformanceCounter("Memory", "Available MBytes");
            tmrUsage.Start();
            StatusBarUpdater.Start();
            MenuItemsDisableAfterLoad();
            WasThisUpdate();
            rtWorldDev.Visible = false;
            WindowSize(true);
            font();
            SearchProcess();
            StatusIcon();


            if (Autostart == "1")
            {
                startstopToolStripMenuItem.Image                = Resources.Button_stop_icon;
                startToolStripMenuItem.Image                    = Resources.Button_stop_icon;
                startstopToolStripMenuItem.Text                 = Resources.Launcher_startNStop_Stop;
                startToolStripMenuItem.Text                     = Resources.Launcher_startNStop_Stop;
                startstopToolStripMenuItem.Enabled              = false;
                startToolStripMenuItem.Enabled                  = false;
                _startStop                                      = true;
                autostartToolStripMenuItem.Checked              = true;
                autorunToolStripMenuItem.Checked                = true;
                exportImportCharactersToolStripMenuItem.Enabled = false;
                StartAll();
            }
            else
            {
                bwUpdate.RunWorkerAsync();
                exportImportCharactersToolStripMenuItem.Enabled    = false;
                startstopToolStripMenuItem.Enabled                 = true;
                startToolStripMenuItem.Enabled                     = true; 
                restartToolStripMenuItem1.Enabled                  = false;
                restartToolStripMenuItem2.Enabled                  = false;
                sendCommandForServerToolStripMenuItem.Enabled      = false;
            }

            SppTray.Visible = true;
            startWowToolStripMenuItem.ToolTipText =
                Resources
                    .Launcher_Launcher_Auto_Changed_RealmList_wtf_and_after_exit_SPP_change_back_original_realmlist_;

        }

        #endregion

        #region [ Servers ]

        private void SetText(string text)
        {
            _realm += text;
        }

        public float getCurrentCpuUsage()
        {
            return cpuCounter.NextValue();

        }

        public int getAvailableRAM()
        {
            return Convert.ToInt32(ramCounter.NextValue());
        }

        private void SetText3(string text1)
        {
            if (richTextBox1.InvokeRequired)
            {
                SetTextCallback d = SetText3;
                Invoke(d, new object[] {text1});
            }
            else
            {
                _sql              += text1;
                richTextBox1.Text += text1 + Environment.NewLine;
                File.WriteAllText("logs\\mysql.txt", richTextBox1.Text);
            }
        }

        private void SetText1(string text)
        {
            if (rtWorldDev.InvokeRequired)
            {
                SetTextCallback d = SetText1;
                Invoke(d, new object[] {text});
            }
            else
            {
                _world += text;

                if (_allowtext)
                {
                    if (!text.Contains("SPELL") &&
                        !text.Contains("wrong DBC files") &&
                        !text.Contains("incorrect race/class") &&
                        !text.Contains("SD2 ERROR") &&
                        !text.Contains("ERROR:Player") &&
                        !text.Contains("DoScriptText") &&
                        !text.Contains("DB-SCRIPTS") &&
                        !text.Contains("removeSpell") &&
                        !text.Contains("ACTION_T_CAST") &&
                        !text.Contains("SD2") &&
                        !text.Contains("TriggerSpell") &&
                        !text.Contains("ACTION_T_SUMMON") &&
                        !text.Contains("INSERT INTO") &&
                        !text.Contains("SQL ERROR") &&
                        !text.Contains("LoadInventory") &&
                        !text.Contains("MoveSpline") &&
                        !text.Contains("unknown spell id"))
                    {
                        Dt = DateTime.Now;

                        if (rtWorldDev.Text      != "") rtWorldDev.Text += Environment.NewLine;
                        rtWorldDev.Text          += Dt.ToString("[" + "HH:mm" + "]: ") + text;
                        rtWorldDev.SelectionStart = rtWorldDev.Text.Length;
                        rtWorldDev.ScrollToCaret();
                    }
                }
            }
        }

        public void WorldStart()
        {
            _start1             = DateTime.Now;
            pbarWorld.Visible   = true;
            pbTempW.Visible     = true;
            pbNotAvailW.Visible = false;
            statusChage(Resources.Launcher_WorldStart_Loading_World, false);
            WindowSize(false);
            tmrWorld.Start();
            var cmdStartInfo                    = new ProcessStartInfo(lwPath + "mangosd.exe");
            cmdStartInfo.CreateNoWindow         = true;
            cmdStartInfo.RedirectStandardInput  = true;
            cmdStartInfo.RedirectStandardOutput = true;
            cmdStartInfo.RedirectStandardError  = true;
            cmdStartInfo.UseShellExecute        = false;
            cmdStartInfo.WindowStyle            = ProcessWindowStyle.Hidden;

            _cmd1 = new Process();
            _cmd1.StartInfo = cmdStartInfo;


            if (_cmd1.Start())
            {
                _cmd1.OutputDataReceived += _cmd_OutputDataReceived1;
                _cmd1.ErrorDataReceived  += _cmd_ErrorDataReceived1;
                _cmd1.Exited             += _cmd_Exited1;
                _cmd1.BeginOutputReadLine();
                _cmd1.BeginErrorReadLine();
            }
            else
            {
                _cmd1 = null;
            }
        }


        internal void RealmdStart()
        {
            _start1             = DateTime.Now;
            statusChage(Resources.Launcher_RealmdStart_Starting_Realm, false);
            tssStatus.Image     = Resources.search_animation;
            pbTempR.Visible     = true;
            pbNotAvailR.Visible = false;
            tmrRealm.Start();
            var cmdStartInfo                    = new ProcessStartInfo(lwPath + "login.exe");
            cmdStartInfo.CreateNoWindow         = true;
            cmdStartInfo.RedirectStandardInput  = true;
            cmdStartInfo.RedirectStandardOutput = true;
            cmdStartInfo.RedirectStandardError  = true;
            cmdStartInfo.UseShellExecute        = false;
            cmdStartInfo.WindowStyle            = ProcessWindowStyle.Hidden;
            _cmd                                = new Process();
            _cmd.StartInfo                      = cmdStartInfo;

            if (_cmd.Start())
            {
                _cmd.OutputDataReceived += _cmd_OutputDataReceived;
                _cmd.ErrorDataReceived  += _cmd_ErrorDataReceived;
                _cmd.Exited             += _cmd_Exited;
                _cmd.BeginOutputReadLine();
                _cmd.BeginErrorReadLine();
            }
            else
            {
                _cmd = null;
            }
        }

        private void _cmd_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            UpdateConsole(e.Data);
        }

        private void _cmd_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            UpdateConsole(e.Data, Brushes.Red);
        }

        private void _cmd_Exited(object sender, EventArgs e)
        {
            _cmd.OutputDataReceived -= _cmd_OutputDataReceived;
            _cmd.Exited -= _cmd_Exited;
        }

        private void UpdateConsole(string text)
        {
            UpdateConsole(text, null);
        }

        private void UpdateConsole(string text, Brush color)
        {
            WriteLine(text, color);
        }

        private void WriteLine(string text, Brush color)
        {
            if (text != null)
            {
                SetText(text);
            }
        }

        private void _cmd_OutputDataReceived1(object sender, DataReceivedEventArgs e)
        {
            UpdateConsole1(e.Data);
        }

        private void _cmd_ErrorDataReceived1(object sender, DataReceivedEventArgs e)
        {
            UpdateConsole1(e.Data, Brushes.Red);
        }

        private void _cmd_Exited1(object sender, EventArgs e)
        {
            _cmd1.OutputDataReceived -= _cmd_OutputDataReceived1;
            _cmd1.Exited -= _cmd_Exited1;
        }

        private void UpdateConsole1(string text)
        {
            UpdateConsole1(text, null);
        }

        private void UpdateConsole1(string text, Brush color)
        {
            WriteLine1(text, color);
        }

        private void WriteLine1(string text, Brush color)
        {
            if (text != null)
            {
                SetText1(text);
            }
        }

        private void Launcher_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void NotifyBallon(int timeout, string title, string msg, bool crashed)
        {
            if (crashed)
                SppTray.ShowBalloonTip(timeout, title, msg, ToolTipIcon.Warning);
            else
                SppTray.ShowBalloonTip(timeout, title, msg, ToolTipIcon.Info);
        }

        public void StartAll()
        {
            _start1 = DateTime.Now;
            if (!_restart)
            {
                statusChage(Resources.Launcher_StartAll_Starting_Mysqlm, false);
                animateStatus(true);
                pbTempM.Visible     = true;
                pbNotAvailM.Visible = false;
                SqlStartCheck.Start();
                var cmdStartInfo = new ProcessStartInfo("cmd.exe",
                    "/C " + sqlpath + "mysqld.exe" +
                    " --defaults-file=" + sqlpath + "my.ini" +
                    " --standalone --console");
                cmdStartInfo.CreateNoWindow         = true;
                cmdStartInfo.RedirectStandardInput  = true;
                cmdStartInfo.RedirectStandardOutput = true;
                cmdStartInfo.RedirectStandardError  = true;
                cmdStartInfo.UseShellExecute        = false;
                cmdStartInfo.WindowStyle            = ProcessWindowStyle.Hidden;

                _cmd3 = new Process();
                try
                {
                    _cmd3.StartInfo = cmdStartInfo;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Some exception: mysql\n" + ex.Message);
                }

                if (_cmd3.Start())
                {
                    _cmd3.OutputDataReceived += _cmd_OutputDataReceived3;
                    _cmd3.ErrorDataReceived  += _cmd_ErrorDataReceived3;
                    _cmd3.Exited             += _cmd_Exited3;
                    _cmd3.BeginOutputReadLine();
                    _cmd3.BeginErrorReadLine();
                }
                else
                {
                    _cmd3 = null;
                }
            }
            else
            {
                pbAvailableM.Visible = true;
                RealmdStart();
            }
        }

        private void _cmd_OutputDataReceived3(object sender, DataReceivedEventArgs e)
        {
            UpdateConsole3(e.Data);
        }

        private void _cmd_ErrorDataReceived3(object sender, DataReceivedEventArgs e)
        {
            UpdateConsole3(e.Data, Brushes.Red);
        }

        private void _cmd_Exited3(object sender, EventArgs e)
        {
            _cmd3.OutputDataReceived -= _cmd_OutputDataReceived3;
            _cmd3.Exited -= _cmd_Exited3;
        }

        private void UpdateConsole3(string text)
        {
            UpdateConsole3(text, null);
        }

        private void UpdateConsole3(string text, Brush color)
        {
            WriteLine3(text, color);
        }

        private void WriteLine3(string text, Brush color)
        {
            if (text != null)
            {
                SetText3(text);
            }
        }

        private delegate void SetTextCallback(string text);

        private void Check_Tick(object sender, EventArgs e)
        {
            if (!CheckRunwow())
            {
                Check.Stop();
                RealmRestore();
            }
        }

        private void rstchck_DoWork(object sender, DoWorkEventArgs e)
        {
            checklang(false);
            statusChage(Resources.Launcher_rstchck_DoWork_Reset_bots, false);
            tssStatus.Image = Resources.search_animation;
            Thread.Sleep(10000);
        }

        private void rstchck_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MenuItemsDisableAfterLoad();
            RealmWorldRestart();
        }

        #endregion

        #region [ ProcessTasks ]

        public void CloseProcess(bool restart)
        {
            try
            {
                checklang(false);
                if (_serverConsoleActive) sendCommandForServerToolStripMenuItem_Click(new object(), new EventArgs());

                foreach (Process proc in Process.GetProcessesByName("mangosd"))
                {
                    statusChage("Saving",false);
                    animateStatus(true);
                    _world = "";
                    Thread.Sleep(5000);
                    statusChage(Resources.Launcher_CloseProcess_World_Shutdown, false);
                    _cmd1.StandardInput.WriteLine("server shutdown 0");
                    Thread.Sleep(1000);
                    proc.Kill();
                }

                foreach (Process proc in Process.GetProcessesByName("login"))
                {
                    statusChage(Resources.Launcher_CloseProcess_Login_Shutdown, false);
                    proc.Kill();
                }

                foreach (Process proc in Process.GetProcessesByName("mysqld"))
                {
                    if (!restart)
                        ShutdownSql();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.Launcher_CloseProcess_ +
                                ex.Message, Resources.Launcher_SearchProcess_Warning, MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void SearchProcess()
        {
            try
            {

                foreach (Process proc in Process.GetProcessesByName("mangosd"))
                {
                
                    DialogResult result =
                        MessageBox.Show(Resources.Launcher_SearchProcess_, Resources.Launcher_SearchProcess_Warning,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        proc.Kill();
                    }
                    else
                    {
                        Loader.Kill = true;
                    }
                }

                Thread.Sleep(100);

                foreach (Process proc in Process.GetProcessesByName("login"))
                {
                    DialogResult result =
                        MessageBox.Show(Resources.Launcher_CloseProcess_Login_Shutdown,
                            Resources.Launcher_SearchProcess_Warning,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        proc.Kill();
                    }
                    else
                    {
                        Loader.Kill = true;
                    }
                }

                if(UnitTestDetector.IsInUnitTest)
                    goto isUnitTest;

                foreach (Process proc in Process.GetProcessesByName("mysqld"))
                {
                    DialogResult result =
                        MessageBox.Show(Resources.Launcher_SearchProcess2_, Resources.Launcher_SearchProcess_Warning,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        proc.Kill();
                    }
                    else
                    {
                        Loader.Kill = true;
                    }
                }

                isUnitTest:;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.Launcher_SearchProcess0_ +
                                ex.Message, Resources.Launcher_SearchProcess_Warning, MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }


        }

        #endregion

        #region [ OtherMethods ]

        public void statusChage(string msg, bool islink)
        {
            Status = msg;
            tssStatus.IsLink = islink;
        }

        public void checklang(bool option)
        {
            if (option)
                switch (lang)
                {
                    case "Hungarian":
                        magyarToolStripMenuItem.Checked = true;
                        break;
                    case "English":
                        englishToolStripMenuItem.Checked = true;
                        break;
                    case "German":
                        germanToolStripMenuItem.Checked = true;
                        break;
                    case "French":
                        frenchToolStripMenuItem.Checked = true;
                        break;
                }
            else
            {
                switch (lang)
                {
                    case "Hungarian":
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("hu-HU");
                        Thread.CurrentThread.CurrentCulture   = CultureInfo.CreateSpecificCulture("hu-HU");
                        break;
                    case "English":
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                        Thread.CurrentThread.CurrentCulture   = CultureInfo.InvariantCulture;
                        break;
                    case "German":
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("de");
                        Thread.CurrentThread.CurrentCulture   = CultureInfo.CreateSpecificCulture("de");
                        break;
                    case "French":
                        Thread.CurrentThread.CurrentUICulture = CultureInfo.CreateSpecificCulture("fr");
                        Thread.CurrentThread.CurrentCulture   = CultureInfo.CreateSpecificCulture("fr");
                        break;
                }
            }
        }

        private void animateStatus(bool animate)
        {
            if (animate)
            {
                tssStatus.Image = Resources.search_animation;
            }
            else
            {
                tssStatus.Image = null;
            }
        }

        public void startNStop()
        {
            if (!_startStop)
            {
                startstopToolStripMenuItem.Enabled            = false;
                startToolStripMenuItem.Enabled                = false;
                restartToolStripMenuItem1.Enabled             = false;
                restartToolStripMenuItem2.Enabled             = false;
                sendCommandForServerToolStripMenuItem.Enabled = false;
                StartAll();
                _startStop                       = true;
                startstopToolStripMenuItem.Text  = Resources.Launcher_startNStop_Stop;
                startToolStripMenuItem.Text      = Resources.Launcher_startNStop_Stop;
                startstopToolStripMenuItem.Image = Resources.Button_stop_icon;
                startToolStripMenuItem.Image     = Resources.Button_stop_icon;
            }
            else
            {
                resetAllRandomBotsToolStripMenuItem.Enabled     = false;
                randomizeBotsToolStripMenuItem.Enabled          = false;
                resetBotsToolStripMenuItem.Enabled              = false;
                randomizeBotsToolStripMenuItem1.Enabled         = false;
                restartToolStripMenuItem1.Enabled               = false;
                restartToolStripMenuItem2.Enabled               = false;
                sendCommandForServerToolStripMenuItem.Enabled   = false;
                exportImportCharactersToolStripMenuItem.Enabled = false;
                startstopToolStripMenuItem.Image                = Resources.Play_1_Hot_icon;
                startToolStripMenuItem.Image                    = Resources.Play_1_Hot_icon;
                startstopToolStripMenuItem.Text                 = Resources.Launcher_startNStop_Start;
                startToolStripMenuItem.Text                     = Resources.Launcher_startNStop_Start;
                _startStop                                      = false;
                CheckMangosCrashed.Stop();
                GetSqlOnlineBot.Stop();
                tssLOnline.Text = Resources.Launcher_startNStop_Online_bot__N_A;
                SaveAnnounce();
                bwStopWorld.RunWorkerAsync(); //
                //StatusIcon();
                //statusChage(Resources.Launcher_startNStop_Derver_is_down, false);
            }
        }

        public int getmemory()
        {
            return Convert.ToInt32(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory/1024/1024);
        }

        public void MenuItemsDisableAfterLoad()
        {
            resetAllRandomBotsToolStripMenuItem.Enabled        = false;
            exportImportCharactersToolStripMenuItem.Enabled    = false;
            startstopToolStripMenuItem.Enabled                 = false;
            randomizeBotsToolStripMenuItem.Enabled             = false;
            resetBotsToolStripMenuItem.Enabled                 = false;
            randomizeBotsToolStripMenuItem1.Enabled            = false;
            restartToolStripMenuItem1.Enabled                  = false;
            restartToolStripMenuItem2.Enabled                  = false;
            sendCommandForServerToolStripMenuItem.Enabled      = false;
            lanSwitcherToolStripMenuItem1.Enabled              = false;
            lanSwitcherToolStripMenuItem.Enabled               = false;
            accountToolToolStripMenuItem1.Enabled              = false;
        }

        public void StatusIcon()
        {
            if (!_restart)
            {
                pbNotAvailM.Visible = true;
            }
            //red
            pbNotAvailR.Visible = true;
            pbNotAvailW.Visible = true;
            //yellow
            pbTempM.Visible = false;
            pbTempR.Visible = false;
            pbTempW.Visible = false;
            //green
            pbAvailableM.Visible = false;
            pbAvailableR.Visible = false;
            pbAvailableW.Visible = false;
        }

        public void font()
        {
            try
            {
                var pfc = new PrivateFontCollection();
                pfc.AddFontFile("data\\font\\Wfont.ttf");
                lblMysql.Font = new Font(pfc.Families[0], 26, FontStyle.Regular);
                lblRealm.Font = new Font(pfc.Families[0], 26, FontStyle.Regular);
                lblWorld.Font = new Font(pfc.Families[0], 26, FontStyle.Regular);
            }
            catch (Exception)
            {
            }
        }

        private void WindowSize(bool min)
        {
            if (min)
            {
                groupBox1.Height = 140;
                Height           = 230;
            }
            else
            {
                groupBox1.Height = 174;
                Height           = 265;
            }
        }

        public void Traymsg()
        {
            if (!_restart)
            {
                NotifyBallon(1000, Resources.Launcher_Traymsg_Servers_Started, Resources.Launcher_Traymsg_Ready_to_play,
                    false);
            }
            else
            {
                NotifyBallon(1000, Resources.Launcher_Traymsg_Servers_Started, Resources.Launcher_Traymsg_Ready_to_play,
                    false);
                _restart = false;
            }
            _world = "";
        }

        public void startWow()
        {
            CheckLanIpInRealmDatabase();

            if (wowExePath == "" || realmListPath == "")
            {
                DialogMethod();
            }
            else
            {
                RealmChange();
                Process.Start(wowExePath);
                Check.Start();

            }
        }

        public void DialogMethod()
        {
            var dialog = new OpenFileDialog();

            dialog.InitialDirectory = "c:\\";
            dialog.Filter           = "Executable (*wow.exe)|*wow.exe";
            dialog.FilterIndex      = 2;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    wowExePath      = dialog.FileName;
                    realmDialogPath = Path.GetDirectoryName(wowExePath);
                    RealmDialog();
                }
                catch (Exception)
                {

                }
            }
        }

        public void RealmDialog()
        {
            var dialog = new OpenFileDialog();

            dialog.InitialDirectory = realmDialogPath;
            dialog.Filter           = "Realmlist File (*realmlist.wtf)|*realmlist.wtf";
            dialog.FilterIndex      = 2;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                realmListPath = dialog.FileName;
                xmlReadWrite.saveMethod();
                RealmChange();
                Process.Start(wowExePath);
                Check.Start();
            }
        }

        private void RealmChange()
        {
            try
            {
                CheckLanIpInRealmDatabase();
                OLDrealmList = File.ReadAllText(realmListPath, Encoding.UTF8);
                NotifyBallon(1000, Resources.Launcher_RealmRestore_Realmlist, "Set " + ipAdress, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.Launcher_RealmChange_ + ex.Message);
            }

            try
            {
                File.WriteAllText(realmListPath, "set realmlist " + ipAdress);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.Launcher_RealmChange0_ + ex.Message);
            }
        }

        private void RealmRestore()
        {
            try
            {
                File.WriteAllText(realmListPath, OLDrealmList);
                NotifyBallon(1000, Resources.Launcher_RealmRestore_Realmlist, Resources.Launcher_RealmRestore_Restored, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Resources.Launcher_RealmRestore_ + ex.Message);
            }
        }

        private void CheckLanIpInRealmDatabase()
        {
            try
            {
                var client = new MySQLClient("127.0.0.1", "realmd", "root", "123456", 3310);
                ipAdress = client.Select("realmlist", "id='1'")["address"];
            }
            catch (Exception)
            {
            }
        }

        private void input_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Enter:
                    e.Handled          = true;
                    e.SuppressKeyPress = true;
                    _cmd1.StandardInput.WriteLine(txbWorldDev.Text);
                    txbWorldDev.Text = "";
                    break;
                case Keys.Escape:
                    txbWorldDev.Text = "";
                    break;
            }
        }

        public static void ShutdownSql()
        {
            MysqlON                   = false;
            var startInfo             = new ProcessStartInfo();
            startInfo.CreateNoWindow  = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName        = "Database\\bin\\mysqladmin.exe";
            startInfo.Arguments       = "-u root -p123456 --port=3310 shutdown";
            startInfo.WindowStyle     = ProcessWindowStyle.Hidden;

            try
            {
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void ResetBots()
        {
            _cmd1.StandardInput.WriteLine("rndbot reset");

            DialogResult dialog = MessageBox.Show(Resources.Launcher_ResetBots_, "Question",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialog == DialogResult.Yes)
            {
                rstchck.RunWorkerAsync();
            }
        }

        public void RandomizeBotsMethod()
        {
            _cmd1.StandardInput.WriteLine("rndbot update");
            MessageBox.Show(Resources.Launcher_RandomizeBotsMethod_);
        }

        public bool CheckRunwow()
        {
            foreach (Process proc in Process.GetProcessesByName("Wow"))
            {
                return true;
            }
            return false;
        }

        public bool ProcessView()
        {
            foreach (Process proc in Process.GetProcessesByName("mangosd"))
            {
                return false;
            }
            return true;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (rtWorldDev.Lines.Length > 250)
            {
                DeleteLine(1);
            }
        }

        private void DeleteLine(int a_line)
        {
            int start_index = rtWorldDev.GetFirstCharIndexFromLine(a_line);
            int count = rtWorldDev.Lines[a_line].Length;

            if (a_line < rtWorldDev.Lines.Length - 1)
            {
                count += rtWorldDev.GetFirstCharIndexFromLine(a_line + 1) -
                         ((start_index + count - 1) + 1);
            }

            rtWorldDev.Text = rtWorldDev.Text.Remove(start_index, count);
        }

        private void StatusBarUpdater_Tick(object sender, EventArgs e)
        {
            tssStatus.Text = Status;
        }

        public bool GetLocalSrvVer()
        {
            try
            {
                CurrEmuVer = Convert.ToDouble(File.ReadAllText("SingleCore\\version"));
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        #endregion

        #region [ WindowMenuItems ]


        private void sendCommandForServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!sendCommandForServerToolStripMenuItem.Checked)
            {
                sendCommandForServerToolStripMenuItem.Checked = true;
                _serverConsoleActive                          = true;
                groupBox2.Visible                             = true;
                Height                                        = 420;
                rtWorldDev.Visible                            = true;
            }
            else
            {
                sendCommandForServerToolStripMenuItem.Checked = false;
                _serverConsoleActive                          = false;
                groupBox2.Visible                             = false;
                Height                                        = 230;
                rtWorldDev.Visible                            = false;
            }
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateInFile();
        }

        private void magyarToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            lang = "Hungarian";
            xmlReadWrite.saveMethod();
            MessageBox.Show(
                Resources.Launcher_englishToolStripMenuItem_Click_Changes_will_take_effect_when_you_restart_Launcher_,
                Resources.Launcher_magyarToolStripMenuItem_Click_1_Info,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lang = "English";
            xmlReadWrite.saveMethod();
            MessageBox.Show(
                Resources.Launcher_englishToolStripMenuItem_Click_Changes_will_take_effect_when_you_restart_Launcher_,
                Resources.Launcher_magyarToolStripMenuItem_Click_1_Info,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void frenchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lang = "French";
            xmlReadWrite.saveMethod();
            MessageBox.Show(
                Resources.Launcher_englishToolStripMenuItem_Click_Changes_will_take_effect_when_you_restart_Launcher_,
                Resources.Launcher_magyarToolStripMenuItem_Click_1_Info,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void germanToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lang = "German";
            xmlReadWrite.saveMethod();
            MessageBox.Show(
                Resources.Launcher_englishToolStripMenuItem_Click_Changes_will_take_effect_when_you_restart_Launcher_,
                Resources.Launcher_magyarToolStripMenuItem_Click_1_Info,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exportToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            export();
        }

        private void importToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            import();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _allowtext = false;
            startNStop();
        }

        private void restartToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MenuItemsDisableAfterLoad();
            RealmWorldRestart();
        }

        private void exportToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            export();
        }

        private void importToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            import();
        }


        private void restartToolStripMenuItem1_Click_1(object sender, EventArgs e)
        {
            MenuItemsDisableAfterLoad();
            RealmWorldRestart();
        }

        private void autorunToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (autorunToolStripMenuItem.Checked)
            {
                autostartToolStripMenuItem.Checked = false;
                autorunToolStripMenuItem.Checked   = false;
                Autostart                          = "0";
                xmlReadWrite.saveMethod();
            }
            else
            {
                autostartToolStripMenuItem.Checked = true;
                autorunToolStripMenuItem.Checked   = true;
                Autostart                          = "1";
                xmlReadWrite.saveMethod();
            }
        }

        private void autostartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (autostartToolStripMenuItem.Checked)
            {
                autostartToolStripMenuItem.Checked = false;
                autorunToolStripMenuItem.Checked   = false;
                Autostart                          = "0";
                xmlReadWrite.saveMethod();
            }
            else
            {
                autostartToolStripMenuItem.Checked = true;
                autorunToolStripMenuItem.Checked   = true;
                Autostart                          = "1";
                xmlReadWrite.saveMethod();
            }
        }

        private void startstopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _allowtext = false;
            startNStop();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            Application.OpenForms["Launcher"].Activate();
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            exitToolStripMenuItem1.Enabled = false;
            CheckMangosCrashed.Stop();
            GetSqlOnlineBot.Stop();
            bwCloseSPP.RunWorkerAsync();
        }

        private void worldSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["WorldConf"] == null)
            {
                var show = new WorldConf();
                show.Show();
            }
            else
            {
                Application.OpenForms["WorldConf"].Activate();
            }
        }

        private void worldSettingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["WorldConf"] == null)
            {
                var show = new WorldConf();
                show.Show();
            }
            else
            {
                Application.OpenForms["WorldConf"].Activate();
            }
        }

        private void tsmHelpUs_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["HelpUsWindow"] == null)
            {
                var show = new HelpUsWindow();
                show.Show();
            }
            else
            {
                Application.OpenForms["HelpUsWindow"].Activate();
            }
        }

        private void reportBugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["BugReport"] == null)
            {
                var show = new BugReport();
                show.Show();
            }
            else
            {
                Application.OpenForms["BugReport"].Activate();
            }
        }

        private void reportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var sReport = new BugReport();
            sReport.Show();

            if (Application.OpenForms["BugReport"] == null)
            {
                var show = new BugReport();
                show.Show();
            }
            else
            {
                Application.OpenForms["BugReport"].Activate();
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["AboutBox"] == null)
            {
                var show = new AboutBox();
                show.Show();
            }
            else
            {
                Application.OpenForms["Aboutbot"].Activate();
            }
        }

        private void tssStatus_Click(object sender, EventArgs e)
        {
            Process.Start(UpdLink);
        }

        #endregion

        #region [ TrayMenu ]

        private void openUpdateFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateInFile();
        }

        private void pbAvailableM_MouseHover(object sender, EventArgs e)
        {
            ToolTip tt = new ToolTip();
            tt.SetToolTip(this.pbAvailableM, _SqlStartTime);
        }

        private void pbAvailableR_MouseHover(object sender, EventArgs e)
        {
            ToolTip tt = new ToolTip();
            tt.SetToolTip(this.pbAvailableR, _RealmStartTime);
        }

        private void pbAvailableW_MouseHover(object sender, EventArgs e)
        {
            ToolTip tt = new ToolTip();
            tt.SetToolTip(this.pbAvailableW, _WorldStartTime);

        }

        private void botSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var botConf = new BotConf();
            botConf.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["AboutBox"] == null)
            {
                var show = new AboutBox();
                show.Show();
            }
            else
            {
                Application.OpenForms["Aboutbox"].Activate();
            }
        }

        private void botSettingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var bot = new BotConf();
            bot.Show();
        }

        private void SppTray_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                SppTray.ContextMenuStrip = cmsTray;
                MethodInfo mi = typeof (NotifyIcon).GetMethod("ShowContextMenu",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(SppTray, null);
                SppTray.ContextMenuStrip = cmsTray;
            }
        }

        private void SppTray_DoubleClick(object sender, EventArgs e)
        {
            Show();
            Application.OpenForms["Launcher"].Activate();
        }

        private void startWowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startWow();
        }

        private void lanSwitcherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["LanSwitcher"] == null)
            {
                var show = new LanSwitcher();
                show.Show();
            }
            else
            {
                Application.OpenForms["LanSwitcher"].Activate();
            }
        }

        private void accountToolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["WowAccountCreator"] == null)
            {
                var show = new WowaccountCreator();
                show.Show();
            }
            else
            {
                Application.OpenForms["WowAccountCreator"].Activate();
            }
        }

        private void changeWoWPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogMethod();
        }

        private void runWoWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startWow();
        }

        private void lanSwitcherToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["LanSwitcher"] == null)
            {
                var show = new LanSwitcher();
                show.Show();
            }
            else
            {
                Application.OpenForms["LanSwitcher"].Activate();
            }
        }

        private void accountToolToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms["WowAccountCreator"] == null)
            {
                var show = new WowaccountCreator();
                show.Show();
            }
            else
            {
                Application.OpenForms["WowAccountCreator"].Activate();
            }
        }

        private void changeWoWPathToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DialogMethod();
        }

        private void exitToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            exitToolStripMenuItem2.Enabled = false;
            CheckMangosCrashed.Stop();
            GetSqlOnlineBot.Stop();
            bwCloseSPP.RunWorkerAsync();
        }

        public void RealmWorldRestart()
        {
            CheckMangosCrashed.Stop();
            rtWorldDev.Visible = false;
            _allowtext         = false;
            _restart           = true;
            SaveAnnounce();
            bwRestart.RunWorkerAsync();
        }

        private void resetAllRandomBotsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetBots();
        }

        private void randomizeBotsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RandomizeBotsMethod();
        }

        private void resetBotsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetBots();
        }

        private void randomizeBotsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            RandomizeBotsMethod();
        }

        private void cmsTray_DoubleClick(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }

        #endregion

        #region [ Timers ]

        private void tmrUsage_Tick(object sender, EventArgs e)
        {
            int ramavail  = getAvailableRAM();
            int ramtoltal = Convert.ToInt32(getmemory());
            int ramfree   = ramtoltal - ramavail;
            int ramp      = ramtoltal/100;
            int perecent  = ramfree/ramp;
            Process proc  = Process.GetCurrentProcess();

            try
            {
                Process[] SqlProcesses;
                SqlProcesses = Process.GetProcessesByName("mysqld");
                _sqlMem      = Convert.ToString(SqlProcesses[0].WorkingSet64/1024/1024) + "MB";
            }
            catch (Exception)
            {
                _sqlMem = "N/A";
            }

            try
            {
                Process[] mangosdProcesses;
                mangosdProcesses = Process.GetProcessesByName("mangosd");
                _mangosdMem      = Convert.ToString(mangosdProcesses[0].WorkingSet64/1024/1024) + "MB";
            }
            catch (Exception)
            {
                _mangosdMem = "N/A";
            }

            try
            {
                Process[] realmdProcesses;
                realmdProcesses = Process.GetProcessesByName("login");
                _realmdMem      = Convert.ToString(realmdProcesses[0].WorkingSet64/1024/1024) + "MB";
            }
            catch (Exception)
            {
                _realmdMem = "N/A";
            }

            tssUsage.ToolTipText = "Launcher: " + Convert.ToString(proc.WorkingSet64/1024/1024) + "MB" +
                                   "\nMysql Server: " + _sqlMem + "\nLogin Server: " + _realmdMem + "\nGame Server: " +
                                   _mangosdMem;

            tssUsage.Text = "CPU: " + Convert.ToInt32(getCurrentCpuUsage()) + "% |" + " RAM: " + perecent + "%" + " (" +
                            ramfree + "/" + ramtoltal + "MB)";

            if (perecent >= 75)
            {
                if (!SysProtect)
                {
                    _cmd1.StandardInput.WriteLine(".announce Current ram usage: {0}%", perecent);
                    _cmd1.StandardInput.WriteLine(".announce If the RAM usage exceeds 90%, auto-restart!}");
                    SysProtect = true;
                }
            }

            if (perecent >= 90)
            {
                if (SysProtect)
                {
                    _cmd1.StandardInput.WriteLine(".announce Auto-Restart!}");
                    restartToolStripMenuItem1_Click_1(new object(), new EventArgs());
                }
            }


        }

        private void tmrRealm_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_realm.Contains("realmd process priority class set to HIGH"))
                {
                    tmrRealm.Stop();
                    DateTime end1          = DateTime.Now;
                    _RealmStartTime = (end1 - _start1).TotalSeconds.ToString("##.0" + "sec");

                    pbAvailableR.Visible   = true;
                    pbTempR.Visible        = false;
                    WorldStart();
                }
            }
            catch
            {
            }
        }

        private void tmrWorld_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_world.Contains("Loading Script Names..."))
                {
                    pbarWorld.Value = 10;
                }
                if (_world.Contains("Loading Item converts..."))
                {
                    pbarWorld.Value = 20;
                    _world          = "";
                }
                if (_world.Contains("Loading pet levelup spells..."))
                {
                    pbarWorld.Value = 30;
                    _world          = "";
                }
                if (_world.Contains("Loading Quests Relations..."))
                {
                    pbarWorld.Value = 40;
                    _world          = "";
                }
                if (_world.Contains("Loading the max pet number..."))
                {
                    pbarWorld.Value = 50;
                    _world          = "";
                }
                if (_world.Contains("Loading Achievements..."))
                {
                    pbarWorld.Value = 60;
                    _world          = "";
                }
                if (_world.Contains("Waypoint templates loaded"))
                {
                    pbarWorld.Value = 70;
                    _world          = "";
                }
                if (_world.Contains("Loading GameTeleports..."))
                {
                    pbarWorld.Value = 80;
                    _world          = "";
                }
                if (_world.Contains("Initialize AuctionHouseBot..."))
                {
                    pbarWorld.Value = 90;
                }
                if (_world.Contains("WORLD: World initialized"))
                {
                    tmrWorld.Stop();

                    DateTime end1          = DateTime.Now;
                    _WorldStartTime = (end1 - _start1).TotalSeconds.ToString("##.0" + "sec");
                    pbarWorld.Value        = 100;
                    statusChage(Resources.Launcher_tmrWorld_Tick_Online, false);
                    animateStatus(false);
                    pbAvailableW.Visible = true;
                    pbTempW.Visible      = false;
                    pbarWorld.Visible    = false;
                    WindowSize(true);
                    pbarWorld.Value = 0;
                    _allowtext      = true;

                    resetAllRandomBotsToolStripMenuItem.Enabled         = true;
                    randomizeBotsToolStripMenuItem.Enabled              = true;
                    resetBotsToolStripMenuItem.Enabled                  = true;
                    randomizeBotsToolStripMenuItem1.Enabled             = true;
                    lanSwitcherToolStripMenuItem1.Enabled               = true;
                    lanSwitcherToolStripMenuItem.Enabled                = true;
                    accountToolToolStripMenuItem1.Enabled               = true;
                    startstopToolStripMenuItem.Enabled                  = true;
                    startToolStripMenuItem.Enabled                      = true;
                    restartToolStripMenuItem1.Enabled                   = true;
                    restartToolStripMenuItem1.Enabled                   = true;
                    sendCommandForServerToolStripMenuItem.Enabled       = true;
                    exportImportCharactersToolStripMenuItem.Enabled     = true;

                    bwUpdate.RunWorkerAsync(); //check update

                    Traymsg();
                    GetSqlOnlineBot.Start(); //get online bots

                    if (Updater)
                    {
                        Updater = false;
                    }

                    CheckMangosCrashed.Start();
                    SrvAnnounce.Start();
                }
            }
            catch
            {
            }
        }

        private void SqlStartCheck_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_sql.Contains("Version: '5.5.32-MariaDB'  socket: ''  port: 3310  mariadb.org binary distribution"))
                {
                    MysqlON = true;
                    SqlStartCheck.Stop();
                    DateTime end1        = DateTime.Now;
                    _SqlStartTime = (end1 - _start1).TotalSeconds.ToString("##.0" + " sec");
                    pbAvailableM.Visible = true;
                    pbTempM.Visible      = false;
                    _sql                 = "";
                    if (Sqlimport) bwImport.RunWorkerAsync();
                    if (dbupdate)
                    {
                        animateStatus(false);
                        Hide();
                        startupdate();
                    }
                    if (sqlexport) bwExport.RunWorkerAsync();
                    if (!OnlyMysqlStart)
                    {
                        RealmdStart();
                    }
                }
            }
            catch
            {
            }
        }

        private void CheckWowRun_Tick(object sender, EventArgs e)
        {
            CheckWowRun.Stop();

            Traymsg();

            if (!_restart && !CheckRunwow())
            {
                DialogResult dialog =
                    MessageBox.Show(Resources.Launcher_CheckWowRun_Tick_Would_you_like_to_start_World_of_Warcraft_,
                        "Question",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialog == DialogResult.Yes)
                {
                    startWow();
                }
            }
        }

        private void GetSqlOnlineBot_Tick(object sender, EventArgs e)
        {
            tssLOnline.ToolTipText = Resources.Total_Character__ + new GetAllChar().GetChar("SELECT COUNT(*) FROM characters");
            tssLOnline.Text        = Resources.Launcher_GetSqlOnlineBot_Tick_Online_Bot_ + new GetAllChar().GetChar("SELECT SUM(online) FROM characters");
            SppTray.Text           = tssLOnline.Text;
        }

        private void SrvAnnounce_Tick(object sender, EventArgs e)
        {
            _cmd1.StandardInput.WriteLine(".announce {0}", tssLOnline);
        }

        private void CheckMangosCrashed_Tick(object sender, EventArgs e)
        {
            if (ProcessView() && !Updater)
            {
                _restart = true;
                NotifyBallon(1000, Resources.Launcher_CheckMangosCrashed_Tick_Mangosd_Crashed,
                    Resources.Launcher_CheckMangosCrashed_Tick_The_process_is_automatically_restart_, true);
                RealmWorldRestart();
            }
        }

        #endregion

        #region [ Update ]


        public void UpdateInFile()
        {
            if (MysqlON)
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Update";
                openFile.Filter = "SPP Upate (*.sppupdate)|*.sppupdate";

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    UpdateUnpack = openFile.FileName;
                    WindowState = FormWindowState.Normal;
                    Show();
                    statusChage(Resources.Launcher_updateToolStripMenuItem_Click_Decompress_Update, false);
                    animateStatus(true);
                    bWUpEx.RunWorkerAsync();
                }
            }
            else
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title = "Update";
                openFile.Filter = "SPP Upate (*.sppupdate)|*.sppupdate";

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    UpdateUnpack = openFile.FileName;
                    WindowState = FormWindowState.Normal;
                    pbAvailableM.Visible = true;
                    Show();
                    statusChage(Resources.Launcher_updateToolStripMenuItem_Click_Decompress_Update, false);
                    animateStatus(true);
                    bWUpEx.RunWorkerAsync();
                }
            }
        }

        private void startupdate()
        {
            DatabaseUpdate frm2 = new DatabaseUpdate(this);
            frm2.Show();
        }

        private void bWUpEx_DoWork(object sender, DoWorkEventArgs e)
        {
            new UpdateExtract().Extract();
        }

        private void bWUpEx_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusChage(Resources.Launcher_bWUpEx_RunWorkerCompleted_Completed, false);
            animateStatus(false);

            try
            {
                int db    = Convert.ToInt32(File.ReadAllText(@"update\version"));
                int local = Convert.ToInt32(File.ReadAllText(@"SingleCore\version"));
                if (db   != local)
                {
                    if (!MysqlON)
                    {
                        dbupdate = true;
                        OnlyMysqlStart = true;
                        StartAll();
                    }
                    else
                    {
                        dbupdate = true;
                        MenuItemsDisableAfterLoad();
                        rtWorldDev.Visible = false;
                        CheckMangosCrashed.Stop();
                        _allowtext = false;
                        _restart   = true;
                        CloseProcess(true);
                        GetSqlOnlineBot.Stop();
                        tssLOnline.Text = Resources.Launcher_import_Online_Bots__N_A;
                        StatusIcon();
                        pbAvailableM.Visible = true;
                        Hide();
                        startupdate();
                    }
                }
                else
                {
                    Directory.Delete(@"update", true);
                    MessageBox.Show(Resources.Your_DB_is_up_to_date_);
                    statusChage(Resources.Launcher_bwUpdate_RunWorkerCompleted_Up_to_date, false);
                }
            }
            catch
            { }
        }

        private void WasThisUpdate()
        {
            if (File.Exists("SppLauncher_OLD.exe"))
            {
                File.Delete("SppLauncher_OLD.exe");
                File.SetAttributes("SppLauncher.exe", FileAttributes.Normal);
                var uC = new Update_Completed();
                uC.Show(); //open update completed form
            }
        }

        //public void WasThisLangUpdate()
        //{
        //    if (File.Exists(@"Languages\hu\SppLauncher.resources_new.dll"))
        //    {
        //        File.Delete(@"Languages\hu\SppLauncher.resources.dll");
        //        File.Delete(@"Languages\de\SppLauncher.resources.dll");
        //        File.Delete(@"Languages\fr\SppLauncher.resources.dll");

        //        File.Move(@"Languages\hu\SppLauncher.resources_new.dll", @"Languages\hu\SppLauncher.resources.dll");
        //        File.SetAttributes(@"Languages\hu\SppLauncher.resources.dll", FileAttributes.Normal);

        //        File.Move(@"Languages\de\SppLauncher.resources_new.dll", @"Languages\de\SppLauncher.resources.dll");
        //        File.SetAttributes(@"Languages\de\SppLauncher.resources.dll", FileAttributes.Normal);

        //        File.Move(@"Languages\fr\SppLauncher.resources_new.dll", @"Languages\fr\SppLauncher.resources.dll");
        //        File.SetAttributes(@"Languages\fr\SppLauncher.resources.dll", FileAttributes.Normal);
        //    }
        //}

        public void test()
        {
            try
            {
                File.SetAttributes("SppLauncher_NEW.exe", FileAttributes.Normal);
            }
            catch
            {
            }

        }

        public bool GetUpdate()
        {
            try
            {
                statusChage(Resources.Launcher_Checking_Update, false);
                animateStatus(true);
                var client     = new WebClient();
                Stream stream  = client.OpenRead("https://raw.github.com/conan513/SingleCore/SPP/Tools/update_new.txt");
                var reader     = new StreamReader(stream);
                String content = reader.ReadToEnd();
                string[] parts = content.Split(';');
                content        = parts[0];
                RemoteEmuVer   = Convert.ToDouble(parts[1]);
                UpdLink        = parts[2];

                if (content != currProgVer)
                {
                    if (MessageBox.Show(
                            Resources.Launcher_bwUpdate_DoWork_New_Version_Available__V + content + "\n" +
                            Resources.Launcher_bwUpdate_DoWork_You_want_to_download_,
                            Resources.Launcher_bwUpdate_DoWork_New_Version, MessageBoxButtons.YesNo,
                            MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1,
                            MessageBoxOptions.DefaultDesktopOnly) == DialogResult.Yes)
                    {
                        _updateYes = true;
                    }
                }
            }
            catch (Exception)
            {
                statusChage(Resources.Launcher_bwUpdate_DoWork_ERROR, false);
                return false;
            }
            return true;
        }

        private void bwUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            checklang(false);
            GetUpdate();
            CurrEmuVer = Convert.ToDouble(File.ReadAllText("SingleCore\\version"));

            if (RemoteEmuVer > CurrEmuVer)
            {
                statusChage(Resources.Launcher_bwUpdate_DoWork_New_Server_Update_Available, true);
                animateStatus(false);
            }
            else
            {
                statusChage(Resources.Launcher_bwUpdate_RunWorkerCompleted_Up_to_date, false);
                animateStatus(false);
            }
        }

        private void bwUpdate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_updateYes)
            {
                CheckMangosCrashed.Stop();
                GetSqlOnlineBot.Stop();
                CloseProcess(false);
                pbAvailableW.Visible = false;
                pbNotAvailW.Visible  = true;
                pbAvailableR.Visible = false;
                pbNotAvailR.Visible  = true;
                Thread.Sleep(1000);
                var update = new Update();
                update.Show();
            }
        }

        #endregion

        #region [ Backup ]

        private void import()
        {
            if (MysqlON)
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title          = Resources.Launcher_import_Import_Characters;
                openFile.Filter         = "SPP Backup (*.sppbackup)|*.sppbackup|All files (*.*)|*.*";

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    MenuItemsDisableAfterLoad();
                    importFile         = openFile.FileName;
                    rtWorldDev.Visible = false;
                    CheckMangosCrashed.Stop();
                    _allowtext = false;
                    _restart = true;
                    bwRunImport.RunWorkerAsync();
                    //CloseProcess(true);
                    //GetSqlOnlineBot.Stop();
                    //tssLOnline.Text = Resources.Launcher_import_Online_Bots__N_A;
                    //StatusIcon();
                    //WindowState = FormWindowState.Normal;
                    //Show();
                    //pbAvailableM.Visible = true;
                    //statusChage(Resources.Launcher_import_Decompress, false);
                    //animateStatus(true);
                    //bwImport.RunWorkerAsync();
                }
            }
            else
            {
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.Title          = Resources.Launcher_import_Import_Characters;
                openFile.Filter         = "SPP Backup (*.sppbackup)|*.sppbackup|All files (*.*)|*.*";

                if (openFile.ShowDialog() == DialogResult.OK)
                {
                    Sqlimport = true;
                    OnlyMysqlStart = true;
                    StartAll();
                    MenuItemsDisableAfterLoad();
                    importFile   = openFile.FileName;
                    WindowState  = FormWindowState.Normal;
                    Show();
                    statusChage(Resources.Launcher_import_Decompress, false);
                    animateStatus(true);
                }
            }
        }

        private void bwImport_DoWork(object sender, DoWorkEventArgs e)
        {
            checklang(false);
            ImportExtract();
            statusChage(Resources.Launcher_import_Import_Characters, false);

            string conn            = "server=127.0.0.1;user=root;pwd=123456;database=characters;port=3310;convertzerodatetime=true;";
            MySqlBackup mb         = new MySqlBackup(conn);
            mb.ImportInfo.FileName = getTemp + "\\save01";
            mb.Import();

            statusChage(Resources.Launcher_bwImport_DoWork_Import_Accounts, false);

            string conn1            = "server=127.0.0.1;user=root;pwd=123456;database=realmd;port=3310;convertzerodatetime=true;";
            MySqlBackup mb1         = new MySqlBackup(conn1);
            mb1.ImportInfo.FileName = getTemp + "\\save02";
            mb1.Import();
        }

        private void bwImport_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Sqlimport = false;
            if (OnlyMysqlStart)
            {
                ShutdownSql();
                pbAvailableM.Visible               = false;
                pbNotAvailM.Visible                = true;
                startstopToolStripMenuItem.Enabled = true;
            }
            if (!OnlyMysqlStart)
            {
                StartAll();
            }
            OnlyMysqlStart = false;
            statusChage(Resources.Launcher_bwImport_RunWorkerCompleted_Import_Completed, false);
            animateStatus(false);
            File.Delete(getTemp + "\\save01");
            File.Delete(getTemp + "\\save02");
        }

        private void ImportExtract() //.sppbackup extract
        {
            string unpck       = importFile;
            string unpckDir    = getTemp;
            using (ZipFile zip = ZipFile.Read(unpck))
            {
                foreach (ZipEntry e in zip)
                {
                    e.Password = "89765487";
                    e.Extract(unpckDir, ExtractExistingFileAction.OverwriteSilently);
                }
            }
        }

        private void export()
        {
            if (MysqlON)
            {
                SaveFileDialog saveFile = new SaveFileDialog();
                saveFile.Title          = Resources.Launcher_export_Export_Characters;
                saveFile.Filter         = "SPP Backup (*.sppbackup)|*.sppbackup|All files (*.*)|*.*";
                saveFile.FileName       = Resources.Launcher_export_Backup;

                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    animateStatus(true);
                    statusChage(Resources.Launcher_export_Exporting_Characters, false);
                    exportfile   = saveFile.FileName;
                    bwExport.RunWorkerAsync();
                }
            }
            else
            {
                SaveFileDialog saveFile = new SaveFileDialog();
                saveFile.Title          = Resources.Launcher_export_Export_Characters;
                saveFile.Filter         = "SPP Backup (*.sppbackup)|*.sppbackup|All files (*.*)|*.*";
                saveFile.FileName       = Resources.Launcher_export_Backup;

                if (saveFile.ShowDialog() == DialogResult.OK)
                {
                    OnlyMysqlStart = true;
                    sqlexport = true;
                    StartAll();
                    animateStatus(true);
                    statusChage(Resources.Launcher_export_Exporting_Characters, false);
                    exportfile   = saveFile.FileName;
                }
            }
        }

        private void bwExport_DoWork(object sender, DoWorkEventArgs e)
        {
            checklang(false);
            string conn =
                "server=127.0.0.1;user=root;pwd=123456;database=characters;port=3310;convertzerodatetime=true;";
            MySqlBackup mb = new MySqlBackup(conn);
            mb.ExportInfo.FileName = getTemp + "\\save01";
            mb.Export();

            statusChage(Resources.Launcher_bwExport_DoWork_Export_Accounts, false);

            string conn1 = "server=127.0.0.1;user=root;pwd=123456;database=realmd;port=3310;convertzerodatetime=true;";
            MySqlBackup mb1 = new MySqlBackup(conn1);
            mb1.ExportInfo.FileName = getTemp + "\\save02";
            mb1.Export();

            statusChage(Resources.Launcher_bwExport_DoWork_Compressing, false);

            using (ZipFile zip = new ZipFile()) //create .sppbackup
            {
                zip.Password = "89765487";
                zip.AddFile(getTemp + "\\save01", @"\");
                zip.AddFile(getTemp + "\\save02", @"\");
                zip.Save(exportfile);
            }
        }

        private void bwExport_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            sqlexport = false;
            if (OnlyMysqlStart)
            {
                ShutdownSql();

                pbAvailableM.Visible               = false;
                pbNotAvailM.Visible                = true;
                startstopToolStripMenuItem.Enabled = true;
            }

            OnlyMysqlStart = false;
            Status = Resources.Launcher_bwExport_RunWorkerCompleted_Export_Completed;
            statusChage(Resources.Launcher_bwExport_RunWorkerCompleted_Export_Completed, false);
            animateStatus(false);
            File.Delete(getTemp + "\\save01");
            File.Delete(getTemp + "\\save02");
        }

        #endregion

        private void bwCloseProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            CloseProcess(false);
        }

        private void bwCloseProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StatusIcon();
            statusChage(Resources.Launcher_startNStop_Derver_is_down, false);
            animateStatus(false);
        }

        private void bwCloseProcess1_DoWork(object sender, DoWorkEventArgs e)
        {
            CloseProcess(true);
        }

        private void bwCloseProcess1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StatusIcon();
            WindowState = FormWindowState.Normal;
            Show();
            StartAll();
        }

        private void bwRunExport_DoWork(object sender, DoWorkEventArgs e)
        {
            CloseProcess(true);
        }

        private void bwRunExport_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            GetSqlOnlineBot.Stop();
            tssLOnline.Text = Resources.Launcher_import_Online_Bots__N_A;
            StatusIcon();
            WindowState = FormWindowState.Normal;
            Show();
            pbAvailableM.Visible = true;
            statusChage(Resources.Launcher_import_Decompress, false);
            animateStatus(true);
            bwImport.RunWorkerAsync();
        }
        
        private void SaveAnnounce()
        {
            _cmd1.StandardInput.WriteLine("saveall");
            _cmd1.StandardInput.WriteLine(".announce Saving...");
        }

        private void bwCloseSPP_DoWork(object sender, DoWorkEventArgs e)
        {
            CloseProcess(false);
        }

        private void bwCloseSPP_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Hide();
            SppTray.Visible = false;
            Loader.Kill = true;
        }
    }
}