using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Tcp
{
    public class Server : IDisposable
    {
        #region Properties
        /// <summary>
        /// The underlying <see cref="System.Net.Sockets.Socket" /> object that 
        /// the <see cref="Server" /> uses to receive and send data.
        /// </summary>
        private Socket Socket { set; get; }

        /// <summary>
        /// The implementation of the port number for the <see cref="Server" />.
        /// </summary>
        private int mPort;

        /// <summary>
        /// Sets the <see cref="Server" />'s port number to a value between 0
        /// and the highest port number available, 65535. Also gets the port 
        /// number for the <see cref="Server" />.
        /// </summary>
        public int Port
        {
            set
            {
                mPort = Math.Min(Math.Max(0, value), 65535);
            }
            get
            {
                return mPort;
            }
        }

        /// <summary>
        /// Gets the local <see cref="IPEndPoint" /> for the 
        /// <see cref="Socket" />.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (Socket != null &&
                    Socket.Connected)
                {
                    return (IPEndPoint)Socket.LocalEndPoint;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Whether the server is currently running or not.
        /// </summary>
        private bool bIsRunning { set; get; }

        /// <summary>
        /// A list of connected clients.
        /// </summary>
        public List<Socket> Clients { private set; get; }

        /// <summary>
        /// A thread dedicated to performing the listen loop.
        /// </summary>
        private Thread ListenForConnectionsThread { set; get; }

        /// <summary>
        /// A thread dedicated to managing the client connections.
        /// </summary>
        private Thread ManageConnectionsThread { set; get; }

        /// <summary>
        /// Signal for the listen thread that a connection was made.
        /// </summary>
        private ManualResetEvent ConnectionMadeSignal { set; get; }
        #endregion

        #region Constructor(s)
        /// <summary>
        /// Initializes a new instance of the <see cref="Server" /> class using 
        /// the specified port number.
        /// </summary>
        /// <param name="port">
        /// The value of the <see cref="Socket" />'s port.
        /// </param>
        public Server(int port)
        {
            Port = port;
            Socket = null;
            bIsRunning = false;
            Clients = new List<Socket>();
            ListenForConnectionsThread = null;
            ManageConnectionsThread = null;
            ConnectionMadeSignal = new ManualResetEvent(false);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Starts the <see cref="Server" />, allowing the specified max number 
        /// of clients to connect to it.
        /// </summary>
        /// <param name="MaxClients">
        /// The maximum allowed connections to the <see cref="Server" />.
        /// </param>
        public void Start(int MaxClients = 10)
        {
            try
            {
                // Create the socket
                if (Socket == null)
                {
                    // Call event

                    // Clear the client list
                    Clients.Clear();

                    //Create the socket
                    Socket = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Stream,
                                        ProtocolType.Tcp);

                    // Bind the socket to the port and start listening
                    Socket.Bind(new IPEndPoint(IPAddress.Any, Port));
                    Socket.Listen(MaxClients);
                }

                // Create the listen thread
                if (ListenForConnectionsThread == null)
                {
                    ListenForConnectionsThread = new Thread(ListenForConnections)
                    {
                        IsBackground = true
                    };
                }

                // Create the client manager thread
                if (ManageConnectionsThread == null)
                {
                    ManageConnectionsThread = new Thread(ManageConnections)
                    {
                        IsBackground = true
                    };
                }

                // Call server started event
                OnStarted();

                // Start the threads
                bIsRunning = true;
                ListenForConnectionsThread.Start();
                ManageConnectionsThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// The asynchronous callback for when a client connects to the 
        /// <see cref="Server" />.
        /// </summary>
        /// <param name="result">
        /// The resulting status of the Accept() operation.
        /// </param>
        private void AcceptCallback(IAsyncResult result)
        {
            try
            {
                lock (Clients)
                {
                    // Add the client's socket to the client list
                    Socket connectedClient = Socket.EndAccept(result);
                    Clients.Add(connectedClient);

                    // Start receiving from the client
                    Receive(Clients.Count - 1);

                    // Call client connected event
                    OnClientConnected(
                        (IPEndPoint) connectedClient.RemoteEndPoint);
                }

                // Set the signal
                ConnectionMadeSignal.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Send a message to all clients connected to a <see cref="Server" />.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendAll(string message)
        {
            SendAll(Encoding.ASCII.GetBytes(message));
        }

        /// <summary>
        /// Send data to all clients connected to a <see cref="Server" />.
        /// </summary>
        /// <param name="data">The data to send.</param>
        public void SendAll(IEnumerable<byte> data)
        {
            foreach(Socket client in Clients)
            {
                Send((IPEndPoint) client.RemoteEndPoint, data);
            }
        }

        /// <summary>
        /// Sends a message asynchronously to the specified 
        /// <see cref="System.Net.IPEndPoint"/>.
        /// </summary>
        /// <param name="clientEP">The client to send to's endpoint.</param>
        /// <param name="message">The message to send.</param>
        public void Send(IPEndPoint clientEP, string message)
        {
            Send(clientEP, Encoding.ASCII.GetBytes(message));
        }

        /// <summary>
        /// Sends data asynchronously to the specified 
        /// <see cref="System.Net.IPEndPoint"/>.
        /// </summary>
        /// <param name="clientEP">The client to send to's endpoint.</param>
        /// <param name="data">The data to send.</param>
        public void Send(IPEndPoint clientEP, IEnumerable<byte> data)
        {
            try
            {
                // Get the correct index (breaking 80 character limit for readability)
                int clientIndex = Clients.IndexOf(
                    Clients.Where(x => 
                        (((IPEndPoint)x.RemoteEndPoint).Address == clientEP.Address) &&
                        (((IPEndPoint)x.RemoteEndPoint).Port == clientEP.Port)).First());

                // Create the state object
                State state =
                    new State(
                        (IPEndPoint)Clients[clientIndex].LocalEndPoint,
                        (IPEndPoint)Clients[clientIndex].RemoteEndPoint,
                        data);
                        //Clients[clientIndex]);

                // Send the data
                Clients[clientIndex].BeginSend(
                    data.ToArray(), 0, data.ToArray().Length, 
                    SocketFlags.None, new AsyncCallback(SendCallback), state);
            }
            catch (SocketException)
            {
                // Expected when client disconnects
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// The asynchronous callback for when data is sent.
        /// </summary>
        /// <param name="result">
        /// The resulting status of the 
        /// <see cref="Send(IPEndPoint, IEnumerable{byte})" /> operation.
        /// </param>
        private void SendCallback(IAsyncResult result)
        {
            try
            {
                // Get the state
                State state = (State)result.AsyncState;

                // Get the clientEP
                IPEndPoint clientEP = state.RemoteEndPoint; 

                // Get the correct index (breaking 80 character limit for readability)
                int clientIndex = Clients.IndexOf(
                    Clients.Where(x =>
                        (((IPEndPoint)x.RemoteEndPoint).Address == clientEP.Address) &&
                        (((IPEndPoint)x.RemoteEndPoint).Port == clientEP.Port)).First());

                // Send the data
                int bytesSent = Clients[clientIndex].EndSend(result);

                // Call sent event
                OnSent(new TcpEventArgs(
                    (IPEndPoint)Clients[clientIndex].LocalEndPoint, 
                    (IPEndPoint)Clients[clientIndex].RemoteEndPoint, 
                    state.Data));
            }
            catch (SocketException)
            {
                // Expected when client disconnects
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Receives data asynchronously for the given socket.
        /// </summary>
        /// <param name="clientIndex">
        /// Index of the client socket to listen for.
        /// </param>
        private void Receive(int clientIndex)
        {
            try
            {
                // Receive data
                State state = 
                    new State(
                        (IPEndPoint)Clients[clientIndex].LocalEndPoint,
                        (IPEndPoint)Clients[clientIndex].RemoteEndPoint,
                        Clients[clientIndex]);

                Clients[clientIndex].BeginReceive(state.Buffer, 0, State.BufferSize,
                                    SocketFlags.None,
                                    new AsyncCallback(ReceiveCallback), state);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// The asynchronous callback for when data is received.
        /// </summary>
        /// <param name="result">
        /// The resulting status of the <see cref="Receive(ref Socket)" /> 
        /// operation.
        /// </param>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                // Get the state
                State state = (State)result.AsyncState;

                // Get the socket
                Socket socket = (Socket)state.Data;

                // Read received data
                int bytesReceived = socket.EndReceive(result);

                // Call event
                if (bytesReceived > 0)
                {
                    StringBuilder response =
                        new StringBuilder(
                            Encoding.ASCII.GetString(
                                state.Buffer, 0, bytesReceived));

                    OnReceived(
                        new TcpEventArgs(state.LocalEndPoint, state.RemoteEndPoint,
                                         response.ToString()));
                }

                // Receive data
                socket.BeginReceive(state.Buffer, 0, State.BufferSize,
                                    SocketFlags.None,
                                    new AsyncCallback(ReceiveCallback),
                                    state);
            }
            catch (ObjectDisposedException)
            {
                // Expected when client disconnects
            }
            catch (SocketException)
            {
                // Expected when client disconnects
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Listens for incoming connections.
        /// </summary>
        private void ListenForConnections()
        {
            while (bIsRunning)
            {
                // Reset the signal
                ConnectionMadeSignal.Reset();

                // Listen for a connection
                Socket.BeginAccept(
                    new AsyncCallback(AcceptCallback), null);

                // Wait for the signal
                ConnectionMadeSignal.WaitOne();
            }
        }
        
        /// <summary>
        /// Manages the connections in the <see cref="Clients" /> list.
        /// </summary>
        private void ManageConnections()
        {
            while (bIsRunning)
            {
                // Check if each client is still connected
                lock (Clients)
                {
                    foreach (Socket socket in Clients.ToArray())
                    {
                        if (!IsConnectionActive(socket))
                        {
                            // Call event
                            OnClientDisconnected(
                                (IPEndPoint) socket.RemoteEndPoint);

                            // Shutdown and close the socket
                            socket.Shutdown(SocketShutdown.Both);
                            socket.Close();

                            // Remove client from list
                            Clients.Remove(socket);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a connection still exists.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the connection is still alive. <c>false</c> otherwise.
        /// </returns>
        private bool IsConnectionActive(in Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the <see cref="Server" /> has started.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Responds to the <see cref="Started" /> event.
        /// </summary>
        protected virtual void OnStarted()
        {
            EventHandler handler = Started;
            handler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when a client connects to the <see cref="Server" />.
        /// </summary>
        public event EventHandler ClientConnected;

        /// <summary>
        /// Responds to the <see cref="ClientConnected" /> event.
        /// </summary>
        protected virtual void OnClientConnected(IPEndPoint clientEP)
        {
            EventHandler handler = ClientConnected;
            handler?.Invoke(this,
                new ConnectionEventArgs(
                    LocalEndPoint,
                    clientEP,
                    ConnectionStatus.Connected));
        }

        /// <summary>
        /// Occurs when a client disconnects to the <see cref="Server" />.
        /// </summary>
        public event EventHandler ClientDisconnected;

        /// <summary>
        /// Responds to the <see cref="ClientDisconnected" /> event.
        /// </summary>
        protected virtual void OnClientDisconnected(IPEndPoint clientEP)
        {
            EventHandler handler = ClientDisconnected;
            handler?.Invoke(this,
                new ConnectionEventArgs(
                    LocalEndPoint,
                    clientEP,
                    ConnectionStatus.Disconnected));
        }
        
        /// <summary>
        /// Occurs when a <see cref="Server" /> receives data.
        /// </summary>
        public event EventHandler Received;

        /// <summary>
        /// Responds to the <see cref="Received" /> event.
        /// </summary>
        /// <param name="args">
        /// The event data for the <see cref="Received" /> event.
        /// </param>
        protected virtual void OnReceived(TcpEventArgs args)
        {
            EventHandler handler = Received;
            handler?.Invoke(this, args);
        }

        /// <summary>
        /// Occurs when a <see cref="Server" /> sends data.
        /// </summary>
        public event EventHandler Sent;

        /// <summary>
        /// Responds to the <see cref="Sent" /> event.
        /// </summary>
        /// <param name="args">
        /// The event data for the <see cref="Sent" /> event.
        /// </param>
        protected virtual void OnSent(TcpEventArgs args)
        {
            EventHandler handler = Sent;
            handler?.Invoke(this, args);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (Socket != null)
                    {
                        Socket.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Server() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
