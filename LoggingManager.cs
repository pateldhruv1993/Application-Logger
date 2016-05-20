﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace ApplicationLogger
{
    class LoggingManager
    {
        MainForm mainForm;
        ConfigManager configMgr;



        private const string LINE_DIVIDER = "\t";
        private const string LINE_END = "\r\n";
        private const string DATE_TIME_FORMAT = "o";							            // 2008-06-15T21:15:07.0000000



        private string lastUserProcessId = null;
        private string newUserProcessId;
        private int lastDayLineLogged;
        private string lastFileNameSaved = "";
        private DateTime lastTimeQueueWritten = DateTime.Now;


        private StringBuilder lineToLog = new StringBuilder();								// Temp, used to create the line
        public List<string> queuedLogMessages = new List<string>();
        public FixedSizedQueue<string> fixedSizeQueue;



        public LoggingManager(ConfigManager cM, MainForm main)
        {
            configMgr = cM;
            mainForm = main;
        }


        public void logUserIdle()
        {
            // Log that the user is idle
            logLine("status::idle", true, false, configMgr.config.idleTime ?? 0);
            mainForm.updateText("User idle");
            lastUserProcessId = null;
            newUserProcessId = null;
        }

        public void logStop()
        {
            // Log stopping the application
            logLine("status::stop", true);
            mainForm.updateText("Stopped");
            newUserProcessId = null;
        }

        private void logEndOfDay()
        {
            // Log an app focus change after the end of the day, and at the end of the specific log file
            logLine("status::end-of-day", true, true);
            newUserProcessId = null;
        }

        public void logUserProcess(Process process)
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
                mainForm.updateText("Name: " + process.ProcessName + ", " + process.MainWindowTitle);
            }
            catch (Exception exception)
            {
                logLine("app::focus", process.ProcessName, "?", "?");
                mainForm.updateText("Name: " + process.ProcessName + ", ?");
            }
        }

        public void logLine(string type, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0)
        {
            logLine(type, "", "", "", forceCommit, usePreviousDayFileName, idleTimeOffsetSeconds);
        }

        public void logLine(string type, string title, string location, string subject, bool forceCommit = false, bool usePreviousDayFileName = false, float idleTimeOffsetSeconds = 0)
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

            if (queuedLogMessages.Count > configMgr.config.maxQueueEntries || forceCommit)
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

        public void commitLines(string fileName = null)
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

                mainForm.updateContextMenu();
            }
        }


        public string getLogFileName()
        {
            // Get the log filename for something to be logged now
            var now = DateTime.Now;
            var filename = configMgr.config.processPath;

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




        public void checkForNewProcess()
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


        public void checkIfShouldCommit(){
            if (queuedLogMessages.Count > 0 && (DateTime.Now - lastTimeQueueWritten).TotalSeconds > configMgr.config.maxQueueTime)
            {
                commitLines();
            }
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
