using System;
using System.Collections.Generic;
using System.Text;

namespace RonALert.Core.Models
{
    public class PersonDTO
    {
        public bool FaceMask { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double NearestDistance { get; set; }
    }
}
