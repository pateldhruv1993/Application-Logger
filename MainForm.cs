using ApplicationLogger.Properties;
using System;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Net.Sockets;


namespace ApplicationLogger
{

    public partial class MainForm : Form
    {

        // Constants
        private const string SETTINGS_FIELD_RUN_AT_STARTUP = "RunAtStartup";
        private const string REGISTRY_KEY_ID = "ApplicationLogger";					// Registry app key for when it's running at startup


        // Properties
        private Timer timerCheck;
        private ContextMenu contextMenu;
        private MenuItem menuItemOpen;
        private MenuItem menuItemOpenLog;
        private MenuItem menuItemStartStop;
        private MenuItem menuItemRunAtStartup;
        private MenuItem menuItemExit;
        private bool allowClose;
        private bool allowShow;
        private bool isRunning;
        private bool isUserIdle;
        private bool hasInitialized;		
        

        private ConfigManager configMgr = new ConfigManager();
        private LoggingManager logMgr;

        //Variables for ICP
        TcpClient tcpclient;
        Stream stm;
        ASCIIEncoding asen = new ASCIIEncoding();
        bool isConnectedIPCServer = false;
        int IPCSkipCount = 0;


        public MainForm()
        {
            InitializeComponent();
            initializeForm();
        }


        private void onFormLoad(object sender, EventArgs e)
        {
            // First time the form is shown
        }

        protected override void SetVisibleCore(bool isVisible)
        {
            if (!allowShow)
            {
                // Initialization form show, when it's ran: doesn't allow showing form
                isVisible = false;
                if (!this.IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(isVisible);
        }

        private void onFormClosing(object sender, FormClosingEventArgs e)
        {
            // Form is attempting to close
            if (!allowClose)
            {
                // User initiated, just minimize instead
                e.Cancel = true;
                Hide();
            }
        }

        private void onFormClosed(object sender, FormClosedEventArgs e)
        {
            // Stops everything
            stop();

            // If debugging, un-hook itself from startup
            if (System.Diagnostics.Debugger.IsAttached && windowsRunAtStartup) windowsRunAtStartup = false;
        }

        private void onTimer(object sender, EventArgs e)
        {
            // Timer tick: check for the current application


            // Check the user is idle
            if (SystemHelper.GetIdleTime() >= configMgr.config.idleTime * 1000f)
            {
                if (!isUserIdle)
                {
                    // User is now idle
                    isUserIdle = true;
                    logMgr.logUserIdle();
                }
            }
            else
            {
                if (isUserIdle)
                {
                    // User is not idle anymore
                    isUserIdle = false;
                }
            }

            // Check the user process
            if (!isUserIdle)
            {
                logMgr.checkForNewProcess();
            }



            //TCP Client Stuff
            if (IPCSkipCount >= 3)//configMgr.config.TCPInterval)
            {
                if (isConnectedIPCServer)
                {
                    connectedToIPCLabel.Text = "Connected to IPC: TRUE";
                    try
                    {

                        byte[] bb = new byte[100];
                        int k = stm.Read(bb, 0, 100);

                        if (k != 0)
                        {
                            String receivedData = System.Text.Encoding.Default.GetString(bb);



                            byte[] ba = asen.GetBytes("Say it don't spray it Ron.");
                            stm.Write(ba, 0, ba.Length);
                        }
                        isConnectedIPCServer = true;
                    }
                    catch (Exception excep)
                    {
                        Console.WriteLine("Error..... " + excep.StackTrace);

                        //May be you got disconnected??
                        isConnectedIPCServer = false;
                    }
                }
                else
                {
                    connectedToIPCLabel.Text = "Connected to IPC: FALSE";
                    connectToIPCServer();
                }

                IPCSkipCount = 0;

            }
            else
            {
                IPCSkipCount++;
            }

            // Write to log if enough time passed
            logMgr.checkIfShouldCommit();
        }

        private void onResize(object sender, EventArgs e)
        {
            // Resized window
            //notifyIcon.BalloonTipTitle = "Minimize to Tray App";
            //notifyIcon.BalloonTipText = "You have successfully minimized your form.";

            if (WindowState == FormWindowState.Minimized)
            {
                //notifyIcon.ShowBalloonTip(500);
                this.Hide();
            }
        }

        private void onMenuItemOpenClicked(object Sender, EventArgs e)
        {
            showForm();
        }

        private void onMenuItemStartStopClicked(object Sender, EventArgs e)
        {
            if (isRunning)
            {
                stop();
            }
            else
            {
                start();
            }
        }

        private void onMenuItemOpenLogClicked(object Sender, EventArgs e)
        {
            logMgr.commitLines();
            Process.Start(logMgr.getLogFileName());
        }

        private void onMenuItemRunAtStartupClicked(object Sender, EventArgs e)
        {
            menuItemRunAtStartup.Checked = !menuItemRunAtStartup.Checked;
            settingsRunAtStartup = menuItemRunAtStartup.Checked;
            applySettingsRunAtStartup();
        }

        private void onMenuItemExitClicked(object Sender, EventArgs e)
        {
            exit();
        }

        private void onDoubleClickNotificationIcon(object sender, MouseEventArgs e)
        {
            showForm();
        }


        private void initializeForm()
        {
            // Initialize

            if (!hasInitialized)
            {

                // Read configuration
                configMgr.readConfiguration();

                // Initialize logging manager class through its constructor
                logMgr = new LoggingManager(configMgr, this);


                allowClose = false;
                isRunning = false;
                allowShow = false;

                // Force working folder
                System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);


                // Create context menu for the tray icon and update it
                createContextMenu();

                // Update tray
                updateTrayIcon();

                // Check if it needs to run at startup
                applySettingsRunAtStartup();

                // Finally, start
                start();

                hasInitialized = true;
            }
        }

        private void createContextMenu()
        {
            // Initialize context menu
            contextMenu = new ContextMenu();

            // Initialize menu items
            menuItemOpen = new MenuItem();
            menuItemOpen.Index = 0;
            menuItemOpen.Text = "&Open";
            menuItemOpen.Click += new EventHandler(onMenuItemOpenClicked);
            contextMenu.MenuItems.Add(menuItemOpen);

            menuItemStartStop = new MenuItem();
            menuItemStartStop.Index = 0;
            menuItemStartStop.Text = ""; // Set later
            menuItemStartStop.Click += new EventHandler(onMenuItemStartStopClicked);
            contextMenu.MenuItems.Add(menuItemStartStop);

            contextMenu.MenuItems.Add("-");

            menuItemOpenLog = new MenuItem();
            menuItemOpenLog.Index = 0;
            menuItemOpenLog.Text = ""; // Set later
            menuItemOpenLog.Click += new EventHandler(onMenuItemOpenLogClicked);
            contextMenu.MenuItems.Add(menuItemOpenLog);

            contextMenu.MenuItems.Add("-");

            menuItemRunAtStartup = new MenuItem();
            menuItemRunAtStartup.Index = 0;
            menuItemRunAtStartup.Text = "Run at Windows startup";
            menuItemRunAtStartup.Click += new EventHandler(onMenuItemRunAtStartupClicked);
            menuItemRunAtStartup.Checked = settingsRunAtStartup;
            contextMenu.MenuItems.Add(menuItemRunAtStartup);

            contextMenu.MenuItems.Add("-");

            menuItemExit = new MenuItem();
            menuItemExit.Index = 1;
            menuItemExit.Text = "E&xit";
            menuItemExit.Click += new EventHandler(onMenuItemExitClicked);
            contextMenu.MenuItems.Add(menuItemExit);

            notifyIcon.ContextMenu = contextMenu;

            updateContextMenu();
        }

        public void updateContextMenu()
        {
            // Update start/stop command
            if (menuItemStartStop != null)
            {
                if (isRunning)
                {
                    menuItemStartStop.Text = "&Stop";
                }
                else
                {
                    menuItemStartStop.Text = "&Start";
                }
            }

            // Update filename
            if (menuItemOpenLog != null)
            {
                var filename = logMgr.getLogFileName();
                if (!System.IO.File.Exists(filename))
                {
                    // Doesn't exist
                    menuItemOpenLog.Text = "Open &log file";
                    menuItemOpenLog.Enabled = false;
                }
                else
                {
                    // Exists
                    menuItemOpenLog.Text = "Open &log file (" + filename + ")";
                    menuItemOpenLog.Enabled = true;
                }
            }
        }

        private void updateTrayIcon()
        {
            if (isRunning)
            {
                notifyIcon.Icon = ApplicationLogger.Properties.Resources.iconNormal;
                notifyIcon.Text = "Application Logger (started)";
            }
            else
            {
                notifyIcon.Icon = ApplicationLogger.Properties.Resources.iconStopped;
                notifyIcon.Text = "Application Logger (stopped)";
            }
        }

        

        private void start()
        {
            if (!isRunning)
            {
                // Initialize timer
                timerCheck = new Timer();
                timerCheck.Tick += new EventHandler(onTimer);
                timerCheck.Interval = (int)(configMgr.config.timeCheckInterval * 1000f);
                timerCheck.Start();
                isRunning = true;
                logMgr.fixedSizeQueue = new FixedSizedQueue<string>(Int32.Parse(configMgr.config.maxLogCache + ""));


                //Log system start (Not really. This just means app started. BUT as I plan to run it on startup, this should be good)
                logMgr.logLine("status::start");


                connectToIPCServer();
                


                updateContextMenu();
                updateTrayIcon();
            }
        }

        private void stop()
        {
            if (isRunning)
            {
                try
                {
                    byte[] ba = asen.GetBytes("Logger::Shutting_Down");
                    stm.Write(ba, 0, ba.Length);
                    tcpclient.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:: " + e.StackTrace);
                }

                
                logMgr.logStop();

                timerCheck.Stop();
                timerCheck.Dispose();
                timerCheck = null;
                isRunning = false;

                updateContextMenu();
                updateTrayIcon();


            }
        }

        public void updateText(string text)
        {
            labelApplication.Text = "Current App: " + text;
            debugLogTextBox.AppendText(text + "\n");
        }

        private void applySettingsRunAtStartup()
        {
            // Check whether it's properly set to run at startup or not
            if (settingsRunAtStartup)
            {
                // Should run at startup
                if (!windowsRunAtStartup) windowsRunAtStartup = true;
            }
            else
            {
                // Should not run at startup
                if (windowsRunAtStartup) windowsRunAtStartup = false;
            }
        }

        private void showForm()
        {
            allowShow = true;
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void exit()
        {
            allowClose = true;
            Close();
        }

        
        private bool settingsRunAtStartup
        {
            // Whether the settings say the app should run at startup or not
            get
            {
                return (bool)Settings.Default[SETTINGS_FIELD_RUN_AT_STARTUP];
            }
            set
            {
                Settings.Default[SETTINGS_FIELD_RUN_AT_STARTUP] = value;
                Settings.Default.Save();
            }
        }

        private bool windowsRunAtStartup
        {
            // Whether it's actually set to run at startup or not
            get
            {
                return getStartupRegistryKey().GetValue(REGISTRY_KEY_ID) != null;
            }
            set
            {
                if (value)
                {
                    // Add
                    getStartupRegistryKey(true).SetValue(REGISTRY_KEY_ID, Application.ExecutablePath.ToString());
                    //Console.WriteLine("RUN AT STARTUP SET AS => TRUE");
                }
                else
                {
                    // Remove
                    getStartupRegistryKey(true).DeleteValue(REGISTRY_KEY_ID, false);
                    //Console.WriteLine("RUN AT STARTUP SET AS => FALSE");
                }
            }
        }

        private RegistryKey getStartupRegistryKey(bool writable = false)
        {
            return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable);
        }


        private void connectToIPCServer()
        {
            try
            {
                tcpclient = new TcpClient();
                Console.WriteLine("Connecting.....");

                tcpclient.Connect(configMgr.config.serverAddress, Int32.Parse(configMgr.config.serverPort + ""));
                // use the ipaddress as in the server program

                Console.WriteLine("Connected");
                stm = tcpclient.GetStream();
                isConnectedIPCServer = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }
        
    }
}