using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitalCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0.0f, 1.0f, 0.0f);
    [SerializeField] private float minDistance = 3.0f;
    [SerializeField] private float maxDistance = 12.0f;
    [SerializeField] private float orbitSensitivity = 0.15f;
    [SerializeField] private float zoomSpeed = 2.0f;
    [SerializeField] private float minPitch = -10.0f;
    [SerializeField] private float maxPitch = 70.0f;
    [SerializeField] private float positionSmoothTime = 0.08f;
    [SerializeField] private float rotationSmoothSpeed = 12.0f;

    private float yaw;
    private float pitch;
    private float distance;
    private Vector3 positionVelocity;

    private void Start()
    {
        if (target == null)
        {
            target = transform.parent;
        }

        if (target == null)
        {
            throw new InvalidOperationException("OrbitalCamera requires a target Transform or a parent Transform.");
        }

        Vector3 offset = transform.position - (target.position + targetOffset);
        distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
        if (offset.sqrMagnitude > 0.0001f)
        {
            yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(offset.y / offset.magnitude, -1.0f, 1.0f)) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    private void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 lookDelta = mouse.delta.ReadValue();
                yaw += lookDelta.x * orbitSensitivity;
                pitch = Mathf.Clamp(pitch - lookDelta.y * orbitSensitivity, minPitch, maxPitch);
            }

            distance = Mathf.Clamp(
                distance - mouse.scroll.ReadValue().y * zoomSpeed * Time.unscaledDeltaTime,
                minDistance,
                maxDistance);
        }

        Vector3 focusPoint = target.position + targetOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        Vector3 desiredPosition = focusPoint + orbitRotation * (Vector3.back * distance);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            positionSmoothTime);

        Quaternion desiredRotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationSmoothSpeed * Time.unscaledDeltaTime);
    }
}
