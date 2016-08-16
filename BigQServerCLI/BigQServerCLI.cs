using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BigQ;
using CommonMethods;

namespace BigQServerCLI
{
    class ServerCLI
    {
        static string configFile;
        static Dictionary<string, object> configFileContents;
        static Server server = null;
        static List<Client> clients;
        
        static void Main(string[] args)
        {
            try
            {
                #region Parse-Arguments

                Welcome();
                if (args == null || args.Length != 1)
                {
                    Usage();
                    return;
                }

                configFile = args[0].Substring(3);
                configFileContents = Common.FileToDictionary(configFile);

                #endregion

                StartServer();
                bool runForever = true;
                while (runForever)
                {
                    Console.Write("Command [? for help] > ");
                    string input = Console.ReadLine();
                    if (String.IsNullOrEmpty(input)) continue;

                    switch (input.ToLower().Trim())
                    {
                        case "?":
                            Console.WriteLine("-------------------------------------------------------------------------------");
                            Console.WriteLine("Menu");
                            Console.WriteLine("  q       quit");
                            Console.WriteLine("  cls     clear screen");
                            Console.WriteLine("  who     list connected users");
                            Console.WriteLine("  count   show server connection count");
                            Console.WriteLine("");
                            break;

                        case "q":
                            runForever = false;
                            break;

                        case "c":
                        case "cls":
                            Console.Clear();
                            break;

                        case "who":
                            clients = server.ListClients();
                            if (clients == null) Console.WriteLine("(null)");
                            else if (clients.Count < 1) Console.WriteLine("(empty)");
                            else
                            {
                                Console.WriteLine(clients.Count + " clients connected");
                                foreach (Client curr in clients)
                                {
                                    Console.WriteLine("  " + curr.IpPort() + "  " + curr.ClientGUID + "  " + curr.Email);
                                }
                            }
                            break;

                        case "count":
                            Console.WriteLine("Server connection count: " + server.ConnectionCount());
                            break;
                            
                        default:
                            break;
                    }
                }

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Common.PrintException("BigQServerCLI", "Main", e);
            }
        }

        static bool MessageReceived(Message msg)
        {
            // Console.WriteLine(msg.SenderGUID + " -> " + msg.RecipientGUID + ": " + Encoding.UTF8.GetString(msg.Data));
            return true;
        }
        
        static bool StartServer()
        {
            while (true)
            {
                try
                {
                    if (server != null)
                    {
                        Console.WriteLine("Attempting to restart server");
                        server.Close();
                        server = null;
                    }

                    Console.WriteLine("Attempting to start server");

                    server = new Server(configFile);
                    server.MessageReceived = MessageReceived;
                    server.ServerStopped = StartServer;
                    server.ClientConnected = ClientConnected;
                    server.ClientLogin = ClientLogin;
                    server.ClientDisconnected = ClientDisconnected;
                    server.LogMessage = LogMessage;
                    server.LogMessage = null;

                    Console.WriteLine("Server started");

                    return true;
                }
                catch (Exception EOuter)
                {
                    Console.WriteLine("*** Exception while attempting to start server: " + EOuter.Message);
                    Console.WriteLine("*** Retrying in five seconds");
                    Thread.Sleep(5000);
                }
            }
        }

        static bool ClientConnected(Client client)
        {
            // client connected
            Console.WriteLine(client.IpPort() + " connected");
            return true;
        }

        static bool ClientLogin(Client client)
        {
            // client login
            Console.WriteLine(client.IpPort() + " login [" + client.ClientGUID + "]");
            return true;
        }

        static bool ClientDisconnected(Client client)
        {
            // client disconnected
            Console.WriteLine("*** Disconnected: " + client.IpPort() + " " + client.ClientGUID);
            return true;
        }

        static bool LogMessage(string msg)
        {
            Console.WriteLine("Log message: " + msg);
            return true;
        }

        static void Welcome()
        {
            Console.WriteLine("BigQ Server Version " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
        }

        static void Usage()
        {
            Console.WriteLine("");
            Console.WriteLine("Usage: C:\\> bigq-server -f=<configfile>");
            Console.WriteLine("Refer to server.json for a sample config.");
        }
    }
}
