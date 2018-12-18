//Ревизия 29.10.2012
//TODO: использовать tracker при insert
using System;
using System.Collections;
using System.Text;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Collections.Generic;

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
    public int      m_Timeout = 600;
    public int      m_ParkRadius = 50;
    public int      m_MinParkTime = 300;
    public int      m_MaxSpeed = 0;
    public int      m_AlarmParkTime = 0;
    public float    m_fFuelExpensePerKm = 0;
    public float    m_fFuelExpensePerHr = 0;

    public float    m_fMinDrainSpeed = 0;//per min
    public float    m_fMinDrain = 0;

    public int      m_nDaysToStore = 90;

    public int      m_MinIdleTime = 180;

    public int      m_dtCreateDate = 0;
    //public int      m_dtInstallDate = 0;
    public string   m_strStateNumber = "";


    public CTracker() { m_nID = 0; }
    public CTracker(CTracker tracker) 
    { 
        m_nID = tracker.m_nID;
        m_nUserID = tracker.m_nUserID;
        m_IMEI = tracker.m_IMEI;
        m_strName = tracker.m_strName;
        m_strComment = tracker.m_strComment;
        m_strDeviceType = tracker.m_strDeviceType;
        m_strFlags = tracker.m_strFlags;
        m_nIconID = tracker.m_nIconID;
        m_strColor = tracker.m_strColor;
        m_strPhone = tracker.m_strPhone;
        m_Timeout = tracker.m_Timeout;
        m_ParkRadius = tracker.m_ParkRadius;
        m_MinParkTime = tracker.m_MinParkTime;
        m_MaxSpeed = tracker.m_MaxSpeed;
        m_AlarmParkTime = tracker.m_AlarmParkTime;
        m_fFuelExpensePerKm = tracker.m_fFuelExpensePerKm;
        m_fFuelExpensePerHr = tracker.m_fFuelExpensePerHr;
        m_fMinDrain = tracker.m_fMinDrain;
        m_fMinDrainSpeed = tracker.m_fMinDrainSpeed; 
        m_nDaysToStore = tracker.m_nDaysToStore;

        m_MinIdleTime = tracker.m_MinIdleTime;

        m_dtCreateDate = tracker.m_dtCreateDate;
        //m_dtInstallDate = tracker.m_dtInstallDate;
        m_strStateNumber = tracker.m_strStateNumber;
        //m_strInstallerName = tracker.m_strInstallerName;
    }

    public string GetDesc()
    {
        return m_strName + ((m_strComment.Length > 0) ? "(" + m_strComment + ")" : "");
    }
    public bool HasFlag(string str) { return m_strFlags.IndexOf(str) >= 0; }

    public override string ToString()
    {
        return "<tracker " +
                "ID=\"" + m_nID + "\" " +
                "UserID=\"" + m_nUserID + "\" " +
                "Name=\"" + String2XML(m_strName) + "\" " +
                "Comment=\"" + String2XML(m_strComment) + "\" " +
                "DeviceType=\"" + String2XML(m_strDeviceType) + "\" " +
                "IconID=\"" + m_nIconID + "\" " +
                "Color=\"" + String2XML(m_strColor) + "\" " +
                "Flags=\"" + String2XML(m_strFlags) + "\" " +
                "Phone=\"" + String2XML(m_strPhone) + "\" " +
                "SleepPeriod=\"" + m_Timeout + "\" " +
                "ParkRadius=\"" + m_ParkRadius + "\" " +
                "MinParkTime=\"" + m_MinParkTime + "\" " +
                "MinIdleTime=\"" + m_MinIdleTime + "\" " +
                "FuelExpense=\"" + CObject.Float2XML(m_fFuelExpensePerKm) + "\" " +
                "FuelExpensePerHr=\"" + CObject.Float2XML(m_fFuelExpensePerHr) + "\" " +
                "MinDrain=\"" + CObject.Float2XML(m_fMinDrain) + "\" " +
                "MinDrainSpeed=\"" + CObject.Float2XML(m_fMinDrainSpeed) + "\" " +
                "MaxSpeed=\"" + m_MaxSpeed + "\" " +
                "AlarmParkTime=\"" + m_AlarmParkTime + "\" " +
                "IMEI=\"" + m_IMEI + "\" " +
                "DaysToStore=\"" + m_nDaysToStore + "\" " +
                "CreateDate=\"" + m_dtCreateDate + "000\" " +
                //"InstallDate=\"" + m_dtInstallDate + "000\" " +
                "StateNumber=\"" + String2XML(m_strStateNumber) + "\" " +
                //"InstallerName=\"" + String2XML(m_strInstallerName) + "\" " +
                "/>";
    }
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
            m_strFlags = reader.GetString("Flags").Trim();
            m_nIconID = reader.GetInt32("IconID");
            m_strColor = reader.GetString("HistoryColor");
            m_strPhone = reader.GetString("Phone");
            m_Timeout = reader.GetInt32("SleepPeriod");
            m_ParkRadius = reader.GetInt32("ParkRadius");
            m_MinParkTime = reader.GetInt32("MinParkTime");
            m_MinIdleTime = reader.GetInt32("MinIdleTime");
            m_nDaysToStore = reader.GetInt32("DaysToStore");
            m_fFuelExpensePerKm = reader.GetFloat("FuelExpense");
            m_fFuelExpensePerHr = reader.GetFloat("FuelExpenseHr");
            m_fMinDrain = reader.GetFloat("MinDrain");
            m_fMinDrainSpeed = reader.GetFloat("MinDrainSpeed");
            m_MaxSpeed = reader.GetInt32("MaxSpeed");
            m_AlarmParkTime = reader.GetInt32("AlarmParkTime");
            m_dtCreateDate = reader.GetInt32("CreateDate");
            //m_dtInstallDate = reader.GetInt32("InstallDate");
            m_strStateNumber = reader.GetString("StateNumber");
            //m_strInstallerName = reader.GetString("InstallerName");

        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }

        if (m_fMinDrain <= 0)
        {
            m_fMinDrain = 8;
            if (m_fFuelExpensePerKm >= 60)
                m_fMinDrain *= 2;
            else
                if (m_fFuelExpensePerKm >= 40)
                    m_fMinDrain *= 1.5F;
        }
        if (m_fMinDrainSpeed < 0.9)
            m_fMinDrainSpeed = 0.9F;

        return true;
    }
}

public class CTrackerList : CObjectList<CTracker>
{
    string strWhere = " (UserID = %user OR ID IN (SELECT TrackerID FROM trackers2users WHERE UserID = %user))";
    public override DBQuery GetInsertQuery(CUser user, CTracker tracker) { return (user.CanDo(CUser.ACL.EDIT) && user.CanDo(CUser.ACL.EDIT_TRACKERS_IMEI)) ? new DBQuery("INSERT INTO Trackers (UserID, CreateDate) VALUES ('" + user.m_nID + "', " + CTime.GetTime(DateTime.UtcNow) + ")") : null; }
    public override DBQuery GetDeleteQuery(int ID, CUser user)
    {
        if (!user.CanDo(CUser.ACL.EDIT) || !user.CanDo(CUser.ACL.EDIT_TRACKERS_IMEI))
            return null;
        return new DBQuery("DELETE FROM points WHERE TrackerID=" + ID + " AND TrackerID IN (SELECT ID FROM Trackers WHERE ID=" + ID + " AND UserID=" + user.m_nID + ");" +
                "DELETE FROM Events WHERE TrackerID=" + ID + " AND TrackerID IN (SELECT ID FROM Trackers WHERE ID=" + ID + " AND UserID=" + user.m_nID + ");" +
                "DELETE FROM Commands WHERE TrackerID=" + ID + " AND TrackerID IN (SELECT ID FROM Trackers WHERE ID=" + ID + " AND UserID=" + user.m_nID + ");" +
                "DELETE FROM Trackers WHERE ID=" + ID + " AND UserID=" + user.m_nID + ";" +
                "DELETE FROM trackers2users WHERE TrackerID=" + ID + " AND UserID=" + user.m_nID + ";");
    }
    public override DBQuery GetLoadQuery(CUser user) { return new DBQuery("SELECT * FROM trackers WHERE " + strWhere.Replace("%user", user.m_nID.ToString()) + " ORDER BY Number ASC"); }
    public override bool Load(CDatabase db, CUser user)
    {
        if (!Load(db, GetLoadQuery(user)))
            return false;
        return true;
    }
    public override DBQuery GetLoadQuery(int ID, CUser user) { return new DBQuery("SELECT * FROM trackers WHERE ID=" + ID + " AND " + strWhere.Replace("%user", user.m_nID.ToString())); }
    public override bool Load(CDatabase db, int ID, CUser user)
    {
        if (!Load(db, GetLoadQuery(ID, user)))
            return false;

        return true;
    }
    public bool Load(CDatabase db, string strQuery)
    {
        return base.Load(db, new DBQuery(strQuery));//TODO: remove this
    }
    public override DBQuery GetLoadQuery() { return new DBQuery("SELECT * FROM trackers WHERE Enable != 0"); }
    public override bool Load(CDatabase db)
    {
        if (!Load(db, GetLoadQuery()))
            return false;

        return true;
    }

    public bool Update(CDatabase db, CUser user, CTracker obj)//TODO: пока настраивает только владелец
    {
        if (!user.CanDo(CUser.ACL.EDIT))
            return true;
        try
        {
            string strQuery = "UPDATE Trackers SET Name=?Name, Comment=?Comment, DeviceType = ?DeviceType, IconID=?IconID, " +
                              "HistoryColor=?Color, Phone=?Phone, SleepPeriod=?SleepPeriod, " +
                              "ParkRadius=?ParkRadius, MinParkTime=?MinParkTime, MinIdleTime=?MinIdleTime, " +
                              "AlarmParkTime=?AlarmParkTime, IMEI=?IMEI, Flags=?Flags," +
                              "FuelExpense=?FuelExpense, FuelExpenseHr=?FuelExpenseHr, MaxSpeed=?MaxSpeed, MinDrain=?MinDrain, MinDrainSpeed=?MinDrainSpeed, " +
                              "StateNumber=?StateNumber, DefLat=?DefLat, DefLng=?DefLng," +
                              "DaysToStore=?DaysToStore WHERE ID=" + obj.m_nID + " AND UserID=" + user.m_nID;
            if (!user.CanDo(CUser.ACL.EDIT_TRACKERS_IMEI))
                strQuery = strQuery.Replace("IMEI=?IMEI,", "");
            using (MySqlCommand cmd = new MySqlCommand(strQuery, db.connection))
            {
                cmd.Parameters.AddWithValue("?Name", obj.m_strName);
                cmd.Parameters.AddWithValue("?Comment", obj.m_strComment);
                cmd.Parameters.AddWithValue("?DeviceType", obj.m_strDeviceType);
                cmd.Parameters.AddWithValue("?Flags", obj.m_strFlags);
                cmd.Parameters.AddWithValue("?IconID", obj.m_nIconID);
                cmd.Parameters.AddWithValue("?Color", obj.m_strColor);
                cmd.Parameters.AddWithValue("?Phone", obj.m_strPhone);
                cmd.Parameters.AddWithValue("?SleepPeriod", obj.m_Timeout);
                cmd.Parameters.AddWithValue("?ParkRadius", obj.m_ParkRadius);
                cmd.Parameters.AddWithValue("?MaxSpeed", obj.m_MaxSpeed);
                cmd.Parameters.AddWithValue("?AlarmParkTime", obj.m_AlarmParkTime);

                if (obj.m_IMEI != 0) cmd.Parameters.AddWithValue("?IMEI", obj.m_IMEI);
                else cmd.Parameters.AddWithValue("?IMEI", null);

                cmd.Parameters.AddWithValue("?MinParkTime", obj.m_MinParkTime);
                cmd.Parameters.AddWithValue("?MinIdleTime", obj.m_MinIdleTime);
                cmd.Parameters.AddWithValue("?FuelExpense", obj.m_fFuelExpensePerKm);
                cmd.Parameters.AddWithValue("?FuelExpenseHr", obj.m_fFuelExpensePerHr);
                cmd.Parameters.AddWithValue("?MinDrain", obj.m_fMinDrain);
                cmd.Parameters.AddWithValue("?MinDrainSpeed", obj.m_fMinDrainSpeed);
                cmd.Parameters.AddWithValue("?DaysToStore", obj.m_nDaysToStore);

                //cmd.Parameters.AddWithValue("?InstallDate", obj.m_dtInstallDate);
                cmd.Parameters.AddWithValue("?StateNumber", obj.m_strStateNumber);
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }

    public bool UpdateGroupIDs(CDatabase db, CUser user, CTracker obj, int[] groupIDs)
    {
        if (!user.CanDo(CUser.ACL.EDIT))
            return false;

        try
        {
            string strIDs = "";
            //добавить новые группы
            using (MySqlCommand cmd = new MySqlCommand("INSERT IGNORE INTO trackers2groups(GroupID, TrackerID) VALUES (?GroupID, " + obj.m_nID + ")", db.connection))
            {
                cmd.Parameters.Add("?GroupID", MySqlDbType.Int32);
                for (int i = 0; i < groupIDs.Length; i++)
                {
                    cmd.Parameters["?GroupID"].Value = groupIDs[i];
                    cmd.ExecuteNonQuery();

                    strIDs += ((i != 0) ? "," : "") + groupIDs[i];
                }
            }

            //удалить неактуальыне
            using (MySqlCommand cmd = new MySqlCommand("DELETE FROM trackers2groups WHERE TrackerID = " + obj.m_nID + " " +
                                                        "AND GroupID IN (SELECT ID FROM TrackerGroups WHERE UserID = " + user.m_nID + ") " +
                                                        ((strIDs.Length > 0) ? " AND GroupID NOT IN (" + strIDs + ")" : ""), db.connection))
                cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }
    public static bool SetOwner(CDatabase db, CUser user, int newOwnerID, int[] IDs)
    {
        if (!user.CanDo(CUser.ACL.EDIT) || !user.CanDo(CUser.ACL.EDITUSERS))
            return false;

        try
        {
            using (MySqlCommand cmd = new MySqlCommand("UPDATE trackers SET UserID = " + newOwnerID + "  WHERE ID = ?ID", db.connection))
            {
                cmd.Parameters.Add("?ID", MySqlDbType.Int32);
                for (int i = 0; i < IDs.Length; i++)
                {
                    cmd.Parameters["?ID"].Value = IDs[i];
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }
    public static bool Share2User(CDatabase db, CUser user, int share2UserID, int[] IDs)
    {
        if (!user.CanDo(CUser.ACL.EDIT) || !user.CanDo(CUser.ACL.EDITUSERS))
            return false;

        try
        {
            string strIDs = "";
            using (MySqlCommand cmd = new MySqlCommand("INSERT IGNORE INTO trackers2users (UserID, TrackerID) VALUES (" + share2UserID + ", ?TrackerID)", db.connection))
            {
                cmd.Parameters.Add("?TrackerID", MySqlDbType.Int32);
                for (int i = 0; i < IDs.Length; i++)
                {
                    strIDs += ((i != 0) ? "," : "") + IDs[i];
                    cmd.Parameters["?TrackerID"].Value = IDs[i];
                    cmd.ExecuteNonQuery();
                }
            }
            //удалить неактуальыне
            using (MySqlCommand cmd = new MySqlCommand("DELETE FROM trackers2users WHERE UserID=" + share2UserID + ((strIDs.Length > 0) ? " AND TrackerID NOT IN (" + strIDs + ")" : ""), db.connection))
                cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }

}
/////////////////////////////////////////////////////////////////////////////////////////////
public class CTrackersDict : Dictionary<int, CTrackerList>
{
    public void Add(int ID, CTracker tracker)
    {
        if (!ContainsKey(ID))
            this[ID] = new CTrackerList();

        CTrackerList trackers = this[ID];
        for (int i = 0; i < trackers.Count; i++)
            if (trackers[i].m_nID == tracker.m_nID)
                return;

        trackers.Add(tracker);
    }
}
