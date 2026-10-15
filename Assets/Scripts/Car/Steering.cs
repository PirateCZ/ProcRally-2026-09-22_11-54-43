using UnityEngine;

namespace Car
{
    public class Steering : MonoBehaviour
    {
        public enum SteeringBehavior { Left, Right, Disabled };

        [Header("Inputs")]
        public SteeringBehavior steeringBehavior;

        private float _wheelbase; //Distance between front and rear wheels
        private float _rearTrackLength; //Distance between the left and right rear wheels
        private float _turningRadius; //Search up online or set to your preference to control max steering angle

        [Header("Outputs")]
        public float steerAngle;

        public void Initialize(float argWheelBase, float argRearTrackLength, float argTurningRadius)
        {
            _wheelbase = argWheelBase;
            _rearTrackLength = argRearTrackLength;
            _turningRadius = argTurningRadius;
        }

        public void UpdatePhysics(float steeringInput)
        {
            //Ackermann Equations
            var inner = Mathf.Atan(_wheelbase / (_turningRadius + (_rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;
            var outer = Mathf.Atan(_wheelbase / (_turningRadius - (_rearTrackLength / 2.0f))) * Mathf.Rad2Deg * steeringInput;

            steerAngle = 0.0f;
            if (steeringBehavior != SteeringBehavior.Disabled)
            {
                steerAngle = steeringInput switch
                {
                    //Turning Right
                    > 0.0f => steeringBehavior switch
                    {
                        //Left wheel
                        SteeringBehavior.Left => inner,
                        //Right wheel
                        SteeringBehavior.Right => outer,
                        _ => steerAngle
                    },
                    //Turning Left
                    < 0.0f => steeringBehavior switch
                    {
                        //Left wheel
                        SteeringBehavior.Left => outer,
                        //Right wheel
                        SteeringBehavior.Right => inner,
                        _ => steerAngle
                    },
                    _ => steerAngle
                };
            }

            // IMPORTANT FOR LATERAL FRICTION!! Set the toplink's rotation accordingly; Allows there to be a lateral velocity at the contact patch
            transform.localRotation = Quaternion.Euler(new Vector3(transform.localEulerAngles.x, steerAngle, transform.localEulerAngles.z));
        }
    }
}