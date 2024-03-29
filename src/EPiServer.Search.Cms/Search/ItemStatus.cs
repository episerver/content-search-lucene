﻿using System;

namespace EPiServer.Search
{
    /// <summary>
    /// Enum for the possible ItemStatus values
    /// </summary>
    [Flags]
    public enum ItemStatus
    {
        Approved = 1,
        Pending = 2,
        Removed = 4
    }
}
