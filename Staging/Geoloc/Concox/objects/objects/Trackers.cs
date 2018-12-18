//Ревизия 29.10.2012
//TODO: использовать tracker при insert
using System;
using System.Collections;
using System.Text;
using MySql.Data.MySqlClient;
using System.Diagnostics;

public class CTracker : CObject
{
    public int      m_nID = 0;
    public int      m_nUserID = 0;
    public long     m_IMEI = 0;
    public string   m_strName = "";
    public string   m_strComment = "";
    public string   m_strDeviceType = "";
    public string   m_strFlags = "";
    public int      m_nIconID = 1;
    public string   m_strColor = "";
    public string   m_strPhone = "";
    public int      m_Period = 60;
    public int      m_Timeout = 600;
    public int      m_ParkRadius = 50;
    public int      m_MinParkTime = 300;
    public int      m_MaxSpeed = 0;
    public int      m_AlarmParkTime = 0;
    public float    m_fFuelExpensePerKm = 0;
    public float    m_fFuelExpensePerHr = 0;
    public float    m_fMinDrain = 0;
    public int      m_nDaysToStore = 90;

    public int      m_MinIdleTime = 180;

    public int      m_dtCreateDate = 0;
    public int      m_dtInstallDate = 0;
    public string   m_strStateNumber = "";
    public string   m_strInstallerName = "";

    public CTracker() { }

    public string GetDesc()
    {
        return m_strName + ((m_strComment.Length > 0) ? "(" + m_strComment + ")" : "");
    }
    public bool HasFlag(string str) { return m_strFlags.IndexOf(str) >= 0; }

    public override bool Load(MySqlDataReader reader)
    {
        try
        {
            m_nID = reader.GetInt32("ID");
            m_nUserID = reader.GetInt32("UserID");
            m_IMEI = DBNull.Value.Equals(reader["IMEI"]) ? 0 : reader.GetInt64("IMEI");
            m_strName = reader.GetString("Name");
            m_strComment = reader.GetString("Comment").Trim();
            m_strDeviceType = reader.GetString("DeviceType");
            m_strFlags = reader.GetString("Flags");
            m_nIconID = reader.GetInt32("IconID");
            m_strColor = reader.GetString("HistoryColor");
            m_strPhone = reader.GetString("Phone");
            m_Period = reader.GetInt32("Period");
            m_Timeout = reader.GetInt32("SleepPeriod");
            m_ParkRadius = reader.GetInt32("ParkRadius");
            m_MinParkTime = reader.GetInt32("MinParkTime");
            m_MinIdleTime = reader.GetInt32("MinIdleTime");
            m_nDaysToStore = reader.GetInt32("DaysToStore");
            m_fFuelExpensePerKm = reader.GetFloat("FuelExpense");
            m_fFuelExpensePerHr = reader.GetFloat("FuelExpenseHr");
            m_fMinDrain = reader.GetFloat("MinDrain");
            m_MaxSpeed = reader.GetInt32("MaxSpeed");
            m_AlarmParkTime = reader.GetInt32("AlarmParkTime");
            m_dtCreateDate = reader.GetInt32("CreateDate");
            m_dtInstallDate = reader.GetInt32("InstallDate");
            m_strStateNumber = reader.GetString("StateNumber");
            m_strInstallerName = reader.GetString("InstallerName");
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }
}

public class CTrackerList : CObjectList<CTracker>
{
    string strWhere = " (UserID = %user OR ID IN (SELECT TrackerID FROM trackers2users WHERE UserID = %user))";

    public override string GetLoadQuery(CUser user) { return "SELECT * FROM trackers WHERE " + strWhere.Replace("%user", user.m_nID.ToString()) + " ORDER BY Number ASC"; }
    public override bool Load(CDatabase db, CUser user) 
    {
        if (!Load(db, GetLoadQuery(user)))
            return false;
        return true;
    }
    public override string GetLoadQuery(int ID, CUser user) { return "SELECT * FROM trackers WHERE ID=" + ID + " AND " + strWhere.Replace("%user", user.m_nID.ToString()); }
    public override bool Load(CDatabase db, int ID, CUser user)
    {
        if (!Load(db, GetLoadQuery(ID, user)))
            return false;
        return true;
    }
    public override string GetLoadQuery() { return "SELECT * FROM trackers WHERE Enable != 0"; }
    public override bool Load(CDatabase db)
    {
        if (!Load(db, GetLoadQuery()))
            return false;

        return true;
    }
}