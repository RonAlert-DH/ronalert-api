using RonALert.Core.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RonALert.Core.Entities
{
    public class Alarm : EntityBase
    {
        public Room Room { get; set; }
        public AlarmType Type { get; set; }
        public AlarmStatus Status { get; set; }
    }
}
