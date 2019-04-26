namespace Tcp
{
    /// <summary>
    /// Possible statuses for a connection.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Successfully connected to a server.
        /// </summary>
        Connected,

        /// <summary>
        /// Currently attempting to connect to a server.
        /// </summary>
        Connecting,

        /// <summary>
        /// Successfully disconnected to a server.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Currently attempting to disconnect to a server.
        /// </summary>
        Disconnecting
    }
}
