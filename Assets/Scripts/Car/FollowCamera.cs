using UnityEngine;

namespace Car
{
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followDistance = 6.5f;
        [SerializeField] private float height = 2.4f;
        [SerializeField] private float lookAheadDistance = 3.0f;
        [SerializeField] private float speedDistance = 0.035f;
        [SerializeField] private float maxSpeed = 60.0f;
        [SerializeField] private float positionSmoothTime = 0.16f;
        [SerializeField] private float rotationSmoothSpeed = 6.0f;
        [SerializeField] private float minFieldOfView = 60.0f;
        [SerializeField] private float maxFieldOfView = 78.0f;
        [SerializeField] private float corneringRoll = 1.2f;
        [SerializeField] private float maxRoll = 7.0f;
        [SerializeField] private float speedShake = 0.025f;

        private Camera _camera;
        private Rigidbody _targetRigidbody;
        private Vector3 _positionVelocity;
        private Vector3 _previousTargetPosition;
        private Vector3 _previousVelocity;
        private Vector3 _velocity;
        private Vector3 _acceleration;
        private Quaternion _heading;
        private bool _initialized;

        private void Awake()
        {
            if (target == null)
            {
                target = transform.parent;
            }

            if (target == null)
            {
                Debug.LogError($"{nameof(FollowCamera)} on {name} requires a target or parent Transform.", this);
                enabled = false;
                return;
            }

            _camera = GetComponent<Camera>();
            _targetRigidbody = target.GetComponentInParent<Rigidbody>();
            _previousTargetPosition = target.position;
            _heading = target.rotation;
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized) return;

            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0.0f) return;

            _velocity = _targetRigidbody != null
                ? _targetRigidbody.linearVelocity
                : (target.position - _previousTargetPosition) / deltaTime;
            _acceleration = (_velocity - _previousVelocity) / deltaTime;
            _previousVelocity = _velocity;
            _previousTargetPosition = target.position;

            var planarVelocity = Vector3.ProjectOnPlane(_velocity, Vector3.up);
            var speed = planarVelocity.magnitude;
            var speedRatio = maxSpeed > 0.0f ? Mathf.Clamp01(speed / maxSpeed) : 0.0f;
            var desiredForward = speed > 1.0f ? planarVelocity.normalized : target.forward;
            var desiredHeading = Quaternion.LookRotation(desiredForward, Vector3.up);
            _heading = Quaternion.Slerp(_heading, desiredHeading, rotationSmoothSpeed * deltaTime);

            var distance = followDistance + speed * speedDistance;
            var focusPoint = target.position + Vector3.up * (height * 0.45f);
            var desiredPosition = focusPoint + _heading * new Vector3(0.0f, height * 0.55f, -distance);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                positionSmoothTime);

            var shake = speedShake * speedRatio;
            var noiseTime = Time.time * 18.0f;
            transform.position += new Vector3(
                (Mathf.PerlinNoise(noiseTime, 0.0f) - 0.5f) * shake,
                (Mathf.PerlinNoise(0.0f, noiseTime) - 0.5f) * shake,
                0.0f);

            var lookPoint = focusPoint + _heading * Vector3.forward * lookAheadDistance;
            var desiredRotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            var lateralAcceleration = Vector3.Dot(_acceleration, _heading * Vector3.right);
            var roll = Mathf.Clamp(-lateralAcceleration * corneringRoll, -maxRoll, maxRoll);
            desiredRotation *= Quaternion.Euler(0.0f, 0.0f, roll);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationSmoothSpeed * deltaTime);

            if (_camera != null)
            {
                _camera.fieldOfView = Mathf.Lerp(minFieldOfView, maxFieldOfView, speedRatio);
            }
        }
    }
}
