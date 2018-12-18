using System;
using System.Collections.Generic;
using System.Text;

static class Global
{
    static public Logger        m_Logger     = new Logger();
    static public ConfigMgr     m_ConfigMgr  = new ConfigMgr(m_Logger.GetQueue("ConfigMgr"));
    static public GPSPointQueue m_GPSQueue   = new GPSPointQueue(m_Logger.GetQueue("GPSQueue"));
}
