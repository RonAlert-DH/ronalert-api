using System;
using System.Collections.Generic;
using System.Text;

namespace RonALert.Core.Entities
{
    public class Room : EntityBase
    {
        public string Name { get; set; }
        public int PeopleLimit { get; set; }
    }
}
