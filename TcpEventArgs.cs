using System;
using System.Collections.Generic;
using System.Net;

namespace Tcp
{
    /// <summary>
    /// Provides data for the <see cref="Client.Sent" /> and 
    /// <see cref="Client.Received"/> events.
    /// </summary>
    public class TcpEventArgs : EventArgs
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
        /// Data to pass through the event.
        /// </summary>
        public object Data { set; get; }
        #endregion

        #region Constructor(s)
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionEventArgs" /> 
        /// class using the specified <see cref="LocalEndPoint" />, 
        /// <see cref="RemoteEndPoint" />, and <see cref="Status" />.
        /// </summary>
        /// <param name="localEP">Local end point.</param>
        /// <param name="remoteEP">Remote end point.</param>
        /// <param name="data">Data to pass.</param>
        public TcpEventArgs(IPEndPoint localEP, IPEndPoint remoteEP, object data)
        {
            LocalEndPoint = localEP;
            RemoteEndPoint = remoteEP;
            Data = data;
        }
        #endregion
    }
}
