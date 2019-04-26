using System;
using System.Text;

namespace Tcp
{
    internal class Program
    {
        // Client example
        private static bool IsRunning = true;

        private static void Main(string[] args)
        {
            Server server = new Server(4321);
            server.Received += Server_Received;
            server.Sent += Server_Sent;
            server.Start();
            /*
            Client client = new Client(1234);
            client.ConnectionChanged += Client_ConnectionChanged;
            client.Received += Client_Received;
            client.Sent += Client_Sent;
            client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4321));
            */

            // Listen loop
            while (IsRunning) ;
            
            Console.Read();
        }

        private static void Server_Sent(object sender, EventArgs e)
        {
            TcpEventArgs args = (TcpEventArgs)e;
            if (args != null)
            {
                Console.WriteLine(string.Format("{0} sent \"{1}\" to {2}", args.LocalEndPoint, Encoding.ASCII.GetString((byte[])args.Data), args.RemoteEndPoint));
            }
        }

        private static void Server_Received(object sender, EventArgs e)
        {
            TcpEventArgs args = (TcpEventArgs)e;
            if (args != null)
            {
                Console.WriteLine(string.Format("Server received \"{0}\" from {1}", args.Data, args.RemoteEndPoint));

                if (args.Data.Equals("send nudes"))
                {
                    Server server = (Server)sender;
                    server?.Send(args.RemoteEndPoint, "nudes");
                }
                else if (args.Data.Equals("send nudes to everyone"))
                {
                    Server server = (Server)sender;
                    server?.SendAll("nudes");
                }
            }
        }

        private static void Client_Sent(object sender, EventArgs e)
        {
            TcpEventArgs args = (TcpEventArgs)e;
            if (args != null)
            {
                Console.WriteLine(string.Format("{0} sent \"{1}\" to {2}", args.LocalEndPoint, Encoding.ASCII.GetString((byte[])args.Data), args.RemoteEndPoint));
            }
        }

        private static void Client_ConnectionChanged(object sender, EventArgs e)
        {
            ConnectionEventArgs args = (ConnectionEventArgs)e;
            if (args != null)
            {
                if (args.LocalEndPoint == null && args.RemoteEndPoint == null)
                {
                    Console.WriteLine(string.Format("{0}", args.Status));
                }
                else if (args.LocalEndPoint == null)
                {
                    Console.WriteLine(string.Format("{0} {1}", args.Status, args.RemoteEndPoint));
                }
                else if (args.RemoteEndPoint == null)
                {
                    Console.WriteLine(string.Format("{0} {1}", args.LocalEndPoint, args.Status));
                }
                else
                {
                    Console.WriteLine(string.Format("{0} {1} {2}", args.LocalEndPoint, args.Status, args.RemoteEndPoint));
                }
            }
        }

        private static void Client_Received(object sender, EventArgs e)
        {
            TcpEventArgs args = (TcpEventArgs)e;
            if (args != null)
            {
                Console.WriteLine(string.Format("{0} received \"{1}\" from {2}", args.LocalEndPoint, args.Data, args.RemoteEndPoint));

                if (args.Data.Equals("disconnect"))
                {
                    Client client = (Client)sender;
                    client?.Disconnect();
                    IsRunning = false;
                }
                else if (args.Data.Equals("hello"))
                {
                    Client client = (Client)sender;
                    client?.Send("hi!");
                }
            }
        }
    }
}
