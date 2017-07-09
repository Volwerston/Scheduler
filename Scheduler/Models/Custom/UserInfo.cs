using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Custom
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public byte[] Avatar { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public string Settlement { get; set; }
        public string Profession { get; set; }
        public string Interests { get; set; }
        public bool IsOnline { get; set; }
        public string Email { get; set; }
        public int TimeZone { get; set; }
        public string TimeZoneDescription { get; set; }
    }
}