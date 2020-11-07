using System;
using System.Collections.Generic;
using System.Text;

namespace RonALert.Core.Entities
{
    public class PersonPosition : EntityBase
    {
        public Room Room { get; set; }
        public DateTime Timestamp { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public bool FaceMask { get; set; }
    }
}
