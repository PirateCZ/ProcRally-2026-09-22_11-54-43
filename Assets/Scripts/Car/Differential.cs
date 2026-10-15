using UnityEngine;

namespace Car
{
    public class Differential : MonoBehaviour
    {
        public float finalDriveRatio;
        
        //Open Differential
        public Vector2 GetDownstreamTorque(float argTorqueIn) 
        {
            return new Vector2(argTorqueIn * finalDriveRatio * 0.5f, argTorqueIn * finalDriveRatio * 0.5f);
        }

        public float GetUpstreamAngularVelocity(Vector2 argAngularVelocityIn)
        {
            return (argAngularVelocityIn.x + argAngularVelocityIn.y) * finalDriveRatio * 0.5f;
        }
   
    }
}