using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public sealed class ButterflyController : MonoBehaviour
{
    private const float PickRadiusPixels = 20.0f;
    private const float OrbitPerPixel = 0.32f;
    private const float PitchLimit = 80.0f;
    private const float IdleHideDelay = 2.4f;
    private const float EaseSharpness = 6.0f;
    private const float Settled = 1e-4f;

    private static readonly Color DialTrack = new Color(0.86f, 0.87f, 0.88f, 1.0f);
    private static readonly Color DialHandle = Color.white;

    public ButterflyBuilder builder;
    public MeshFilter dialMesh;
    public MeshRenderer dialRenderer;

    public float orbitRadius = 3.4f;
    public float yaw;
    public float pitch = 62.0f;
    public float beatsPerSecond = 1.4f;
    public bool isFlapping = true;

    public Dial[] dials = ButterflyDials.Defaults();

    private float[] shownValues;
    private bool isDraftShown;
    private float beatPhase;
    private int heldDial = -1;
    private bool isTurning;
    private Vector2 lastPointer;
    private float idleSeconds;
    private Mesh dialGeometry;

    private void OnEnable()
    {
        RebuildDials();
        PlaceCamera();
    }

    private void OnValidate()
    {
        RebuildDials();
        PlaceCamera();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EaseButterfly(Time.deltaTime);
        if (isFlapping)
        {
            Flap(Time.deltaTime * beatsPerSecond);
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        HandlePointer(mouse);

        idleSeconds += Time.deltaTime;
        if (dialRenderer != null)
        {
            dialRenderer.enabled = idleSeconds < IdleHideDelay;
        }
    }

    private void HandlePointer(Mouse mouse)
    {
        Vector2 pointer = mouse.position.ReadValue();
        Camera view = GetComponent<Camera>();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            heldDial = dialMesh != null ? ButterflyDials.Pick(dials, dialMesh.transform, view, pointer, PickRadiusPixels) : -1;
            isTurning = heldDial < 0;
            lastPointer = pointer;
        }

        if (mouse.leftButton.isPressed)
        {
            if (heldDial >= 0)
            {
                dials[heldDial].value = ButterflyDials.ValueUnderPointer(dials[heldDial], dialMesh.transform, view, pointer);
                RebuildDials();
            }
            else if (isTurning)
            {
                Vector2 delta = pointer - lastPointer;
                yaw -= delta.x * OrbitPerPixel;
                pitch = Mathf.Clamp(pitch + delta.y * OrbitPerPixel, -PitchLimit, PitchLimit);
                PlaceCamera();
            }

            lastPointer = pointer;
            idleSeconds = 0.0f;
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            heldDial = -1;
            isTurning = false;
        }
    }

    public void PlaceCamera()
    {
        Camera view = GetComponent<Camera>();
        ButterflyCamera.Place(view, yaw, pitch, orbitRadius);
        if (dialMesh != null)
        {
            dialMesh.transform.localPosition = ButterflyDials.RightEdgeOffset(view);
        }
    }

    public void Flap(float beats)
    {
        beatPhase = Mathf.Repeat(beatPhase + beats, 1.0f);
        if (builder != null)
        {
            builder.Pose(WingBeat.ForeLift(beatPhase), WingBeat.HindLift(beatPhase));
        }
    }

    public void ApplyDialsInstantly()
    {
        shownValues = ButterflyDials.Values(dials);
        ShowValues(isDraft: false);
        RebuildDials();
    }

    public void EaseButterfly(float deltaTime)
    {
        if (builder == null)
        {
            return;
        }

        float[] targets = ButterflyDials.Values(dials);
        if (shownValues == null || shownValues.Length != targets.Length)
        {
            shownValues = targets;
            ShowValues(isDraft: false);
            return;
        }

        float blend = 1.0f - Mathf.Exp(-deltaTime * EaseSharpness);
        bool isMoving = false;
        for (int i = 0; i < targets.Length; i++)
        {
            float gap = targets[i] - shownValues[i];
            if (gap == 0.0f)
            {
                continue;
            }

            isMoving = true;
            shownValues[i] = Mathf.Abs(gap) < Settled ? targets[i] : shownValues[i] + gap * blend;
        }

        if (isMoving)
        {
            ShowValues(isDraft: true);
            return;
        }

        if (isDraftShown)
        {
            ShowValues(isDraft: false);
        }
    }

    private void ShowValues(bool isDraft)
    {
        if (builder == null)
        {
            return;
        }

        builder.settings = ButterflyDials.Resolve(shownValues);
        builder.Rebuild(isDraft);
        isDraftShown = isDraft;
        if (isFlapping && Application.isPlaying)
        {
            Flap(0.0f);
        }
    }

    public void RebuildDials()
    {
        if (dialMesh == null)
        {
            return;
        }

        if (dialGeometry == null)
        {
            dialGeometry = new Mesh { name = "ButterflyDials", hideFlags = HideFlags.DontSave };
        }

        MeshBuffer buffer = new MeshBuffer(isShaded: false);
        ButterflyDials.BuildMesh(buffer, dials, DialTrack, DialHandle);
        buffer.WriteTo(dialGeometry);
        dialMesh.sharedMesh = dialGeometry;
    }
}
