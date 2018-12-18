using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
////////////////////////////////////////////////////////////////////////
abstract class Session
{
    protected GPSPointQueue m_GPSQueue;
    protected LogItemQueue m_Logger;
    protected uint m_nID = 0;     //Ќомер сессии

    protected DateTime m_dtTimeStamp = DateTime.Now;              //врем€ последнего пакета
    protected static TimeSpan m_Timeout = new TimeSpan(0, 10, 0);         //10 мин таймаут на сессию

    protected static System.Globalization.NumberFormatInfo m_Format = new System.Globalization.NumberFormatInfo();
    //////////////////////////////////////////////////////////////////////
    public Session(GPSPointQueue GPSQueue, LogItemQueue logger)
    {
        m_Format.NumberDecimalSeparator = ".";

        m_GPSQueue = GPSQueue;
        m_Logger = logger;
    }
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    protected void LogEvent(LogLevel loglevel, string strMessage)
    {
        m_Logger.Push(loglevel, m_nID, strMessage);
    }
    //////////////////////////////////////////////////////////////////////
    protected virtual bool PushPacket(TrackerPacket packet)
    {
        LogEvent(LogLevel.INFO, packet.ToString());
        return m_GPSQueue.PushPacket(packet);
    }
    //////////////////////////////////////////////////////////////////////
    public abstract bool ProcessBuffer(byte[] buffer, int size);    //если вернет false - разорвать соединение
    //////////////////////////////////////////////////////////////////////
    protected void Send(byte[] buffer, int size)
    {
        string str = ">> " + size + " bytes: ";
        for (int i = 0; i < size; i++)
            str += " " + buffer[i].ToString("X2");
        LogEvent(LogLevel.DEBUG, str);

        InternalSend(buffer, size);
    }
    //////////////////////////////////////////////////////////////////////
    protected void Send(string str)
    {
        InternalSend(Encoding.ASCII.GetBytes(str), str.Length);
        LogEvent(LogLevel.DEBUG, ">> " + str);
    }
    //////////////////////////////////////////////////////////////////////
    public abstract void InternalSend(byte[] buffer, int size);
    //////////////////////////////////////////////////////////////////////
    static protected float String2Coord(string val)
    {
        double res = 0;
        if (double.TryParse(val, NumberStyles.Float, m_Format, out res))
        {
            res = res / 100.0;
            res = Math.Truncate(res) + (res - Math.Truncate(res)) / 0.6;
        }
        return (float)res;
    }

}
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
abstract class ServerSession : Session
{
    protected   long    m_IMEI = 0;

    protected const int m_BufferSize = 16384;//32768;
    private Socket      m_Socket = null;
    private string      m_strEndPoint = "";
    private byte[]      m_Buffer = new byte[m_BufferSize];
    //////////////////////////////////////////////////////////////////////
    public ServerSession(uint nID, Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger): base(GPSQueue, logger)
    {
        try
        {
            m_nID = nID;
            m_Socket = socket;
            m_strEndPoint = m_Socket.RemoteEndPoint.ToString();
            m_Socket.BeginReceive(m_Buffer, 0, m_BufferSize, 0, new AsyncCallback(ReadCallback), this);

            LogEvent(LogLevel.INFO, "Connection established with " + m_strEndPoint);
        }
        catch (System.Exception ex)
        {
            //LogEvent(LogLevel.ERROR, "ServerSession: " + ex.ToString());
            ex = null;
        }
    }
    //////////////////////////////////////////////////////////////////////
    ~ServerSession()
    {
        Close();
    }
    //////////////////////////////////////////////////////////////////////
    public void Close()
    {
        try
        {
            if (m_Socket.Connected)
            {
                LogEvent(LogLevel.INFO, "Connection closed " + m_strEndPoint);
                m_Socket.Close();
            }
        }
        catch (Exception ex)
        {
            //LogEvent(LogLevel.ERROR, "Close: " + ex.ToString());
        }
        m_GPSQueue.SetOffline(m_nID);
    }
    //////////////////////////////////////////////////////////////////////
    public bool IsConnected()
    {
        return m_Socket != null && m_Socket.Connected && (DateTime.Now - m_dtTimeStamp < m_Timeout); 
    }
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    void ReadCallback(IAsyncResult ar)
    {
        try
        {
            if (IsConnected())
            {
                int bytesRead = m_Socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    if (ProcessBuffer(m_Buffer, bytesRead))
                    {
                        m_dtTimeStamp = DateTime.Now;
                        m_Socket.BeginReceive(m_Buffer, 0, m_BufferSize, SocketFlags.None, new AsyncCallback(ReadCallback), this);
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogEvent(LogLevel.DEBUG, "ReadCallback: " + e.ToString());
        }
        Close();
    }
    //////////////////////////////////////////////////////////////////////
    public override void InternalSend(byte[] buffer, int size)
    {
        try
        {
            m_Socket.BeginSend(buffer, 0, size, 0, null, null);
        }
        catch (Exception e)
        {
            Close();
        }
    }
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    protected override bool PushPacket(TrackerPacket packet)
    {
        long IMEI = packet.m_ID;
        return base.PushPacket(packet) && m_GPSQueue.SetOnline(IMEI, m_nID);
    }
    //////////////////////////////////////////////////////////////////////
    protected bool IMEIIsValid()      //если true - такой IMEI зарегистрирован
    {
        LogEvent(LogLevel.INFO, "Device " + m_IMEI + " connected");
        return m_GPSQueue.SetOnline(m_IMEI, m_nID);
    }
    //////////////////////////////////////////////////////////////////////
};
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
abstract class AsyncServer
{
    ArrayList               m_ServerSessions = new ArrayList();
    protected GPSPointQueue m_GPSQueue;
    private LogItemQueue    m_Logger;

    Socket                  m_Listener;
    IPEndPoint              m_endPoint;
    ManualResetEvent        m_evtAcceptConnection;

    Timer                   m_TestConnectionTimer;
    
    static protected uint   m_nSessionCounter;      //счетчик сессий
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    public abstract override string ToString();
    //////////////////////////////////////////////////////////////////////////////////
    public AsyncServer(IPAddress ipAddres, int nPort)
    {
        m_endPoint = new IPEndPoint(ipAddres, nPort);
        m_evtAcceptConnection = new ManualResetEvent(false);  
        m_TestConnectionTimer = new Timer(new TimerCallback(TestConnections), null, 10000, 30000);

        m_GPSQueue = GPSPointQueue.inst();
        m_Logger = Global.m_Logger.GetQueue(ToString() + "." + nPort.ToString());
    }
    //////////////////////////////////////////////////////////////////////////////////
    ~AsyncServer()
    {
        StopListener();
    }
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////
    void Listen(object me)
    {
        try
        {
            m_Logger.Push(LogLevel.WARNING, 0, "Server " + ToString() +  " started at " + m_endPoint.ToString());

            m_Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_Listener.Bind(m_endPoint);
            m_Listener.Listen(100);
            while (m_Listener != null)
            {
                m_evtAcceptConnection.Reset();
                m_Listener.BeginAccept(new AsyncCallback(AcceptCallback), m_Listener);
                m_evtAcceptConnection.WaitOne();
            }
        }
        catch(Exception e)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "Listen: " + e.ToString());
        }

        StopListener();
    }
    public void StopListener()
    {
        try
        {
            if (m_Listener != null)
            {
                m_Logger.Push(LogLevel.WARNING, 0, "Server " + ToString() + " stopped");

                m_Listener.Close();
                m_Listener = null;
            }
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "StopListener: " + ex.ToString());
        }
    }
    //////////////////////////////////////////////////////////////////////////////////
    void AcceptCallback(IAsyncResult ar)
    {
        try
        {
            m_evtAcceptConnection.Set();

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            ServerSession sess = CreateSession(handler, m_GPSQueue, m_Logger);
            lock (m_ServerSessions)
                m_ServerSessions.Add(sess);
        }
        catch (Exception e)
        {
            //m_Logger.Push(LogLevel.ERROR, 0, "AcceptCallback: " + e.ToString());
        }
    }
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    public abstract ServerSession CreateSession(Socket socket, GPSPointQueue GPSQueue, LogItemQueue logger);
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    void TestConnections(object me)
    {
        try
        {
            if (m_Listener == null)
                ThreadPool.QueueUserWorkItem(new WaitCallback(Listen));

            lock (m_ServerSessions)
            {
                for (int i = 0; i < m_ServerSessions.Count; i++)
                {
                    ServerSession sess = (ServerSession)m_ServerSessions[i];
                    if (!sess.IsConnected())
                    {
                        sess.Close();
                        m_ServerSessions.RemoveAt(i--);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "Error: " + ex.ToString());        	
        }
    }
}
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////
abstract class AsyncUDPServer: Session
{
    private     UdpClient       m_Listener = null;
    private     Timer           m_TestConnectionTimer;
    private     IPEndPoint      m_endPoint;
    private     IPEndPoint      m_ClientEndPoint;
    private     AsyncCallback m_CallBack;
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    public abstract override string ToString();
    //////////////////////////////////////////////////////////////////////////////////
    public AsyncUDPServer(IPAddress ipAddres, int nPort)
        : base(GPSPointQueue.inst(), null)
    {
        m_endPoint = new IPEndPoint(ipAddres, nPort);

        m_Logger = Global.m_Logger.GetQueue(ToString() + "." + nPort.ToString());

        m_TestConnectionTimer = new Timer(new TimerCallback(Listen), null, 1000, 10000);

        m_CallBack = new AsyncCallback(ReadCallback);
    }
    //////////////////////////////////////////////////////////////////////////////////
    ~AsyncUDPServer()
    {
        m_TestConnectionTimer.Dispose();
        StopListener();
    }
    //////////////////////////////////////////////////////////////////////////////////
    public bool IsConnected()
    {
        return m_Listener != null && (DateTime.Now - m_dtTimeStamp < m_Timeout);
    }
    //////////////////////////////////////////////////////////////////////////////////
    void Listen(object me)
    {
        if (IsConnected())
            return;

        StopListener();

        try
        {
            m_Logger.Push(LogLevel.WARNING, 0, "Server " + ToString() + " started at " + m_endPoint.ToString());

            m_Listener = new UdpClient(m_endPoint);
            if (m_Listener != null)
            {
                m_dtTimeStamp = DateTime.Now;
                m_Listener.BeginReceive(m_CallBack, this);
            }
        }
        catch (Exception e)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "Listen: " + e.ToString());
        }
    }
    public void StopListener()
    {
        try
        {
            if (m_Listener != null)
            {
                m_Logger.Push(LogLevel.WARNING, 0, "Server " + ToString() + " stopped");

                m_Listener.Close();
                m_Listener = null;
            }
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, "StopListener: " + ex.ToString());
        }
    }
    //////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    void ReadCallback(IAsyncResult ar)
    {
        try
        {
            if (m_Listener != null)
            {
                m_ClientEndPoint = null;
                byte[] bytesRead = m_Listener.EndReceive(ar, ref m_ClientEndPoint);
                if (bytesRead != null && bytesRead.Length > 0)
                {
                    if (ProcessBuffer(bytesRead, bytesRead.Length))
                        m_dtTimeStamp = DateTime.Now;

                    m_Listener.BeginReceive(m_CallBack, this);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            //LogEvent(LogLevel.ERROR, "ReadCallback: " + e.ToString());
        }
        StopListener();
    }
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////
    public override void InternalSend(byte[] buffer, int size)
    {
        try
        {
            m_Listener.BeginSend(buffer, size, m_ClientEndPoint, null, null);
        }
        catch (Exception e)
        {
            StopListener();
        }
    }
}
