﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Scheduler.Models.Auxiliary
{
    public class MessageWriter
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime LastDeletionTime { get; set; }
    }
}