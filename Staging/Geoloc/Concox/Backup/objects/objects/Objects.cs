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
        string ret = str.Replace("&", "&amp;");
        ret = ret.Replace("<", "&lt;");
        ret = ret.Replace(">", "&gt;");
        ret = ret.Replace("\"", "&quot;");
        ret = ret.Replace("'", "&apos;");
        return ret;
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

    protected bool Load(CDatabase db, string strQuery)
    {
        Clear();
        if (strQuery == "")
            return false;

        try
        {
            using (MySqlCommand cmd = new MySqlCommand(strQuery, db.connection))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        T obj = new T();
                        if (obj.Load(reader))
                            Add(obj);
                    }
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return true;
    }

    public virtual string GetLoadQuery() { Debug.Assert(false); return ""; }
    public virtual bool Load(CDatabase db) { return Load(db, GetLoadQuery()); }

    public virtual string GetLoadQuery(CUser user) { Debug.Assert(false); return ""; }
    public virtual bool Load(CDatabase db, CUser user) { return Load(db, GetLoadQuery(user)); }

    public virtual string GetLoadQuery(int ID, CUser user) { Debug.Assert(false); return ""; }
    public virtual bool Load(CDatabase db, int ID, CUser user) { return Load(db, GetLoadQuery(ID, user)); }

    public virtual string GetInsertQuery(CUser user, T obj) { Debug.Assert(false); return ""; }
    public virtual bool InsertNew(CDatabase db, CUser user, T obj)
    {
        Clear();
        string strQuery = GetInsertQuery(user, obj);
        if (strQuery == "")
            return false;

        int ID = 0;
        try
        {
            using (MySqlCommand cmd = new MySqlCommand(strQuery, db.connection))
                cmd.ExecuteNonQuery();

            ID = GetLastInsertIndex(db);
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.ToString());
            return false;
        }
        return (ID > 0) && Load(db, ID, user);
    }

    public virtual string GetDeleteQuery(int ID, CUser user) { Debug.Assert(false); return ""; }
    public virtual bool Delete(CDatabase db, int ID, CUser user)
    {
        string strQuery = GetDeleteQuery(ID, user);
        if (strQuery == "")
            return false; 
        
        try
        {
            using (MySqlCommand cmd = new MySqlCommand(strQuery, db.connection))
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