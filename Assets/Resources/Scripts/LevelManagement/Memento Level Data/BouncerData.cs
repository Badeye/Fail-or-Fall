﻿using FlipFall.Levels;
using FlipFall.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This is the actual Progress Data that is saved and loaded.
/// It consists out of an array of scoreboars, each of them containing the scores for a level.
/// It also holds other variables important for the players progress like achievemnts or settings.
/// </summary>

namespace FlipFall.Levels
{
    [Serializable]
    public class BouncerData
    {
        public Position2 position;
        public Position3 rotation;
        public int bounciness;
        public int width;

        public BouncerData(Position2 pos)
        {
            position = pos;
            bounciness = 0;
            width = 4;
        }
    }
}