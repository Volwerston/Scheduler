using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Scheduler.Models.Custom;
using System.Data.SqlClient;

namespace Scheduler.Models.Auxiliary
{
    public class FriendRequestNotificationHandler : NotificationHandler
    {
        public override bool CanHandle(Notification n)
        {
            return !String.IsNullOrEmpty(n.Type) && n.Type.Split('_')[0] == "FriendRequest";
        }

        public override void Handle(Notification n, string status)
        {
            if(status.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)[0] == "Accept")
            {
                AcceptFriendRequest(n.Type.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)[1], n.UserEmail);
            }
            else
            {
                RejectFriendRequest(n.Type.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)[1], n.UserEmail);
            }
        }

        private void RejectFriendRequest(string sender, string recipient)
        {
            using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("Delete from FriendRequests Where SenderMail = @sm AND RecipientMail = @rm", con);
                cmd.Parameters.AddWithValue("@sm", sender);
                cmd.Parameters.AddWithValue("@rm", recipient);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void AcceptFriendRequest(string sender, string recipient)
        {
            using (SqlConnection con = new SqlConnection(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["DBCS"].ConnectionString))
            {
                SqlCommand cmd = new SqlCommand("Delete from FriendRequests Where SenderMail = @sm AND RecipientMail = @rm; Insert into Friends values(@sm, @rm);", con);
                cmd.Parameters.AddWithValue("@sm", sender);
                cmd.Parameters.AddWithValue("@rm", recipient);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}