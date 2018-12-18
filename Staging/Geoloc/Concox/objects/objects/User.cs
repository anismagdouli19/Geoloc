// Ревизия 28.10.2012
//Доделать: более четкое деление по ролям, больше разных прав пользователей
//Доделать: При добавлении пользователя использовать информацию переданную в функцию, убрать имя по времени

using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Text;
using System.Diagnostics;
using System.Globalization;
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
public class CUser : CObject
{
    public int m_nID = 0;
    public string m_strName = "";
    public string m_strOrgName = "";
    public string m_strContactName = "";
    public string m_strPhone = "";
    public string m_strFax = "";
    public string m_strEmail = "";
    public string m_strPostAdr = "";
    public string m_strLang = "";
    public string m_strTimeZone = "";
    public long   m_ACL = 0;
    public bool   m_bDisabled = false;
    public int    m_nRegionID = 0;

    public string m_strPassword = "";
    public CUser() { m_nID = 0; }

    public override string ToString()
    {
        return "<user " +
                "ID=\"" + m_nID + "\" " +
                "Name=\"" + String2XML(m_strName) + "\" " +
                "OrgName=\"" + String2XML(m_strOrgName) + "\" " +
                "ContactName=\"" + String2XML(m_strContactName) + "\" " +
                "Phone=\"" + String2XML(m_strPhone) + "\" " +
                "Fax=\"" + String2XML(m_strFax) + "\" " +
                "Email=\"" + String2XML(m_strEmail) + "\" " +
                "PostAdr=\"" + String2XML(m_strPostAdr) + "\" " +
                "Lang=\"" + String2XML(m_strLang) + "\" " +
                "TimeZone=\"" + m_strTimeZone + "\" " +
                "Disabled=\"" + (m_bDisabled ? 1 : 0) + "\" " +
                "ACL=\"" + m_ACL + "\" " +
                "RegionID=\"" + m_nRegionID + "\" " +
                "/>";
    }
    public override bool Load(MySqlDataReader reader)
    {
        try
        {
            m_nID = reader.GetInt32("ID");
            m_strName = reader.GetString("Name");
            m_strOrgName = reader.GetString("OrgName");
            m_strContactName = reader.GetString("ContactName");
            m_strPhone = reader.GetString("Phone");
            m_strFax = reader.GetString("Fax");
            m_strEmail = reader.GetString("Email");
            m_strPostAdr = reader.GetString("PostAdr");
            m_ACL = reader.GetInt64("ACL");
            m_bDisabled = (reader.GetInt32("Disabled") != 0) ? true : false;
            m_strPassword = reader.GetString("Password");
            m_strLang = reader.GetString("Lang").ToUpper();
            m_strTimeZone = reader.GetString("TimeZone");
            m_nRegionID = Convert.IsDBNull(reader["RegionID"]) ? 0 : reader.GetInt32("RegionID");
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }

    public enum ACL {   VIEW_CURPOS = 0, 
                        EDIT = 8, EDIT_TRACKERS_IMEI = 9, VIEW_TRACKER_PACKET=10,
                        EDITUSERS = 16, EDIT_ALL_USERS = 24};
    public bool CanDo(ACL acl){return (m_ACL & (1 << (int)acl)) != 0;}
    public CultureInfo GetCultureInfo()
    {
        String selectedLanguage = "ru-ru";
        switch (m_strLang)
        {
            case "EN":
                selectedLanguage = "en-us";
                break;
            case "TR":
                selectedLanguage = "tr-tr";
                break;
            case "AZ":
                selectedLanguage = "az-Latn-AZ";
                break;
        }
        return new CultureInfo(selectedLanguage);
    }
}
public class CUserList : CObjectList<CUser>
{
    public override string GetLoadQuery() { return "SELECT * FROM users"; }
    public override string GetLoadQuery(CUser user) 
    {
        if (user.CanDo(CUser.ACL.EDIT_ALL_USERS))
            return "SELECT * FROM users ORDER BY Name";

        return "SELECT * FROM users WHERE ID = " + user.m_nID + " OR ParentUserID = " + user.m_nID + " ORDER BY Name";
    }

    public override string GetLoadQuery(int ID, CUser user) 
    {
        if (user.CanDo(CUser.ACL.EDIT_ALL_USERS) || user.m_nID == ID)
            return "SELECT * FROM users WHERE ID = " + ID;

        return "SELECT * FROM users WHERE ID = " + ID + " AND ParentUserID = " + user.m_nID; 
    }
    public override string GetInsertQuery(CUser user, CUser newUser)//TODO: newUser пока не используется
    {
        if (!user.CanDo(CUser.ACL.EDITUSERS) && !user.CanDo(CUser.ACL.EDIT_ALL_USERS))
            return "";
        int time = CTime.GetTime(DateTime.Now);
        return "INSERT INTO Users(Name, ParentUserID) VALUES(\"u" + time % 10000 + "." + DateTime.Now.Millisecond + "\", " + user.m_nID + ")"; 
    }
    public override string GetDeleteQuery(int ID, CUser user)
    {
        if (user.CanDo(CUser.ACL.EDIT_ALL_USERS))
            return "DELETE FROM Users WHERE ID = " + ID;

        if (!user.CanDo(CUser.ACL.EDITUSERS))
            return "";

        return "DELETE FROM Users WHERE ID = " + ID + " AND ParentUserID = " + user.m_nID; 
    }   

    public bool Update(CDatabase db, CUser user, CUser obj)
    {
        string strQuery = "";
        string strQueryPrefix = "UPDATE Users SET Name=?Name, OrgName=?OrgName, ContactName=?ContactName, PostAdr=?PostAdr, Phone=?Phone, Fax=?Fax, Email=?Email, Lang=?Lang, TimeZone=?TimeZone, Disabled = ?Disabled, ACL=?ACL, Lang=?Lang, TimeZone=?TimeZone, RegionID=?RegionID WHERE ID=" + obj.m_nID + " ";
        if (user.CanDo(CUser.ACL.EDIT_ALL_USERS))
            strQuery = strQueryPrefix;
        else
            if (user.CanDo(CUser.ACL.EDITUSERS))
                strQuery = strQueryPrefix + " AND ParentUserID = " + user.m_nID;

        try
        {
            using (MySqlCommand cmd = new MySqlCommand(strQuery, db.connection))
            {
                cmd.Parameters.AddWithValue("?Name",        obj.m_strName);
                cmd.Parameters.AddWithValue("?OrgName",     obj.m_strOrgName);
                cmd.Parameters.AddWithValue("?ContactName", obj.m_strContactName);
                cmd.Parameters.AddWithValue("?PostAdr",     obj.m_strPostAdr);
                cmd.Parameters.AddWithValue("?Phone",       obj.m_strPhone);
                cmd.Parameters.AddWithValue("?Fax",         obj.m_strFax);
                cmd.Parameters.AddWithValue("?Email",       obj.m_strEmail);
                cmd.Parameters.AddWithValue("?Disabled",    obj.m_bDisabled);
                cmd.Parameters.AddWithValue("?ACL",         obj.m_ACL & ~(obj.m_ACL ^ user.m_ACL));
                cmd.Parameters.AddWithValue("?Lang",        obj.m_strLang);
                cmd.Parameters.AddWithValue("?TimeZone",    obj.m_strTimeZone);

                if (obj.m_nRegionID > 0)
                    cmd.Parameters.AddWithValue("?RegionID",obj.m_nRegionID);
                else
                    cmd.Parameters.AddWithValue("?RegionID",DBNull.Value);
                
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
    public bool UpdateContact(CDatabase db, CUser user)
    {
        if (!user.CanDo(CUser.ACL.EDIT))
            return false;

        try
        {
            using (MySqlCommand cmd = new MySqlCommand("UPDATE Users SET OrgName=?OrgName, ContactName=?ContactName, PostAdr=?PostAdr, Phone=?Phone, Fax=?Fax, Email=?Email, Lang=?Lang, TimeZone=?TimeZone, RegionID=?RegionID WHERE ID=" + user.m_nID, db.connection))
            {
                cmd.Parameters.AddWithValue("?OrgName", user.m_strOrgName);
                cmd.Parameters.AddWithValue("?ContactName", user.m_strContactName);
                cmd.Parameters.AddWithValue("?PostAdr", user.m_strPostAdr);
                cmd.Parameters.AddWithValue("?Phone", user.m_strPhone);
                cmd.Parameters.AddWithValue("?Fax", user.m_strFax);
                cmd.Parameters.AddWithValue("?Email", user.m_strEmail);
                cmd.Parameters.AddWithValue("?Lang", user.m_strLang);
                cmd.Parameters.AddWithValue("?TimeZone", user.m_strTimeZone);
                cmd.Parameters.AddWithValue("?RegionID", user.m_nRegionID);
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
    public bool SetPassword(CDatabase db, CUser user, int userID, string password)
    {
        string strQuery = "";
        string strQueryPrefix = "UPDATE Users SET Password = '" + _MD5.GetHash(password) + "' WHERE ID = " + userID + " ";
        if (user.CanDo(CUser.ACL.EDIT_ALL_USERS) || (user.m_nID == userID && user.CanDo(CUser.ACL.EDIT)))
            strQuery = strQueryPrefix;
        else
            if (user.CanDo(CUser.ACL.EDITUSERS))
                strQuery = strQueryPrefix + " AND ParentUserID = " + user.m_nID;

        try
        {
            using (MySqlCommand cmd = new MySqlCommand(strQuery, db.connection))
                if (cmd.ExecuteNonQuery() != 0)
                    return true;
        }
        catch (Exception e){Debug.Assert(false, e.ToString());}
        return false;
    }
    public static CUser GetActiveUser(CDatabase db, int UserID)
    {
        CUserList users = new CUserList();
        return (users.Load(db, "SELECT * FROM Users WHERE ID = " + UserID) && users.Count > 0 && !((CUser)users[0]).m_bDisabled) ? (CUser)users[0] : null;
    }
    public static CUser GetActiveUser(CDatabase db, string userName)
    {
        CUserList users = new CUserList();
        return (users.Load(db, "SELECT * FROM Users WHERE Name = \"" + userName + "\"") && users.Count > 0 && !((CUser)users[0]).m_bDisabled) ? (CUser)users[0] : null;
    }
}
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////