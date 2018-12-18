using System;
using System.Collections.Generic;
using System.Text;

public class IOChannelMgr
{
    private Dictionary<string, ushort> m_Name2ID = new Dictionary<string, ushort>();
    private Dictionary<ushort, string> m_ID2Name = new Dictionary<ushort, string>();
    static IOChannelMgr _inst = null;

    IOChannelMgr()
    {
        AddIOChannel("PWR", 1);
        AddIOChannel("ACC", 2);
        AddIOChannel("HDOP", 3);
        AddIOChannel("RS232_0", 4);
        AddIOChannel("RS232_1", 5);
        AddIOChannel("RS232_2", 6);
        AddIOChannel("RS232_3", 7);
        AddIOChannel("FUEL", 8);
        AddIOChannel("FUEL1", 9);
        AddIOChannel("DIRECTION", 10);
        AddIOChannel("FUEL2", 11);
        AddIOChannel("FUEL3", 12);
        AddIOChannel("FUEL4", 13);

        AddIOChannel("EKEY", 20);

        AddIOChannel("TEMPINT", 21);
        AddIOChannel("TEMP", 22);
        AddIOChannel("TEMP1", 23);
        AddIOChannel("TEMP2", 24);
        AddIOChannel("TEMP3", 25);
        AddIOChannel("TEMP4", 26);

        AddIOChannel("ACC1", 27);

        AddIOChannel("ACCEL", 50);          //ускорение движения
        AddIOChannel("BREAK_ACCEL", 51);    //ускорение торможения 
        AddIOChannel("TURN_ACCEL", 52);     //ускорение поворота
        AddIOChannel("VACCEL", 53);         //вертикальное ускорение

        AddIOChannel("SHOCK", 60);          //удар
        AddIOChannel("TILT", 61);           //перворот

        for (int i = 0; i < 256; i++)   //для телтоники
            AddIOChannel("IN" + i, (ushort)(i + 256));

        for (int i = 0; i < 16; i++)   //аналоговые входы
            AddIOChannel("AIN" + i, (ushort)(i + 512));

        for (int i = 0; i < 16; i++)   //дискретные входы
            AddIOChannel("DIN" + i, (ushort)(i + 768));

        for (int i = 0; i < 16; i++)   //дискретные выходы
            AddIOChannel("DOUT" + i, (ushort)(i + 1024));

        for (int i = 0; i < 16; i++)   //счетчики
            AddIOChannel("COUNT" + i, (ushort)(i + 1280));

        for (int i = 0; i < 16; i++)   //счетчики
            AddIOChannel("СAN" + i, (ushort)(i + 1536));
    }
    public static IOChannelMgr inst()
    {
        if (_inst == null)
            _inst = new IOChannelMgr();
        return _inst;
    }
    ///////////////////////////////////////////////////////////////////
    void AddIOChannel(string name, ushort id)
    {
        m_Name2ID[name] = id;
        m_ID2Name[id] = name;
    }
    ///////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////
    ///////////////////////////////////////////////////////////////////
    public string GetIOChannel(ushort id)
    {
        string res = "";
        m_ID2Name.TryGetValue(id, out res);
        return res;
    }
    public ushort GetIOChannel(string strName)
    {
        ushort res = 0;
        m_Name2ID.TryGetValue(strName, out res);
        return res;
    }
}
