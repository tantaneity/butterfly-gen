using UnityEngine;

[RequireComponent(typeof(ButterflyBuilder))]
public sealed class ButterflyFlight : MonoBehaviour
{
    private const float FieldScale = 0.22f;
    private const float FieldDrift = 0.07f;
    private const float SeedSpread = 97.0f;
    private const float Steering = 2.2f;
    private const float HomePull = 1.5f;
    private const float AltitudePull = 2.0f;
    private const float TurnSharpness = 5.0f;
    private const float BankPerTurn = 0.35f;
    private const float MaxBank = 35.0f;
    private const float MaxPitch = 30.0f;
    private const float MinHeadingSpeed = 1e-3f;
    private const float MinAmplitude = 0.72f;
    private const float MaxAmplitude = 1.05f;
    private const float RateJitter = 0.18f;
    private const float GlideChance = 0.14f;
    private const float MinGlideSeconds = 0.35f;
    private const float MaxGlideSeconds = 1.1f;
    private const float GlideLift = 12.0f;
    private const float GlideSink = 0.35f;
    private const float FlapBlendSharpness = 7.0f;
    private const int SeedScale = 7919;

    public float seed;
    public float speed = 1.3f;
    public float beatsPerSecond = 2.2f;
    public float bobHeight = 0.09f;
    public float roamRadius = 3.2f;
    public float altitude = 0.0f;
    public float altitudeRange = 1.2f;

    private Vector3 flightPosition;
    private Vector3 velocity;
    private float heading;
    private float bank;
    private float beatPhase;
    private float elapsed;
    private bool isLaunched;
    private float beatAmplitude = 1.0f;
    private float beatRate = 1.0f;
    private float glideSecondsLeft;
    private float flapWeight = 1.0f;
    private System.Random random;
    private ButterflyBuilder builder;

    private void Update()
    {
        Step(Time.deltaTime);
    }

    public void Launch()
    {
        builder = GetComponent<ButterflyBuilder>();
        flightPosition = transform.position;
        velocity = transform.forward * speed;
        heading = transform.eulerAngles.y;
        beatPhase = Mathf.Repeat(seed * SeedSpread, 1.0f);
        elapsed = 0.0f;
        random = new System.Random(Mathf.RoundToInt(seed * SeedScale));
        isLaunched = true;
    }

    public void Step(float deltaTime)
    {
        if (!isLaunched)
        {
            Launch();
        }

        elapsed += deltaTime;
        AdvanceBeat(deltaTime);
        Vector3 desired = CurlField.Sample(FieldPoint()) * speed + Containment() + Vector3.down * (GlideSink * (1.0f - flapWeight));
        velocity = Vector3.Lerp(velocity, desired, 1.0f - Mathf.Exp(-deltaTime * Steering));
        flightPosition += velocity * deltaTime;

        float strength = beatAmplitude * flapWeight;
        builder.Pose(Mathf.LerpUnclamped(GlideLift, WingBeat.ForeLift(beatPhase), strength),
            Mathf.LerpUnclamped(GlideLift, WingBeat.HindLift(beatPhase), strength));

        float bob = bobHeight * strength * WingBeat.Depth(beatPhase);
        transform.SetPositionAndRotation(flightPosition + Vector3.up * bob, Orient(deltaTime));
    }

    private void AdvanceBeat(float deltaTime)
    {
        glideSecondsLeft = Mathf.Max(0.0f, glideSecondsLeft - deltaTime);
        float targetWeight = glideSecondsLeft > 0.0f ? 0.0f : 1.0f;
        flapWeight = Mathf.Lerp(flapWeight, targetWeight, 1.0f - Mathf.Exp(-deltaTime * FlapBlendSharpness));

        float next = beatPhase + deltaTime * beatsPerSecond * beatRate;
        beatPhase = Mathf.Repeat(next, 1.0f);
        if (next < 1.0f)
        {
            return;
        }

        beatAmplitude = Mathf.Lerp(MinAmplitude, MaxAmplitude, (float)random.NextDouble());
        beatRate = 1.0f + RateJitter * ((float)random.NextDouble() * 2.0f - 1.0f);
        if (glideSecondsLeft <= 0.0f && random.NextDouble() < GlideChance)
        {
            glideSecondsLeft = Mathf.Lerp(MinGlideSeconds, MaxGlideSeconds, (float)random.NextDouble());
        }
    }

    private Vector3 FieldPoint()
    {
        return flightPosition * FieldScale + new Vector3(seed * SeedSpread, 0.0f, elapsed * FieldDrift);
    }

    private Vector3 Containment()
    {
        Vector3 horizontal = new Vector3(flightPosition.x, 0.0f, flightPosition.z);
        float overshoot = Mathf.Max(0.0f, horizontal.magnitude - roamRadius);
        float height = flightPosition.y - altitude;
        float heightOvershoot = Mathf.Sign(height) * Mathf.Max(0.0f, Mathf.Abs(height) - altitudeRange);
        return -horizontal.normalized * (overshoot * HomePull) - Vector3.up * (heightOvershoot * AltitudePull);
    }

    private Quaternion Orient(float deltaTime)
    {
        Vector3 horizontal = new Vector3(velocity.x, 0.0f, velocity.z);
        if (horizontal.magnitude < MinHeadingSpeed || deltaTime <= 0.0f)
        {
            return transform.rotation;
        }

        float targetHeading = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
        float turn = Mathf.DeltaAngle(heading, targetHeading) * (1.0f - Mathf.Exp(-deltaTime * TurnSharpness));
        heading += turn;

        float targetBank = Mathf.Clamp(-turn / deltaTime * BankPerTurn, -MaxBank, MaxBank);
        bank = Mathf.Lerp(bank, targetBank, 1.0f - Mathf.Exp(-deltaTime * TurnSharpness));
        float pitch = Mathf.Clamp(-Mathf.Atan2(velocity.y, horizontal.magnitude) * Mathf.Rad2Deg, -MaxPitch, MaxPitch);
        return Quaternion.Euler(pitch, heading, bank);
    }
}
