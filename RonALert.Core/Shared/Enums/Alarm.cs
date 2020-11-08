using System;
using System.Collections.Generic;
using System.Text;

namespace RonALert.Core.Shared.Enums
{
    public enum AlarmType
    {
        NoFaceMask = 1,
        PeopleTooClose = 2,
        TooManyPeople = 3
    }

    public enum AlarmStatus
    {
        Open = 1,
        Closed = 2
    }
}
