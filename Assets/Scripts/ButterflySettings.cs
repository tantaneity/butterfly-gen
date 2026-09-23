using System;
using UnityEngine;

[Serializable]
public sealed class ButterflySettings
{
    public WingSettings fore = WingSettings.Fore();
    public WingSettings hind = WingSettings.Hind();

    public Color ground = Hex(0xd9853b);
    public Color basal = Hex(0xc4702e);
    public Color band = Hex(0x2a2420);
    public Color stripe = Hex(0xefd98a);
    public Color margin = Hex(0x2a2420);
    public Color marginSpot = Hex(0xf3efe6);
    public Color eyeOuter = Hex(0xe8d9a8);
    public Color eyeRing = Hex(0x2a2420);
    public Color eyeIris = Hex(0x5b7fb0);
    public Color eyePupil = Hex(0xf3efe6);
    public Color eyeGlint = Hex(0xdcd2f2);
    public Color vein = Hex(0x2a2420);
    public Color body = Hex(0x2a2420);

    public float seed;
    public float wingLift = 8.0f;
    public float antennaLength = 0.34f;

    public static Color Hex(int hex)
    {
        return new Color(((hex >> 16) & 0xFF) / 255.0f, ((hex >> 8) & 0xFF) / 255.0f, (hex & 0xFF) / 255.0f, 1.0f);
    }
}
