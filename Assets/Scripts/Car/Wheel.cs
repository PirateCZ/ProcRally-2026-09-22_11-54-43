using UnityEngine;

namespace Car
{
    public class Wheel : MonoBehaviour
    {
        private float _deltaTime;
        public Rigidbody vehicleBody;

        [Header("Hit Detection - Inputs")]
        public LayerMask layerMask;
        [Header("Hit Detection - Outputs")]
        public bool isGrounded;

        private RaycastHit _hit;

        [Header("Suspension - Inputs")]
        public float restLength;
        public float springStiffness;
        public float damperStiffness;
        [Header("Suspension - Outputs")]
        public Vector3 fZ;
        public float currentLength;
        private float _lastLength;

        [Header("Wheel Motion - Inputs")] private float _driveTorque;
        public float wheelRadius;
        public float wheelInertia;
        public float brakeTorque = 1000.0f;
        [Header("Wheel Motion - Outputs")]
        public float wheelAngularVelocity;
        public Vector3 linearVelocityLocal;
        private float _totalTorque;
        private Vector3 _angularVelocityLocal;
        private Vector3 _longitudinalDir;
        private Vector3 _lateralDir;

        [Header("Friction - Outputs")]
        public Vector3 fX;
        public Vector3 fY;
        private float _slipAngle;
        private float _muX;
        private float _slipSpeed;
        private float _muY;

        

        public void UpdatePhysicsPre(float argDeltaTime)
        {
            _deltaTime = argDeltaTime;

            isGrounded = Physics.Raycast(transform.position, -transform.up, out _hit, restLength + wheelRadius, layerMask); //Fire a raycast to get the distance between the toplink and the ground

            if (isGrounded) //If we hit something,
            {
                //Calculate and apply the suspension force (Fz)
                currentLength = _hit.distance - wheelRadius;
                CalculateSuspensionForce();
                ApplySuspensionForce();

                //Calculate the wheel's velocity and direction vectors
                GetWheelMotionOnGround();
            }
            else //If we don't,
            {
                //Reset values that need resetting
                ResetValues();
            }
        }

        public void UpdatePhysicsDrivetrain(float argDeltaTime, float argDriveTorque, float argBrakeInput, float argHandbrakeInput)
        {
            _deltaTime = argDeltaTime;
            _driveTorque = argDriveTorque;

            if (isGrounded)
            {
                //Calculate the friction force (Fx, Fy)
                CalculateLateralFriction();
                CalculateLongitudinalFriction(argBrakeInput, argHandbrakeInput);
            }
            else
            {
                //Keep the wheel's ability to spin
                GetWheelMotionInAir();
            }
        }

        public void UpdatePhysicsPost()
        { 
            if (isGrounded)
            {
                //Apply the friction force (Fx, Fy)
                ApplyFrictionForce();
            }
        }

        private void CalculateSuspensionForce()
        {
            //Hooke's Law
            var springDisplacement = restLength - currentLength;
            var springForce = springDisplacement * springStiffness;

            //Damping Equation
            var springVelocity = (_lastLength - currentLength) / _deltaTime;
            var damperForce = springVelocity * damperStiffness;

            var suspensionForce = springForce + damperForce;
            fZ = _hit.normal.normalized * suspensionForce; //Suspension force acts perpendicular to the contact patch

            _lastLength = currentLength; //Set the lastLength for the next frame
        }

        private void ApplySuspensionForce()
        {
            vehicleBody.AddForceAtPosition(fZ, transform.position); //Apply the suspension force to the vehicle at the toplink position
        }

        private void GetWheelMotionOnGround()
        {
            //Get the velocity of the wheel relative to the ground
            linearVelocityLocal = transform.InverseTransformDirection(vehicleBody.GetPointVelocity(_hit.point)); //RB.GetPointVelocity Does Not Update w/ Substeps, If There's A Way To Get This Value Without The Use Of RB Functions, We Can Substep The Whole VP Implementation And Keep The Timestep @ 0.02
            _angularVelocityLocal = linearVelocityLocal / wheelRadius; // omega = v / r

            //Lateral and longitudinal directions of motion of the wheel
            _longitudinalDir = Vector3.ProjectOnPlane(transform.forward, _hit.normal).normalized;
            _lateralDir = Vector3.ProjectOnPlane(transform.right, _hit.normal).normalized;
        }

        private void CalculateLateralFriction()
        {
            //Calculate Wheel Slip (Lateral)
            const float slipAnglePeak = 16.0f; //Pre-Pacejka: Hard-Coded Peak Slip Angle, Should Be Equal To The Peak In Pacejka Curve
            var lowSpeedSlipAngle = slipAnglePeak * Mathf.Sign(-linearVelocityLocal.x) * Mathf.Clamp01(Mathf.Abs(linearVelocityLocal.x)); //Ramp function that mimics slip angle formula at low speeds
            var highSpeedSlipAngle = 0.0f;
            if (linearVelocityLocal.z != 0.0f)
            {
                highSpeedSlipAngle = Mathf.Atan(-linearVelocityLocal.x / Mathf.Abs(linearVelocityLocal.z)) * Mathf.Rad2Deg;
            }
            _slipAngle = Mathf.Lerp(lowSpeedSlipAngle, highSpeedSlipAngle, MapRangeClamped(linearVelocityLocal.magnitude, 3.0f, 6.0f, 0.0f, 1.0f)); //Transition Between Low And High Speed Friction Models Based Off Of Wheel Speed

            //Map Wheel Slip To Friction Curve
            _muX = MapRangeClamped(Mathf.Abs(_slipAngle), 0.0f, slipAnglePeak, 0.0f, 1.0f) * Mathf.Sign(_slipAngle); //Pre-Pacejka
        }

        private void CalculateLongitudinalFriction(float argBrakeInput, float argHandbrakeInput)
        {
            //Calculate Torque Acting On Wheel
            // float driveTorque = throttleInput * motorTorque; //Temp, will come from drivetrain later
            var frictionTorque = _muY * Mathf.Max(fZ.y, 0.0f) * wheelRadius;
            _totalTorque = _driveTorque - frictionTorque;

            //Integrate Angular Velocity
            var wheelAngularAcceleration = _totalTorque / wheelInertia;
            wheelAngularVelocity += wheelAngularAcceleration * _deltaTime;

            if (argHandbrakeInput > 0.0f)
            {
                wheelAngularVelocity = 0.0f;
            }
            else
            {
                var brakeAngularDelta = Mathf.Clamp01(argBrakeInput) * brakeTorque / wheelInertia * _deltaTime;
                wheelAngularVelocity = Mathf.MoveTowards(wheelAngularVelocity, 0.0f, brakeAngularDelta);
            }

            //Calculate Wheel Slip (Longitudinal)
            const float slipSpeedPeak = 8.0f; //Pre-Pacejka: Hard-Coded Peak Slip Speed, Should Be Equal To The Peak In Pacejka Curve
            _slipSpeed = wheelAngularVelocity - _angularVelocityLocal.z;

            //Map Wheel Slip To Friction Curve
            _muY = MapRangeClamped(Mathf.Abs(_slipSpeed), 0.0f, slipSpeedPeak, 0.0f, 1.0f) * Mathf.Sign(_slipSpeed); //Pre-Pacejka
        }

        private void ApplyFrictionForce()
        {
            fX = _lateralDir * (_muX * Mathf.Max(fZ.y, 0.0f)); //F_lat = u * N * -latDir
            fY = _longitudinalDir * (_muY * Mathf.Max(fZ.y, 0.0f)); // F_long = u * N * -longDir
            vehicleBody.AddForceAtPosition(fX + fY, _hit.point); //Apply the friction force at the wheel's contact patch
        }

        private void ResetValues()
        {
            _lastLength = currentLength = restLength; //Fully extend suspension

            _slipAngle = _slipSpeed = 0.0f; //Set wheel slip to zero
            _muX = _muY = 0.0f; //Set friction coefficients to zero
            fX = fY = fZ = Vector3.zero; //Set forces to zero
        }

        private void GetWheelMotionInAir()
        {
            var totalTorque = _driveTorque;

            var wheelAngularAcceleration = totalTorque / wheelInertia;
            wheelAngularVelocity += wheelAngularAcceleration * _deltaTime;
        }

        private static float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB) //Maps a value from one range to another
        {
            var result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
            return (result);
        }
    }
}