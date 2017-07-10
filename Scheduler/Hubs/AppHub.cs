using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Runtime.Caching;
using Microsoft.AspNet.SignalR;
using System.Net.Http;
using System.Net.Http.Headers;
using Scheduler.Models.Custom;

namespace Scheduler.Hubs
{
    public class AppHub : Hub
    {
        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public bool AddOnlineUser(string email)
        {
            try
            {
                ObjectCache cache = MemoryCache.Default;
                List<string> users = cache["users"] as List<string>;
                if(users == null)
                {
                    users = new List<string>();
                }

                if (!users.Contains(email))
                {
                    users.Add(email);
                }

                cache.Add("users", users, DateTime.Now.AddHours(1));
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void NewMessageReceived(string SenderMail, string RecipientMail, string SenderName, DateTime SendingTime, string Message, string DialogueId)
        {
            if(!IsUserOnline(RecipientMail))
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.BaseAddress = new Uri("http://localhost:24082");

                    Notification toAdd = new Notification()
                    {
                        Body = String.Format("<a href=\"/Main/DialoguePage?recipientMail={0}\">{1}</a> sent you a new message", SenderMail, SenderName),
                        Seen = false,
                        SendingTime = DateTime.Now,
                        Title = "New message",
                        Type = String.Format("NewMessage_{0}_{1}", DialogueId, RecipientMail),
                        UserEmail = RecipientMail     
                    };

                    HttpResponseMessage msg = client.PostAsJsonAsync("/api/Notifications/AddNewMessageNotification", toAdd).Result;
                }
            }

            Clients.All.notifyOfMessage(SenderMail, RecipientMail, SenderName, SendingTime, Message, DialogueId);
        }

        public bool DeleteOnlineUser(string email)
        {
            try
            {
                ObjectCache cache = MemoryCache.Default;
                List<string> users = cache["users"] as List<string>;
                if (users == null)
                {
                    users = new List<string>();
                }

                users.Remove(email);
                cache.Add("users", users, DateTime.Now.AddHours(1)); ;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsUserOnline(string v)
        {
            ObjectCache cache = MemoryCache.Default;
            List<string> users = cache["users"] as List<string>;

            if (users == null) return false;

            return users.Contains(v);
        }
    }
}