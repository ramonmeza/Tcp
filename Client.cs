using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Tcp
{
    /// <summary>
    /// A TCP client which can connect to a server to send and receive data.
    /// </summary>
    public class Client : IDisposable
    {
        #region Properties
        /// <summary>
        /// The underlying <see cref="System.Net.Sockets.Socket" /> object that 
        /// the <see cref="Client" /> uses to receive and send data.
        /// </summary>
        private Socket Socket { set; get; }

        /// <summary>
        /// The implementation of the port number for the <see cref="Client" />.
        /// </summary>
        private int mPort;

        /// <summary>
        /// Sets the <see cref="Client" />'s port number to a value between 0
        /// and the highest port number available, 65535. Also gets the port 
        /// number for the <see cref="Client" />.
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
        /// Gets the remote <see cref="IPEndPoint" /> for the 
        /// <see cref="Socket" />.
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                if (Socket != null &&
                    Socket.Connected)
                {
                    return (IPEndPoint)Socket.RemoteEndPoint;
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion

        #region Constructor(s)
        /// <summary>
        /// Initializes a new instance of the <see cref="Client" /> class using 
        /// the specified port number.
        /// </summary>
        /// <param name="port">
        /// The value of the <see cref="Socket" />'s port.
        /// </param>
        public Client(int port)
        {
            Port = port;
            Socket = null;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Connects the <see cref="Client" /> asynchronously to the specified 
        /// <see cref="IPEndPoint" />.
        /// </summary>
        /// <param name="serverEP">
        /// The <see cref="IPEndPoint" /> of the server to connect to.
        /// </param>
        public void Connect(IPEndPoint serverEP)
        {
            try
            {
                if (Socket == null)
                {
                    // Call event
                    OnConnectionChanged(
                        new ConnectionEventArgs(
                            LocalEndPoint, serverEP,
                            ConnectionStatus.Connecting));

                    // Create the socket
                    Socket = new Socket(AddressFamily.InterNetwork,
                                        SocketType.Stream,
                                        ProtocolType.Tcp);

                    // Bind the socket to the port
                    Socket.Bind(new IPEndPoint(IPAddress.Any, Port));

                    // Connect to the server
                    Socket.BeginConnect(serverEP,
                        new AsyncCallback(ConnectCallback),
                        new State(LocalEndPoint, serverEP));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// The asynchronous callback for when a connection is made.
        /// </summary>
        /// <param name="result">
        /// The resulting status of the <see cref="Connect(IPEndPoint)" /> 
        /// operation.
        /// </param>
        private void ConnectCallback(IAsyncResult result)
        {
            try
            {
                // Get the state
                State state = (State)result.AsyncState;

                // Complete the connection
                Socket.EndConnect(result);

                // Call event
                OnConnectionChanged(
                    new ConnectionEventArgs(
                        state.LocalEndPoint,
                        state.RemoteEndPoint,
                        ConnectionStatus.Connected));

                // Receive
                Receive();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Receives data asynchronously from the <see cref="RemoteEndPoint" />.
        /// </summary>
        private void Receive()
        {
            try
            {
                // Receive data
                State state = new State(LocalEndPoint, RemoteEndPoint);

                Socket.BeginReceive(state.Buffer, 0, State.BufferSize,
                                    SocketFlags.None,
                                    new AsyncCallback(ReceiveCallback), state);
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
        /// The asynchronous callback for when data is received.
        /// </summary>
        /// <param name="result">
        /// The resulting status of the <see cref="Receive()" /> operation.
        /// </param>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                // Get the state
                State state = (State)result.AsyncState;

                // Read received data
                int bytesReceived = Socket.EndReceive(result);

                // Call event
                if (bytesReceived > 0)
                {
                    StringBuilder response = 
                        new StringBuilder(
                            Encoding.ASCII.GetString(
                                state.Buffer, 0, bytesReceived));

                    OnReceived(
                        new TcpEventArgs(LocalEndPoint, RemoteEndPoint,
                                         response.ToString()));
                }

                // Receive data
                Socket.BeginReceive(state.Buffer, 0, State.BufferSize,
                                    SocketFlags.None,
                                    new AsyncCallback(ReceiveCallback),
                                    state);
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
        /// Sends a message asynchronously to the <see cref="RemoteEndPoint" />.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(string message)
        {
            Send(Encoding.ASCII.GetBytes(message));
        }

        /// <summary>
        /// Sends data asynchronously to the <see cref="RemoteEndPoint" />.
        /// </summary>
        /// <param name="data">The data as a byte array to send.</param>
        public void Send(IEnumerable<byte> data)
        {
            try
            {
                if (Socket.Connected)
                {
                    // Send the data
                    Socket.BeginSend(data.ToArray(), 0, data.ToArray().Length,
                                     SocketFlags.None,
                                     new AsyncCallback(SendCallback),
                                     new State(
                                         LocalEndPoint,
                                         RemoteEndPoint,
                                         data));
                }
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
        /// The resulting status of the <see cref="Send(IEnumerable{byte})()" /> 
        /// operation.
        /// </param>
        private void SendCallback(IAsyncResult result)
        {
            try
            {
                // Get the state
                State state = (State)result.AsyncState;

                // Send the data
                int bytesSent = Socket.EndSend(result);

                // Call event
                OnSent(new TcpEventArgs(
                         LocalEndPoint, RemoteEndPoint, state.Data));
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
        /// Disconnects the <see cref="Client" /> asynchronously from the 
        /// <see cref="RemoteEndPoint" />.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (Socket != null)
                {
                    // Call event
                    ConnectionChanged(this,
                        new ConnectionEventArgs(
                            LocalEndPoint, RemoteEndPoint,
                            ConnectionStatus.Disconnecting));

                    // Shutdown the socket
                    Socket.Shutdown(SocketShutdown.Both);

                    // Disconnect from the server
                    Socket.BeginDisconnect(true, new AsyncCallback(DisconnectCallback), new State(LocalEndPoint, RemoteEndPoint));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// The asynchronous callback for when the <see cref="Client" /> is
        /// disconnected.
        /// </summary>
        /// <param name="result">
        /// The resulting status of the <see cref="Disconnect()" /> operation.
        /// </param>
        private void DisconnectCallback(IAsyncResult result)
        {
            try
            {
                // Get the state
                State state = (State)result.AsyncState;

                // Complete the disconnection
                Socket.EndDisconnect(result);

                // Call event
                OnConnectionChanged(
                    new ConnectionEventArgs(
                        state.LocalEndPoint, 
                        state.RemoteEndPoint, 
                        ConnectionStatus.Disconnected));

                // Clean up
                Socket.Close();
                Socket = null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a <see cref="Client" />'s 
        /// <see cref="ConnectionStatus" /> has changed.
        /// </summary>
        public event EventHandler ConnectionChanged;

        /// <summary>
        /// Responds to the <see cref="ConnectionChanged" /> event.
        /// </summary>
        /// <param name="args">
        /// The event data for the <see cref="ConnectionChanged" /> event.
        /// </param>
        protected virtual void OnConnectionChanged(ConnectionEventArgs args)
        {
            EventHandler handler = ConnectionChanged;
            handler?.Invoke(this, args);
        }

        /// <summary>
        /// Occurs when a <see cref="Client" /> receives data.
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
        /// Occurs when a <see cref="Client" /> sends data.
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
        // ~Client() {
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
