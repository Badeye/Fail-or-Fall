﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Impulse.Theme
{
    public class Theme : MonoBehaviour
    {
        public ThemeManager.Skin horizonSkin;

        [Header("Horizon Color Settings")]
        [Tooltip("If useFirstHorizonColor is true, set the camera to this color.")]
        public Color backgorundColor;

        [Tooltip("Ignore backgroundColor and color the background in the firt element's color of horizonColors")]
        public bool useFirstHorizonColor;

        [Tooltip("The seven colors making up the animated background. First element is at the top.")]
        public Color horizonColor1;
        public Color horizonColor2;
        public Color horizonColor3;
        public Color horizonColor4;
        public Color horizonColor5;
        public Color horizonColor6;
        public Color horizonColor7;

        [Header("Gameplay Color Settings")]
        public Color playerColor;
        public Color finishColor;
        public Color turretColor;
        public Color attractorColor;
        public Color moveZoneColor;

        private const int SIZE = 5;
        public int[] ints = new int[SIZE];
    }
}