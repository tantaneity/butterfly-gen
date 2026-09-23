using System;
using System.Reflection;
using UnityEngine;

public static class ButterflySpecies
{
    public static readonly string[] Names = { "monarch", "peacock", "swallowtail", "morpho", "zebra longwing", "luna moth" };

    public static int Count => Names.Length;

    public static ButterflySettings Preset(int index)
    {
        switch (Mathf.Clamp(index, 0, Count - 1))
        {
            case 1: return Peacock();
            case 2: return Swallowtail();
            case 3: return Morpho();
            case 4: return ZebraLongwing();
            case 5: return LunaMoth();
            default: return Monarch();
        }
    }

    public static ButterflySettings At(float position)
    {
        int lower = Mathf.Clamp(Mathf.FloorToInt(position), 0, Count - 1);
        int upper = Mathf.Min(lower + 1, Count - 1);
        return Lerp(Preset(lower), Preset(upper), Mathf.Clamp01(position - lower));
    }

    public static T Lerp<T>(T from, T to, float t) where T : class, new()
    {
        return (T)Blend(from, to, t, typeof(T));
    }

    private static object Blend(object from, object to, float t, Type type)
    {
        object blended = Activator.CreateInstance(type);
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            field.SetValue(blended, BlendValue(field.GetValue(from), field.GetValue(to), t, field.FieldType));
        }

        return blended;
    }

    private static object BlendValue(object from, object to, float t, Type type)
    {
        if (type == typeof(float))
        {
            return Mathf.Lerp((float)from, (float)to, t);
        }

        if (type == typeof(Color))
        {
            return Color.Lerp((Color)from, (Color)to, t);
        }

        if (type == typeof(float[]))
        {
            return BlendArray((float[])from, (float[])to, t);
        }

        if (type == typeof(WingSettings))
        {
            return Blend(from, to, t, type);
        }

        return t < 0.5f ? from : to;
    }

    private static float[] BlendArray(float[] from, float[] to, float t)
    {
        if (from.Length != to.Length)
        {
            throw new ArgumentException($"cannot blend arrays of length {from.Length} and {to.Length}");
        }

        float[] blended = new float[from.Length];
        for (int i = 0; i < blended.Length; i++)
        {
            blended[i] = Mathf.Lerp(from[i], to[i], t);
        }

        return blended;
    }

    private static ButterflySettings Monarch()
    {
        ButterflySettings monarch = new ButterflySettings();
        monarch.fore.veinWidth = 0.011f;
        monarch.fore.marginWidth = 0.1f;
        monarch.fore.apexPatch = 0.2f;
        monarch.fore.marginSpotSize = 0.011f;
        monarch.hind.veinWidth = 0.013f;
        monarch.hind.marginWidth = 0.12f;
        monarch.hind.marginSpotSize = 0.012f;
        return monarch;
    }

    private static ButterflySettings Peacock()
    {
        ButterflySettings peacock = new ButterflySettings
        {
            ground = ButterflySettings.Hex(0xb5463c), basal = ButterflySettings.Hex(0x6e3a30),
            margin = ButterflySettings.Hex(0x5a3a30), vein = ButterflySettings.Hex(0x8a3d33),
            eyeOuter = ButterflySettings.Hex(0xe8d9a8), eyeRing = ButterflySettings.Hex(0x2a2420),
            eyeIris = ButterflySettings.Hex(0x5b7fb0), eyePupil = ButterflySettings.Hex(0x2a2420)
        };
        peacock.fore.basalReach = 0.28f;
        peacock.fore.marginWidth = 0.06f;
        peacock.fore.eyeSize = 0.2f;
        peacock.fore.eyeReach = 0.74f;
        peacock.fore.eyeCells[2] = 1.0f;
        peacock.fore.scallopDepth = 0.05f;
        peacock.hind.basalReach = 0.35f;
        peacock.hind.marginWidth = 0.08f;
        peacock.hind.eyeSize = 0.3f;
        peacock.hind.eyeReach = 0.6f;
        peacock.hind.eyeCells[3] = 1.0f;
        peacock.hind.scallopDepth = 0.08f;
        return peacock;
    }

    private static ButterflySettings Swallowtail()
    {
        ButterflySettings swallowtail = new ButterflySettings
        {
            ground = ButterflySettings.Hex(0xecd78a), basal = ButterflySettings.Hex(0x2a2420),
            band = ButterflySettings.Hex(0x6f8fc0), margin = ButterflySettings.Hex(0x2a2420),
            marginSpot = ButterflySettings.Hex(0xecd78a), vein = ButterflySettings.Hex(0x2a2420),
            eyeOuter = ButterflySettings.Hex(0x2a2420), eyeRing = ButterflySettings.Hex(0xc24a3c),
            eyeIris = ButterflySettings.Hex(0xc24a3c), eyePupil = ButterflySettings.Hex(0x2a2420)
        };
        swallowtail.fore.basalReach = 0.25f;
        swallowtail.fore.veinWidth = 0.007f;
        swallowtail.fore.marginWidth = 0.2f;
        swallowtail.fore.apexPatch = 0.08f;
        swallowtail.fore.marginSpotSize = 0.02f;
        swallowtail.hind.basalReach = 0.3f;
        swallowtail.hind.veinWidth = 0.008f;
        swallowtail.hind.marginWidth = 0.26f;
        swallowtail.hind.marginSpotSize = 0.024f;
        swallowtail.hind.bandCenter = 0.8f;
        swallowtail.hind.bandWidth = 0.1f;
        swallowtail.hind.tailLength = 0.42f;
        swallowtail.hind.tailPosition = 0.62f;
        swallowtail.hind.tailWidth = 0.035f;
        swallowtail.hind.scallopDepth = 0.07f;
        swallowtail.hind.eyeSize = 0.08f;
        swallowtail.hind.eyeReach = 0.8f;
        swallowtail.hind.eyeCells[6] = 1.0f;
        return swallowtail;
    }

    private static ButterflySettings Morpho()
    {
        ButterflySettings morpho = new ButterflySettings
        {
            ground = ButterflySettings.Hex(0x5a8fd0), basal = ButterflySettings.Hex(0x3f6aa8),
            margin = ButterflySettings.Hex(0x2a2420), marginSpot = ButterflySettings.Hex(0xf3efe6),
            vein = ButterflySettings.Hex(0x3f6aa8)
        };
        morpho.fore.radii = new[] { 0.9f, 1.0f, 1.0f, 0.96f, 0.88f, 0.76f, 0.66f };
        morpho.fore.basalReach = 0.2f;
        morpho.fore.veinWidth = 0.002f;
        morpho.fore.marginWidth = 0.14f;
        morpho.fore.apexPatch = 0.22f;
        morpho.fore.marginSpotSize = 0.01f;
        morpho.hind.radii = new[] { 0.85f, 1.0f, 1.04f, 1.02f, 0.96f, 0.84f, 0.62f };
        morpho.hind.basalReach = 0.2f;
        morpho.hind.veinWidth = 0.002f;
        morpho.hind.marginWidth = 0.12f;
        morpho.hind.marginSpotSize = 0.01f;
        morpho.hind.scallopDepth = 0.06f;
        return morpho;
    }

    private static ButterflySettings ZebraLongwing()
    {
        ButterflySettings zebra = new ButterflySettings
        {
            ground = ButterflySettings.Hex(0x2a2420), basal = ButterflySettings.Hex(0x2a2420),
            band = ButterflySettings.Hex(0xefd98a), stripe = ButterflySettings.Hex(0xefd98a),
            margin = ButterflySettings.Hex(0x2a2420), vein = ButterflySettings.Hex(0x2a2420),
            antennaLength = 0.42f
        };
        zebra.fore.startAngle = 64.0f;
        zebra.fore.endAngle = 116.0f;
        zebra.fore.length = 0.7f;
        zebra.fore.radii = new[] { 0.95f, 1.0f, 0.9f, 0.76f, 0.62f, 0.54f, 0.5f };
        zebra.fore.stripeWidth = 0.42f;
        zebra.fore.stripeStart = 0.12f;
        zebra.fore.stripeEnd = 0.88f;
        zebra.hind.startAngle = 108.0f;
        zebra.hind.endAngle = 162.0f;
        zebra.hind.length = 0.4f;
        zebra.hind.bandCenter = 0.55f;
        zebra.hind.bandWidth = 0.16f;
        zebra.hind.bandTilt = 0.3f;
        return zebra;
    }

    private static ButterflySettings LunaMoth()
    {
        ButterflySettings luna = new ButterflySettings
        {
            ground = ButterflySettings.Hex(0xc7dcae), basal = ButterflySettings.Hex(0xd8e8c6),
            margin = ButterflySettings.Hex(0x9aae84), vein = ButterflySettings.Hex(0xa9bf92),
            eyeOuter = ButterflySettings.Hex(0xc9a24e), eyeRing = ButterflySettings.Hex(0x6e5a6a),
            eyeIris = ButterflySettings.Hex(0xd9c27a), eyePupil = ButterflySettings.Hex(0xe8eedc),
            body = ButterflySettings.Hex(0xe8e2d0), antennaLength = 0.22f
        };
        luna.fore.radii = new[] { 0.96f, 1.0f, 0.92f, 0.82f, 0.72f, 0.64f, 0.58f };
        luna.fore.basalReach = 0.18f;
        luna.fore.veinWidth = 0.003f;
        luna.fore.marginWidth = 0.03f;
        luna.fore.eyeSize = 0.075f;
        luna.fore.eyeReach = 0.46f;
        luna.fore.eyeCells[4] = 1.0f;
        luna.hind.basalReach = 0.2f;
        luna.hind.veinWidth = 0.003f;
        luna.hind.marginWidth = 0.03f;
        luna.hind.tailLength = 0.95f;
        luna.hind.tailPosition = 0.56f;
        luna.hind.tailWidth = 0.07f;
        luna.hind.eyeSize = 0.09f;
        luna.hind.eyeReach = 0.46f;
        luna.hind.eyeCells[3] = 1.0f;
        return luna;
    }
}
