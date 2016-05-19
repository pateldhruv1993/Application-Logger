using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationLogger
{
    class DataManager
    {

        private const string CONFIG_FILE = "ApplicationLogger.cfg";


        public struct configDataStruct{
            public string processPath;
            public float? idleTime;												// In seconds
            public float? timeCheckInterval;										// In seconds
            public float? maxQueueTime;
            public int? maxQueueEntries;
            public int? maxLogCache;
            public int? TCPInterval;
            public int? maxTCPAttempts;
            public string serverAddress;
            public int? serverPort;
        }

        public configDataStruct config = new configDataStruct();


        public void readConfiguration()
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
            config.processPath = configUser.getString("path") ?? configDefault.getString("path");
            config.idleTime = configUser.getFloat("idleTime") ?? configDefault.getFloat("idleTime");
            config.timeCheckInterval = configUser.getFloat("checkInterval") ?? configDefault.getFloat("checkInterval");
            config.maxQueueEntries = configUser.getInt("maxQueueEntries") ?? configDefault.getInt("maxQueueEntries");
            config.maxQueueTime = configUser.getFloat("maxQueueTime") ?? configDefault.getFloat("maxQueueTime");
            config.TCPInterval = configUser.getInt("TCPInterval") ?? configDefault.getInt("TCPInterval");
            config.maxTCPAttempts = configUser.getInt("maxTCPAttempts") ?? configDefault.getInt("maxTCPAttempts");
            config.serverAddress = configUser.getString("serverAddress") ?? configDefault.getString("serverAddress");
            config.serverPort = configUser.getInt("serverPort") ?? configDefault.getInt("serverPort");
            config.maxLogCache = configUser.getInt("maxLogCache") ?? configDefault.getInt("maxLogCache");
        }
    }
}
