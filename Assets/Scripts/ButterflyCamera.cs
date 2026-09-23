using UnityEngine;

public static class ButterflyCamera
{
    public const float FieldOfView = 30.0f;

    public static void Place(Camera camera, float yaw, float pitch, float radius)
    {
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0.0f);
        camera.transform.position = orbit * new Vector3(0.0f, 0.0f, -radius);
        camera.transform.rotation = orbit;
    }
}
