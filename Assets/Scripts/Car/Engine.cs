using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Car
{
    public class Engine : MonoBehaviour
    {
        [Header("Inputs")]
        public float throttle;

        [Header("Idler Circuit")]
        [Range(0.0f, 1.0f)]
        public float idleThrottle;
        [Range(0.0f, 1.0f)]
        public float throttleStability;
        [FormerlySerializedAs("idleRPM")] public float idleRpm;
        private float _idler;

        [FormerlySerializedAs("redlineRPM")] [Header("Rev Limiter")]
        public float redlineRpm;
        public float throttleCutoffDuration;
        private bool _throttleCut;
        private float _instability;
        private float _timer;

        [Header("Engine Parameters")]
        public AnimationCurve torqueCurve;
        public float starterTorque;
        public float initialFrictionTorque;
        public float frictionLossCoefficient;
        public float inertia;

        [Header("Outputs")]
        public float netTorque;
        [HideInInspector] public float angularVelocity;
        public float engineRpm;

        public void Initialize()
        {
            //Set engine to idle
            angularVelocity = Rpm2Rads(idleRpm);
            engineRpm = Rads2Rpm(angularVelocity);
        }

        public void UpdatePhysics(float argDeltaTime, float throttleInput, float starterInput, float argLoadTorque)
        {
            //Idler Circuit
            _idler = MapRangeClamped(engineRpm, idleRpm - 200.0f, idleRpm + 200.0f, idleThrottle, 0.0f);
            _timer += argDeltaTime;
            if (_timer >= 0.1f) //Update instability every tenth of a second
            {
                _timer = 0.0f;
                _instability = Random.Range(throttleStability, 1.0f);
            }
            _idler *= _instability; //Add throttle instability

            //Rev Limiter
            if (engineRpm >= redlineRpm && !_throttleCut)
            {
                StartCoroutine(ThrottleCutoff());
            }

            //Combine player input, idler circuit, and rev limiter into final throttle value
            throttle = Mathf.MoveTowards(throttle, !_throttleCut ? Mathf.Max(throttleInput, _idler) : 0.0f, argDeltaTime * 10.0f);

            //Calculate engine torque
            var startingTorque = starterInput * starterTorque;
            var grossTorque = torqueCurve.Evaluate(engineRpm) * throttle; //Evaluate the engine's current gross torque output based off the current RPM and throttle input
            var frictionLosses = Mathf.Min(Mathf.Abs(initialFrictionTorque + (engineRpm * frictionLossCoefficient)), Mathf.Abs((angularVelocity / argDeltaTime) * inertia)) * Mathf.Sign(angularVelocity); //loss = constant + (linear * RPM)
            netTorque = (grossTorque + startingTorque) - frictionLosses - argLoadTorque;

            //Integrate engine speed
            var angularAcceleration = netTorque / inertia; //Newton's 2nd law of motion
            angularVelocity += angularAcceleration * argDeltaTime; //Newton's 1st equation of motion
            engineRpm = Rads2Rpm(angularVelocity);
        }

        private IEnumerator ThrottleCutoff()
        {
            _throttleCut = true;
            yield return new WaitForSeconds(throttleCutoffDuration);
            _throttleCut = false;
        }

        private static float Rads2Rpm(float rads) //Rad/s -> RPM
        {
            var rpm = rads * (60.0f / (Mathf.PI * 2.0f));
            return rpm;
        }

        private static float Rpm2Rads(float rpm) //RPM -> Rad/s
        {
            var rads = rpm * ((Mathf.PI * 2.0f) / 60.0f);
            return rads;
        }

        private static float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB) //Maps a value from one range to another
        {
            var result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
            return (result);
        }
    }
}