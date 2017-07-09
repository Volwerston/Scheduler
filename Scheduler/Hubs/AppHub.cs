using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Runtime.Caching;
using Microsoft.AspNet.SignalR;

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