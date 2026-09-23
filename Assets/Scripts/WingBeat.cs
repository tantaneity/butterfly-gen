using UnityEngine;

public static class WingBeat
{
    private const float DownstrokeShare = 0.42f;
    private const float TopLift = 70.0f;
    private const float BottomLift = -25.0f;
    private const float HindLag = 0.025f;

    public static float ForeLift(float phase)
    {
        return Lift(phase);
    }

    public static float HindLift(float phase)
    {
        return Lift(phase - HindLag);
    }

    private static float Lift(float phase)
    {
        float cycle = Mathf.Repeat(phase, 1.0f);
        if (cycle < DownstrokeShare)
        {
            return Mathf.Lerp(TopLift, BottomLift, Mathf.SmoothStep(0.0f, 1.0f, cycle / DownstrokeShare));
        }

        float upstroke = (cycle - DownstrokeShare) / (1.0f - DownstrokeShare);
        return Mathf.Lerp(BottomLift, TopLift, Mathf.SmoothStep(0.0f, 1.0f, upstroke));
    }
}
