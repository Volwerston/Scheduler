using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Scheduler.Models
{
    public class MongoUserManager : UserManager<ApplicationUser>
    {
        public MongoUserManager(IUserStore<ApplicationUser> store) : base(store)
        {
        }

        public override Task<IdentityResult> CreateAsync(ApplicationUser user)
        {
            return base.CreateAsync(user);
        }
        
        public Task<ApplicationUser> GetUserAsync(string userId)
        {
            return null;
        }

        public override Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            return base.UpdateAsync(user);
        }

        public override Task<IdentityResult> DeleteAsync(ApplicationUser user)
        {
            return base.DeleteAsync(user);
        }
    }
}