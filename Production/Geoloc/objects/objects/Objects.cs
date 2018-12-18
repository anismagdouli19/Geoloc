//Ревизия 29.10.2012

using System;
using System.Collections;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Text;
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
public abstract class CObject : Object
{
    public CObject(){}
    static public string String2XML(string str)
    {
        /*string ret = str.Replace("&", "&amp;");
        ret = ret.Replace("<", "&lt;");
        ret = ret.Replace(">", "&gt;");
        ret = ret.Replace("\"", "&quot;");
        ret = ret.Replace("'", "&apos;");*/
        return System.Security.SecurityElement.Escape(str);
    }
    static public string Float2XML(float value)
    {
        return value.ToString("0.#####").Replace(",", ".");
    }
    static public string Float2XML(decimal value)
    {
        return value.ToString("0.#####").Replace(",", ".");
    }
    static public string Float2XML(double value)
    {
        return value.ToString("0.#####").Replace(",", ".");
    }
    public abstract bool Load(MySqlDataReader reader);
}
/////////////////////////////////////////////////////////////////////////////////////////////
public class CObjectList<T> : ArrayList where T : CObject, new()
{
    public new T this[int index] 
    { 
        get {return (T)base[index];}
        set { base[index] = value; }
    }
    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        for (int i = 0; i < Count; i++)
            result.Append(this[i].ToString());
        return result.ToString();
    }

    public static int GetLastInsertIndex(CDatabase db)
    {
        try
        {
            using (MySqlCommand cmd = new MySqlCommand("SELECT LAST_INSERT_ID()", db.connection))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                    if (reader.Read())
                        return reader.GetInt32(0);
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
        }
        return 0;

    }

    public class DBQuery
    {
        public string m_Query;
        public string[] m_Parametrs;
        public DBQuery(string _Query, string[] _Parametrs)
        {
            m_Query = _Query;
            m_Parametrs = _Parametrs;
        }
        public DBQuery(string _Query)
        {
            m_Query = _Query;
            m_Parametrs = new string[0]{};
        }
    }

    protected bool Load(CDatabase db, DBQuery _Query)
    {
        Clear();
        if (_Query == null)
            return false;

        try
        {
            using (MySqlCommand cmd = new MySqlCommand(_Query.m_Query, db.connection))
            {
                for (int i = 0; i < _Query.m_Parametrs.Length; i++)
                    cmd.Parameters.AddWithValue("?Param" + (i + 1), _Query.m_Parametrs[i]);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        T obj = new T();
                        if (obj.Load(reader))
                            Add(obj);
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

    public virtual DBQuery GetLoadQuery() { Debug.Assert(false); return null; }
    public virtual bool Load(CDatabase db) { return Load(db, GetLoadQuery()); }

    public virtual DBQuery GetLoadQuery(CUser user) { Debug.Assert(false); return null; }
    public virtual bool Load(CDatabase db, CUser user) { return Load(db, GetLoadQuery(user)); }

    public virtual DBQuery GetLoadQuery(int ID, CUser user) { Debug.Assert(false); return null; }
    public virtual bool Load(CDatabase db, int ID, CUser user) { return Load(db, GetLoadQuery(ID, user)); }

    public virtual DBQuery GetInsertQuery(CUser user, T obj) { Debug.Assert(false); return null; }
    public virtual bool InsertNew(CDatabase db, CUser user, T obj)
    {
        Clear();
        DBQuery _Query = GetInsertQuery(user, obj);
        if (_Query == null)
            return false;

        int ID = 0;
        try
        {
            using (MySqlCommand cmd = new MySqlCommand(_Query.m_Query, db.connection))
            {
                for (int i = 0; i < _Query.m_Parametrs.Length; i++)
                    cmd.Parameters.AddWithValue("?Param" + (i + 1), _Query.m_Parametrs[i]);
                cmd.ExecuteNonQuery();
            }
            ID = GetLastInsertIndex(db);
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return (ID > 0) && Load(db, ID, user);
    }

    public virtual DBQuery GetDeleteQuery(int ID, CUser user) { Debug.Assert(false); return null; }
    public virtual bool Delete(CDatabase db, int ID, CUser user)
    {
        DBQuery _Query = GetDeleteQuery(ID, user);
        if (_Query == null)
            return false;
        
        try
        {
            using (MySqlCommand cmd = new MySqlCommand(_Query.m_Query, db.connection))
            {
                for (int i = 0; i < _Query.m_Parametrs.Length; i++)
                    cmd.Parameters.AddWithValue("?Param" + (i + 1), _Query.m_Parametrs[i]);
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
}
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////

/*public class CAction : CObject
{
    public int      m_nID;
    public int      m_nTrackerID;
    public string   m_strName;
    public string   m_strActionType;
    public string   m_strData;

    public CAction(){m_nID = 0;}

    public override string ToString()
    {
        return "<action " +
               "ID=\""          + m_nID + "\" " +
               "TrackerID=\""   + m_nTrackerID + "\" " +
               "Name=\""        + String2XML(m_strName) + "\" " +
               "ActionType=\""  + String2XML(m_strActionType) + "\" " +
               "Data=\""        + String2XML(m_strData) + "\" " +
               "/>";
    }
    public override bool Load(MySqlDataReader reader)
    {
        try
        {
            m_nID = reader.GetInt32("ID");
            m_nTrackerID = reader.GetInt32("TrackerID");
            m_strName = reader.GetString("Name");
            m_strActionType = reader.GetString("ActionType");
            m_strData = reader.GetString("Data");
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }
}

public class CActionList : CObjectList<CAction>
{
    public override string GetLoadQuery()           { return "SELECT * FROM Actions"; }
    public override string GetLoadQuery(int UserID) { return "SELECT * FROM Actions WHERE TrackerID IN ( SELECT ID FROM Trackers WHERE UserID = \"" + UserID + "\")"; }
    public override string GetLoadQuery(int ID, int UserID) { return "SELECT * FROM Actions WHERE ID = " + ID + " AND TrackerID IN ( SELECT ID FROM Trackers WHERE UserID = \"" + UserID + "\")"; }
    //public override string GetUpdateQuery(int UserID) { return ""; }
    public override string GetInsertQuery(int UserID) { return ""; }
    public override string GetDeleteQuery(int ID, int UserID) { return ""; }
}*/
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
public class CIcon : CObject
{
    public int      m_nID = 0;
    public string   m_strName = "";
    public string   m_strURL = "";
    public string   m_strURLInactive = "";
    public string   m_strURLDisabled = "";
    public string   m_strURLEvent = "";
    public string   m_strColor = "";
    public int      m_Width = 0;
    public int      m_Height = 0;
    public int      m_AnchorX = 0;
    public int      m_AnchorY = 0;
    public bool     m_bRotate = false;

    public CIcon() { m_nID = 0; }

    public override string ToString()
    {
        return "<icon " +
               "ID=\""      + m_nID + "\" " +
               "Name=\""    + String2XML(m_strName) + "\" " +
               "URL=\""     + String2XML(m_strURL) + "\" " +
               "URLInactive=\"" + String2XML(m_strURLInactive) + "\" " +
               "URLDisabled=\"" + String2XML(m_strURLDisabled) + "\" " +
               "Color=\""   + String2XML(m_strColor) + "\" " +
               "width=\""   + m_Width + "\" " +
               "height=\""  + m_Height + "\" " +
               "anchorx=\"" + m_AnchorX + "\" " +
               "anchory=\"" + m_AnchorY + "\" " +
               (m_bRotate ? "rotate=\"1\" ": "") +
               ((m_strURLEvent.Length > 0) ? "URLEvent=\"" + String2XML(m_strURLEvent) + "\" " : "") +
               "/>";
    }
    public override bool Load(MySqlDataReader reader)
    {
        try
        {
            m_nID = reader.GetInt32("ID");
            m_strURL = reader.GetString("url");
            m_strURLInactive = reader.GetString("url_cross");
            m_strURLDisabled = reader.GetString("url_disabled");
            m_strColor = reader.GetString("Color");
            m_Width = reader.GetInt32("width");
            m_Height = reader.GetInt32("height");
            m_AnchorX = reader.GetInt32("anchorx");
            m_AnchorY = reader.GetInt32("anchory");
            m_strName = reader.GetString("Name");
            try { m_bRotate = reader.GetBoolean("Rotate"); } catch (Exception ex) { }
            try { m_strURLEvent = reader.GetString("url_event"); } catch (Exception ex) { }
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }
}

public class CIconList : CObjectList<CIcon>
{
    public override DBQuery GetLoadQuery() { return new DBQuery("SELECT * FROM icons WHERE url_cross != \"\" ORDER BY Id"); }
    public override DBQuery GetLoadQuery(CUser user) { return new DBQuery("SELECT * FROM Icons"/* WHERE url_cross = \"\" OR ID IN (SELECT IconID FROM Trackers WHERE UserID = " + user.m_nID + ")"*/); }
}
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
public class CRegion : CObject
{
    public int m_nID = 0;
    public string m_strName = "";

    public override bool Load(MySqlDataReader reader)
    {
        try
        {
            m_nID = reader.GetInt32("ID");
            m_strName = reader.GetString("Name");
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }
}

public class CRegionList : CObjectList<CRegion>
{
    public override DBQuery GetLoadQuery() { return new DBQuery("SELECT * FROM Regions"); }
}
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
