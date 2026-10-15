using UnityEngine;

namespace Car
{
   public class Visuals : MonoBehaviour
   {
      private Wheel _wheel;
      private Steering _steering;
      private float _wheelRot;

      public void Initialize(Wheel argWheel, Steering argSteering)
      {
         _wheel = argWheel;
         _steering = argSteering;
      }

      public void UpdateVisuals(float argDeltaTime)
      {
         //Lateral = Wheel Spacers (X); Vertical = Suspension Motion (Y); Longitudinal = Unused (Z)
         transform.localPosition = new Vector3(transform.localPosition.x, _wheel.transform.localPosition.y - _wheel.currentLength, _wheel.transform.localPosition.z);

         //Integrate Wheel Rotation
         _wheelRot += _wheel.wheelAngularVelocity * Mathf.Rad2Deg * argDeltaTime;
         if (Mathf.Abs(_wheelRot) > 360.0f) //Prevent from reaching absurd values
         {
            _wheelRot -= 360.0f * Mathf.Sign(_wheelRot);
         }

         //Roll = Tire Roll (X); Yaw = Steering (Y); Pitch = Camber (Z) NOTE: If you want to have non-zero camber, place an empty at the wheel's origin, parent the wheel to it, and rotate the empty on the Z
         transform.localRotation = Quaternion.Euler(new Vector3(_wheelRot, _steering.steerAngle, 0.0f)); //IMPORTANT: 0.0 fixes gimbal lock
      }
   }
}