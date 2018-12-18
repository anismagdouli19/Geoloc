using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;

public class Email
{
    public string m_strAddress;
    public string m_strMessage;
}

public class EmailSender
{
    Timer m_Timer = null;
    List<Email> m_Emails = new List<Email>();

    protected LogItemQueue m_Logger;

    static EmailSender m_inst = null;

    public EmailSender()
    {
        m_Timer = new Timer(Tick, null, 1000, 30000);
    
        ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
    }
    public void SetLogger(LogItemQueue queue)
    {
        m_Logger = queue;
    }

    public static EmailSender inst()
    {
        if (m_inst == null)
            m_inst = new EmailSender();
        return m_inst;
    }
    //////////////////////////////////////////////////////////////////////////
    void Tick(object obj)
    {
        try
        {
            if (m_Emails.Count > 0)
            {
                SmtpClient client = new SmtpClient(Configuration.inst().Email.SMTPHost, Configuration.inst().Email.SMTPPort);
                client.Credentials = new NetworkCredential(Configuration.inst().Email.User, Configuration.inst().Email.Password);
                client.EnableSsl = Configuration.inst().Email.SSL;
                client.Timeout = 5000;

                lock (m_Emails)
                    if (SendMessage(client, m_Emails[0]))
                        m_Emails.RemoveAt(0);
            }
        }
        catch (Exception e)
        {
        }
    }
    //////////////////////////////////////////////////////////////////////////
    bool SendMessage(SmtpClient client, Email mail)
    {
        try
        {
            string addr = mail.m_strAddress.Replace(";", ",");
            addr = addr.Replace(" ", "");

            m_Logger.Push(LogLevel.INFO, 0, "Sending message: " + addr + "->" + mail.m_strMessage);

            MailMessage msg = new MailMessage(Configuration.inst().Email.From, addr, Configuration.inst().Email.Title, mail.m_strMessage);
            client.Send(msg);
            return true;
        }
        catch (Exception ex)
        {
            m_Logger.Push(LogLevel.ERROR, 0, ex.ToString());
        }
        return false;
    }
    public void AddMessage(string strAddr, string strMessage)
    {
        if (strAddr.Length == 0)
            return;

        m_Logger.Push(LogLevel.INFO, 0, "Message added: " + strAddr + "->" + strMessage);

        Email email = new Email();
        email.m_strAddress = strAddr;
        email.m_strMessage = strMessage;

        lock (m_Emails)
            m_Emails.Add(email);
    }
}
