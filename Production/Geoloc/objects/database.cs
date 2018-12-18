using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Diagnostics;

public class CDatabase : IDisposable
{
    public MySqlConnection connection = null;
    public CDatabase(string login, string pwd, string db, string host)
    {
        try
        {
            connection = new MySqlConnection("datasource=" + host + ";username=" + login + ";password=" + pwd + ";database=" + db + ";default command timeout=60;");
            connection.Open();
        }
        catch (Exception ex)
        {
            Debug.Assert(false, ex.ToString());
        }
    }
    ~CDatabase()
    {
        Dispose(true);
    }
    public void Dispose()
    {
        Dispose(false);
        GC.SuppressFinalize(this);
    }
    private void Dispose(bool bFinalizer)
    {
        if (connection != null && !bFinalizer)
            connection.Dispose();   //managed class?????
        connection = null;
    }
    /////////////////////////////////////////////////////////////////
    public bool IsConnected()
    {
        return connection != null && connection.State == ConnectionState.Open;
    }
}