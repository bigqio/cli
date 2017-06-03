using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BigQ;
using CommonMethods;

namespace BigQClientCLI
{
    class ClientCLI
    {
        static string configFile;
        static Dictionary<string, object> configFileContents;
        static Client client = null;
        static Message response;
        static List<Client> users;
        
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

                ConnectToServer();
                bool runForever = true;
                while (runForever)
                {
                    if (client == null) Console.Write("[OFFLINE] ");
                    Console.Write("Command [? for help]: ");

                    string input = Console.ReadLine();
                    if (String.IsNullOrEmpty(input)) continue;

                    if (input.StartsWith("/"))
                    {
                        if (client == null) continue;

                        string msg = input.Substring(1);
                        string recipient = "";
                        string recipientMessage = "";

                        int currPosition = 0;
                        while (true)
                        {
                            if (currPosition >= msg.Length) break;
                            if (msg[currPosition] != ' ')
                            {
                                recipient += msg[currPosition];
                                currPosition++;
                            }
                            else
                            {
                                currPosition++;
                                break;
                            }
                        }

                        recipientMessage = input.Substring(currPosition).Trim();

                        if (String.IsNullOrEmpty(recipient))
                        {
                            Console.WriteLine("*** No recipient specified");
                            continue;
                        }

                        if (String.IsNullOrEmpty(recipientMessage))
                        {
                            Console.WriteLine("*** No message specified");
                            continue;
                        }

                        if (!client.SendPrivateMessageAsync(recipient, recipientMessage))
                        {
                            Console.WriteLine("*** Unable to send message to " + recipient);
                        }

                        continue;
                    }
                    else
                    {
                        switch (input.ToLower())
                        {
                            case "?":
                                Console.WriteLine("");
                                Console.WriteLine("Available Commands:");
                                Console.WriteLine("  q                  quit");
                                Console.WriteLine("  cls                clear the screen");
                                Console.WriteLine("  whoami             show my TCP endpoint");
                                Console.WriteLine("  who                list all connected users");
                                Console.WriteLine("  debug              enable/disable console debugging (currently " + client.Config.Logging.ConsoleLogging + ")");
                                Console.WriteLine("  /(handle) (msg)    send message (msg) to user with handle (handle)");
                                Console.WriteLine("                     leave parentheses off for both handle and message data");
                                Console.WriteLine("");
                                break;

                            case "q":
                            case "quit":
                                runForever = false;
                                break;

                            case "c":
                            case "cls":
                                Console.Clear();
                                break;

                            case "login":
                                if (client == null) break;
                                if (!client.Login(out response))
                                {
                                    Console.WriteLine("*** Login failed");
                                }
                                else
                                {
                                    Console.WriteLine("Login succeeded");
                                }
                                break;

                            case "whoami":
                                if (client == null) break;
                                Console.Write(client.IpPort);
                                if (!String.IsNullOrEmpty(client.ClientGUID)) Console.WriteLine("  GUID " + client.ClientGUID);
                                else Console.WriteLine("[not logged in]");
                                break;

                            case "who":
                                if (client == null) break;
                                if (!client.ListClients(out response, out users))
                                {
                                    Console.WriteLine("*** Unable to retrieve user list");
                                }
                                else
                                {
                                    if (users == null || users.Count < 1)
                                    {
                                        Console.WriteLine("*** Null or empty user list retrieved");
                                    }
                                    else
                                    {
                                        List<Client> deduped = users.Distinct().ToList();

                                        Console.WriteLine("Connected users:");
                                        foreach (Client curr in deduped)
                                        {
                                            Console.WriteLine("  " + curr.IpPort + "  " + curr.ClientGUID + "  " + curr.Email);
                                        }
                                    }
                                }
                                break;

                            case "debug":
                                client.Config.Logging.ConsoleLogging = !client.Config.Logging.ConsoleLogging;
                                break;

                            default:
                                Console.WriteLine("Unknown command");
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Common.PrintException("BigQServerCLI", "Main", e);
            }
        }
        
        static bool AsyncMessageReceived(Message msg)
        {
            if (msg == null) return false;
            if (msg.Data == null) return false;
            int len = 0;
            if (msg.Data != null && msg.Data.Length > 0) len = (msg.Data.Length);
            Console.WriteLine(msg.SenderGUID + " -> " + msg.RecipientGUID + " async (" + len + " bytes): " + Encoding.UTF8.GetString(msg.Data));
            return true;
        }
        
        static bool ClientJoinedServer(string clientGuid)
        {
            Console.WriteLine("Client " + clientGuid + " joined the server");
            return true;
        }

        static bool ClientLeftServer(string clientGuid)
        {
            Console.WriteLine("Client " + clientGuid + " left the server");
            return true;
        }

        static bool ServerConnected()
        {
            Console.WriteLine("Server connected");
            return true;
        }

        static bool ConnectToServer()
        {
            while (true)
            {
                Stopwatch connectSw = new Stopwatch();
                long connectSwMs = 0;
                Stopwatch loginSw = new Stopwatch();
                long loginSwMs = 0;

                try
                {
                    if (client != null)
                    {
                        Console.WriteLine("Attempting to close client");
                        client.Dispose();
                    }

                    Console.WriteLine("Attempting to connect to server");
                    client = null;

                    connectSw.Start();
                    client = new Client(configFile);
                    connectSw.Stop();
                    connectSwMs = connectSw.ElapsedMilliseconds;

                    client.AsyncMessageReceived = AsyncMessageReceived;
                    client.SyncMessageReceived = null;
                    client.ServerDisconnected = ConnectToServer;
                    client.ServerConnected = ServerConnected;
                    client.ClientJoinedServer = ClientJoinedServer;
                    client.ClientLeftServer = ClientLeftServer;
                    client.ClientJoinedChannel = null;          // not implemented in this app
                    client.ClientLeftChannel = null;            // not implemented in this app
                    client.SubscriberJoinedChannel = null;      // not implemented in this app
                    client.SubscriberLeftChannel = null;        // not implemented in this app
                    // client.LogMessage = LogMessage;

                    Console.WriteLine("Successfully connected to server");

                    Console.WriteLine("Attempting login");
                    Message response;

                    loginSw.Start();
                    if (!client.Login(out response))
                    {
                        Console.WriteLine("Unable to login, retrying in five seconds");
                        Thread.Sleep(5000);
                        return ConnectToServer();
                    }
                    loginSw.Stop();
                    loginSwMs = loginSw.ElapsedMilliseconds;

                    Console.WriteLine("Successfully logged into server (" + loginSwMs + "ms)");
                    return true;
                }
                catch (SocketException)
                {
                    Console.WriteLine("*** Unable to connect to server (port not reachable)");
                    Console.WriteLine("*** Retrying in five seconds");
                    Thread.Sleep(5000);
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("*** Timeout connecting to server");
                    Console.WriteLine("*** Retrying in five seconds");
                    Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine("*** Unable to connect to server due to the following exception:");
                    Common.PrintException("BigQClientCLI", "ConnectToSerer", e);
                    Console.WriteLine("*** Retrying in five seconds");
                    Thread.Sleep(5000);
                }
            }
        }

        static bool LogMessage(string msg)
        {
            Console.WriteLine("BigQClient message: " + msg);
            return true;
        }

        static void Welcome()
        {
            Console.WriteLine("BigQ Client Version " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
        }

        static void Usage()
        {
            Console.WriteLine("");
            Console.WriteLine("Usage: C:\\> bigq-client -f=<configfile>");
            Console.WriteLine("Refer to client1.json or client2.json for a sample config.");
        }
    }
}
