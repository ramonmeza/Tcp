using System.Net;
using System.Text;

namespace Tcp
{
    /// <summary>
    /// Represents the state of a <see cref="Tcp" /> object.
    /// </summary>
    internal class State
    {
        #region Constants
        /// <summary>
        /// The size of the buffer.
        /// </summary>
        public const int BufferSize = 1024;
        #endregion

        #region Properties
        /// <summary>
        /// A buffer for receiving data.
        /// </summary>
        public byte[] Buffer { set; get; }
        
        /// <summary>
        /// Miscellaneous data.
        /// </summary>
        public object Data { set; get; }

        /// <summary>
        /// The local <see cref="IPEndPoint" /> of the state.
        /// </summary>
        public IPEndPoint LocalEndPoint { set; get; }

        /// <summary>
        /// The remote <see cref="IPEndPoint" /> of the state.
        /// </summary>
        public IPEndPoint RemoteEndPoint { set; get; }
        #endregion

        #region Constructor(s)
        /// <summary>
        /// Initializes a new instance of the <see cref="State" /> class using 
        /// the specified <see cref="LocalEndPoint" />, 
        /// <see cref="RemoteEndPoint" />, and <see cref="Data" />.
        /// </summary>
        /// <param name="localEP">Local end point.</param>
        /// <param name="remoteEP">Remote end point.</param>
        /// <param name="data">Data to pass.</param>
        public State(IPEndPoint localEP, IPEndPoint remoteEP, object data = null)
        {
            LocalEndPoint = localEP;
            RemoteEndPoint = remoteEP;
            Buffer = new byte[BufferSize];
            Data = data;
        }
        #endregion
    }
}
