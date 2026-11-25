using Unity.Cinemachine;
using UnityEngine;

// Lives on CMCam. Each area has its own bounding shape, so the portal calls this
// to swap the confiner over and to stop the camera gliding across the gap between areas.
public class CameraBounds : MonoBehaviour
{
    public CinemachineCamera cam;
    public CinemachineConfiner2D confiner;
    public Collider2D startBounds;

    private void Awake()
    {
        if (startBounds != null) SetBounds(startBounds);
    }

    public void SetBounds(Collider2D shape)
    {
        confiner.BoundingShape2D = shape;
        confiner.InvalidateBoundingShapeCache();
    }

    // call after teleporting the camera's target
    public void Warp(Transform target, Vector3 positionDelta)
    {
        cam.OnTargetObjectWarped(target, positionDelta);
    }
}
