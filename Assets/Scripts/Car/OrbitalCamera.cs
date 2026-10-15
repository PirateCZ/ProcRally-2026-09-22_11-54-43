using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
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

        private float _yaw;
        private float _pitch;
        private float _distance;
        private Vector3 _positionVelocity;

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

            var offset = transform.position - (target.position + targetOffset);
            _distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
            if (!(offset.sqrMagnitude > 0.0001f)) return;
            _yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Asin(Mathf.Clamp(offset.y / offset.magnitude, -1.0f, 1.0f)) * Mathf.Rad2Deg;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var lookDelta = mouse.delta.ReadValue();
                    _yaw += lookDelta.x * orbitSensitivity;
                    _pitch = Mathf.Clamp(_pitch - lookDelta.y * orbitSensitivity, minPitch, maxPitch);
                }

                _distance = Mathf.Clamp(
                    _distance - mouse.scroll.ReadValue().y * zoomSpeed * Time.unscaledDeltaTime,
                    minDistance,
                    maxDistance);
            }

            var focusPoint = target.position + targetOffset;
            var orbitRotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
            var desiredPosition = focusPoint + orbitRotation * (Vector3.back * _distance);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                positionSmoothTime);

            var desiredRotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationSmoothSpeed * Time.unscaledDeltaTime);
        }
    }
}
