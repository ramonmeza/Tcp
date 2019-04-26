using System;
using System.Net;

namespace Tcp
{
    /// <summary>
    /// Provides data for the <see cref="Client.ConnectionChanged" /> event.
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        #region Properties
        /// <summary>
        /// The local <see cref="IPEndPoint" /> of the object raising the event.
        /// </summary>
        public IPEndPoint LocalEndPoint { set; get; }

        /// <summary>
        /// The remote <see cref="IPEndPoint" /> of the object raising the 
        /// event.
        /// </summary>
        public IPEndPoint RemoteEndPoint { set; get; }

        /// <summary>
        /// The resulting <see cref="ConnectionStatus" /> from the event.
        /// </summary>
        public ConnectionStatus Status { set; get; }
        #endregion

        #region Constructor(s)
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionEventArgs" /> 
        /// class using the specified <see cref="LocalEndPoint" />, 
        /// <see cref="RemoteEndPoint" />, and <see cref="Status" />.
        /// </summary>
        /// <param name="localEP">Local end point.</param>
        /// <param name="remoteEP">Remote end point.</param>
        /// <param name="status">Resulting status.</param>
        public ConnectionEventArgs(IPEndPoint localEP, IPEndPoint remoteEP, ConnectionStatus status)
        {
            LocalEndPoint = localEP;
            RemoteEndPoint = remoteEP;
            Status = status;
        }
        #endregion
    }
}
