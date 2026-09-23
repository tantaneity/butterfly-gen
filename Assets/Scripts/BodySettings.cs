using System;
using UnityEngine;

[Serializable]
public sealed class BodySettings
{
    public Color colour = ButterflySettings.Hex(0x2a2420);
    public Color accent = ButterflySettings.Hex(0x2a2420);
    public float bandStrength;
    public float abdomenLength = 0.3f;
    public float abdomenWidth = 0.042f;
    public float thoraxLength = 0.15f;
    public float thoraxWidth = 0.05f;
    public float headRadius = 0.032f;
    public float antennaLength = 0.34f;
    public float antennaSpread = 24.0f;
    public float antennaSplay = 0.7f;
    public float clubWidth = 0.011f;
    public float clubStart = 0.78f;
}
