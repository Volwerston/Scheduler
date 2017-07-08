using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public abstract class MailService
    {
        public abstract string GetMailBody(object context);

        public void SendEmail(string sender, string receiver, string message)
        {
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            string smtpUserName = "btsemail1@gmail.com";
            string smtpUserPass = "btsadmin";

            StringBuilder toSend = new StringBuilder();
            toSend.Append("Dear " + receiver + ", <br/>");
            toSend.Append(message);
            toSend.Append("Kind regards, <br/> Scheduler Team");

            SmtpClient client = new SmtpClient(smtpHost, smtpPort);
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUserName, smtpUserPass);
            client.EnableSsl = true;

            string msgSubject = "Scheduler Notification";       

            MailMessage mes = new MailMessage(sender,receiver, msgSubject,message);

            mes.IsBodyHtml = true;

            client.Send(mes);
        }
    }
}