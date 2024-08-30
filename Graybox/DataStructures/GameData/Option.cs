﻿using System;

namespace Graybox.DataStructures.GameData
{
    public class Option
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public bool On { get; set; }

        public string DisplayText()
        {
            return String.IsNullOrWhiteSpace(Description) ? Key : Description;
        }
    }
}
