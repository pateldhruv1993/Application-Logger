using ApplicationLogger.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
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

        /*
         * TODO:
         * . Count lines in UI?
         * . Allow opening current log file in context menu
         * . Allow app change ignoring on regex?
         * . Create analyzer
         * . ignore private windows? http://stackoverflow.com/questions/14132142/using-c-sharp-to-close-google-chrome-incognito-windows-only
         */

        // Constants
        private const string SETTINGS_FIELD_RUN_AT_STARTUP = "RunAtStartup";
        private const string REGISTRY_KEY_ID = "ApplicationLogger";					// Registry app key for when it's running at startup
        private const string CONFIG_FILE = "ApplicationLogger.cfg";

        private const string LINE_DIVIDER = "\t";
        private const string LINE_END = "\r\n";
        private const string DATE_TIME_FORMAT = "o";								// 2008-06-15T21:15:07.0000000

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
        private string lastUserProcessId;
        private string lastFileNameSaved;
        private int lastDayLineLogged;
        private DateTime lastTimeQueueWritten;
        private List<string> queuedLogMessages;
        private FixedSizedQueue<string> fixedSizeQueue;

        private string configPath;
        private float? configIdleTime;												// In seconds
        private float? configTimeCheckInterval;										// In seconds
        private float? configMaxQueueTime;
        private int? configMaxQueueEntries;
        private int? configMaxLogCache;
        private int? configTCPInterval;
        private int? configMaxTCPAttempts;
        private string configServerAddress;
        private int? configServerPort;

        private string newUserProcessId;											// Temp
        private StringBuilder lineToLog;											// Temp, used to create the line


        //Variables for ICP with Node App
        TcpClient tcpclient;
        Stream stm;
        ASCIIEncoding asen = new ASCIIEncoding();



        // ================================================================================================================
        // CONSTRUCTOR ----------------------------------------------------------------------------------------------------

        public MainForm()
        {
            InitializeComponent();
            initializeForm();
        }


        // ================================================================================================================
        // EVENT INTERFACE ------------------------------------------------------------------------------------------------

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
            if (SystemHelper.GetIdleTime() >= configIdleTime * 1000f)
            {
                if (!isUserIdle)
                {
                    // User is now idle
                    isUserIdle = true;
                    lastUserProcessId = null;
                    logUserIdle();
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
                var process = getCurrentUserProcess();
                if (process != null)
                {
                    // Valid process, create a unique id
                    newUserProcessId = process.ProcessName + "_" + process.MainWindowTitle;

                    if (lastUserProcessId != newUserProcessId)
                    {
                        // New process
                        logUserProcess(process);
                        lastUserProcessId = newUserProcessId;
                    }
                }
            }






            //TCP Client Stuff
            try
            {

                byte[] bb = new byte[100];
                int k = stm.Read(bb, 0, 100);

                if (k != 0 || k != null)
                {
                    String receivedData = System.Text.Encoding.Default.GetString(bb);



                    byte[] ba = asen.GetBytes("Say it don't spray it Ron.");
                    stm.Write(ba, 0, ba.Length);
                }
            }
            catch (Exception excep)
            {
                Console.WriteLine("Error..... " + excep.StackTrace);
            }







            // Write to log if enough time passed
            if (queuedLogMessages.Count > 0 && (DateTime.Now - lastTimeQueueWritten).TotalSeconds > configMaxQueueTime)
            {
                commitLines();
            }
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
            commitLines();
            Process.Start(getLogFileName());
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


        // ================================================================================================================
        // INTERNAL INTERFACE ---------------------------------------------------------------------------------------------

        private void initializeForm()
        {
            // Initialize

            if (!hasInitialized)
            {
                allowClose = false;
                isRunning = false;
                queuedLogMessages = new List<string>();
                lineToLog = new StringBuilder();
                lastFileNameSaved = "";
                allowShow = false;

                // Force working folder
                System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                // Read configuration
                readConfiguration();

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

        private void updateContextMenu()
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
                var filename = getLogFileName();
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

        private void readConfiguration()
        {
            // Read the current configuration file

            // Read default file
            ConfigParser configDefault = new ConfigParser(ApplicationLogger.Properties.Resources.default_config);
            ConfigParser configUser;

            if (!System.IO.File.Exists(CONFIG_FILE))
            {
                // Config file not found, create it first
                Console.Write("Config file does not exist, creating");

                // Write file so it can be edited by the user
                System.IO.File.WriteAllText(CONFIG_FILE, ApplicationLogger.Properties.Resources.default_config);

                // User config is the same as the default
                configUser = configDefault;
            }
            else
            {
                // Read the existing user config
                configUser = new ConfigParser(System.IO.File.ReadAllText(CONFIG_FILE));
            }

            // Interprets config data
            configPath =                    configUser.getString("path") ?? configDefault.getString("path");
            configIdleTime =                configUser.getFloat("idleTime") ?? configDefault.getFloat("idleTime");
            configTimeCheckInterval =       configUser.getFloat("checkInterval") ?? configDefault.getFloat("checkInterval");
            configMaxQueueEntries =         configUser.getInt("maxQueueEntries") ?? configDefault.getInt("maxQueueEntries");
            configMaxQueueTime =            configUser.getFloat("maxQueueTime") ?? configDefault.getFloat("maxQueueTime");
            configTCPInterval =             configUser.getInt("TCPInterval") ?? configDefault.getInt("TCPInterval");
            configMaxTCPAttempts =          configUser.getInt("maxTCPAttempts") ?? configDefault.getInt("maxTCPAttempts");
            configServerAddress =           configUser.getString("serverAddress") ?? configDefault.getString("serverAddress");
            configServerPort =              configUser.getInt("serverPort") ?? configDefault.getInt("serverPort");
            configMaxLogCache =             configUser.getInt("maxLogCache") ?? configDefault.getInt("maxLogCache");
        }

        private void start()
        {
            if (!isRunning)
            {
                // Initialize timer
                timerCheck = new Timer();
                timerCheck.Tick += new EventHandler(onTimer);
                timerCheck.Interval = (int)(configTimeCheckInterval * 1000f);
                timerCheck.Start();
                lastUserProcessId = null;
                lastTimeQueueWritten = DateTime.Now;
                isRunning = true;
                fixedSizeQueue = new FixedSizedQueue<string>(Int32.Parse(configMaxLogCache +""));


                //Log system start (Not really. This just means app started. BUT as I plan to run it on startup, this should be good)
                logLine("status::start");




                try
                {
                    tcpclient = new TcpClient();
                    Console.WriteLine("Connecting.....");

                    tcpclient.Connect(configServerAddress, Int32.Parse(configServerPort + ""));
                    // use the ipaddress as in the server program

                    Console.WriteLine("Connected");
                    stm = tcpclient.GetStream();

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error..... " + e.StackTrace);
                }


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

                
                logStop();

                timerCheck.Stop();
                timerCheck.Dispose();
                timerCheck = null;

                isRunning = false;

                updateContextMenu();
                updateTrayIcon();


            }
        }

        private void logUserIdle()
        {
            // Log that the user is idle
            logLine("status::idle", true, false, configIdleTime ?? 0);
            updateText("User idle");
            newUserProcessId = null;
        }

        private void logStop()
        {
            // Log stopping the application
            logLine("status::stop", true);
            updateText("Stopped");
            newUserProcessId = null;
        }

        private void logEndOfDay()
        {
            // Log an app focus change after the end of the day, and at the end of the specific log file
            logLine("status::end-of-day", true, true);
            newUserProcessId = null;
        }

        private void logUserProcess(Process process)
        {
            // Log the current user process

            int dayOfLog = DateTime.Now.Day;

            if (dayOfLog != lastDayLineLogged)
            {
                // The last line was logged on a different day, so check if it should be a new file
                string newFileName = getLogFileName();

                if (newFileName != lastFileNameSaved && lastFileNameSaved != "")
                {
                    // It's a new file: commit current with an end-of-day event
                    logEndOfDay();
                }
            }



            try
            {
                logLine("app::focus", process.ProcessName, process.MainModule.FileName, process.MainWindowTitle);
                updateText("Name: " + process.ProcessName + ", " + process.MainWindowTitle);
            }
            catch (Exception exception)
            {
                logLine("app::focus", process.ProcessName, "?", "?");
                updateText("Name: " + process.ProcessName + ", ?");
            }
        }

        private void logLine(string type, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0)
        {
            logLine(type, "", "", "", forceCommit, usePreviousDayFileName, idleTimeOffsetSeconds);
        }

        private void logLine(string type, string title, string location, string subject, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0)
        {
            // Log a single line
            DateTime now = DateTime.Now;

            now.AddSeconds(idleTimeOffsetSeconds);

            lineToLog.Clear();
            lineToLog.Append(now.ToString(DATE_TIME_FORMAT));
            lineToLog.Append(LINE_DIVIDER);
            lineToLog.Append(type);
            lineToLog.Append(LINE_DIVIDER);
            lineToLog.Append(Environment.MachineName);
            lineToLog.Append(LINE_DIVIDER);
            lineToLog.Append(title);
            lineToLog.Append(LINE_DIVIDER);
            lineToLog.Append(location);
            lineToLog.Append(LINE_DIVIDER);
            lineToLog.Append(subject);
            lineToLog.Append(LINE_END);

            //Console.Write("LOG ==> " + lineToLog.ToString());

            queuedLogMessages.Add(lineToLog.ToString());
            fixedSizeQueue.Enqueue(lineToLog.ToString());

            lastDayLineLogged = DateTime.Now.Day;

            if (queuedLogMessages.Count > configMaxQueueEntries || forceCommit)
            {
                if (usePreviousDayFileName)
                {
                    commitLines(lastFileNameSaved);
                }
                else
                {
                    commitLines();
                }
            }
        }

        private void commitLines(string fileName = null)
        {
            // Commit all currently queued lines to the file

            // If no commit needed, just return
            if (queuedLogMessages.Count == 0) return;

            lineToLog.Clear();
            foreach (var line in queuedLogMessages)
            {
                lineToLog.Append(line);
            }

            string commitFileName = fileName ?? getLogFileName();
            bool saved = false;

            // Check if the path exists, creating it otherwise
            string filePath = System.IO.Path.GetDirectoryName(commitFileName);
            if (filePath.Length > 0 && !System.IO.Directory.Exists(filePath))
            {
                System.IO.Directory.CreateDirectory(filePath);
            }

            try
            {
                System.IO.File.AppendAllText(commitFileName, lineToLog.ToString());
                saved = true;
            }
            catch (Exception exception)
            {
            }

            if (saved)
            {
                // Saved successfully, now clear the queue
                queuedLogMessages.Clear();

                lastTimeQueueWritten = DateTime.Now;

                updateContextMenu();
            }
        }

        private void updateText(string text)
        {
            labelApplication.Text = text;
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


        // ================================================================================================================
        // ACCESSOR INTERFACE ---------------------------------------------------------------------------------------------

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

        private Process getCurrentUserProcess()
        {
            // Find the process that's currently on top
            var processes = Process.GetProcesses();
            var foregroundWindowHandle = SystemHelper.GetForegroundWindow();

            foreach (var process in processes)
            {
                if (process.Id <= 4) { continue; } // system processes
                if (process.MainWindowHandle == foregroundWindowHandle) return process;
            }

            // Nothing found!
            return null;
        }




        private string getLogFileName()
        {
            // Get the log filename for something to be logged now
            var now = DateTime.Now;
            var filename = configPath;

            // Replaces variables
            filename = filename.Replace("[[month]]", now.ToString("MM"));
            filename = filename.Replace("[[day]]", now.ToString("dd"));
            filename = filename.Replace("[[year]]", now.ToString("yyyy"));
            filename = filename.Replace("[[machine]]", Environment.MachineName);

            var pathOnly = System.IO.Path.GetDirectoryName(filename);
            var fileOnly = System.IO.Path.GetFileName(filename);

            // Make it safe
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileOnly = fileOnly.Replace(c, '_');
            }

            return (pathOnly.Length > 0 ? pathOnly + "\\" : "") + fileOnly;
        }
    }




    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        public int Size { get; private set; }

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (base.Count > Size)
                {
                    T outObj;
                    base.TryDequeue(out outObj);
                }
            }
        }
    }

}
