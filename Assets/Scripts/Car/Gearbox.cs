using System.Collections;
using UnityEngine;

namespace Car
{
    public class Gearbox : MonoBehaviour
    {
        public float[] gearRatios;
        public float shiftDuration;
        private int _currentGear;
        private bool _shifting;


        [Header("Outputs")]
        public bool inGear;
        public float currentGearRatio;

        public void Initialize()
        {
            //Be in neutral on startup
            inGear = false;
            _currentGear = 1;
        }

        public void UpdatePhysics()
        {
            //Gear Train
            currentGearRatio = inGear ? gearRatios[_currentGear] : 0.0f;
            if (currentGearRatio == 0.0f)
            {
                inGear = false;
            }
        }
    
        public float GetDownstreamTorque(float argTorque) 
        {
            return argTorque * currentGearRatio;
        }

        public float GetUpstreamAngularVelocity(float argAngularVelocity)
        {
            return argAngularVelocity * currentGearRatio;
        }

        public IEnumerator ShiftUp()
        {
            if ((_currentGear < gearRatios.Length - 1) && (!_shifting)) //If not currently in top gear,
            {
                //Shift to neutral,
                _shifting = true;
                inGear = false;
                int nextGear = _currentGear + 1;
                _currentGear = 1;

                //Wait,
                yield return new WaitForSeconds(shiftDuration);

                //Shift up
                _currentGear = nextGear;
                inGear = true;
                _shifting = false;
            }
        }

        public IEnumerator ShiftDown()
        {
            if((_currentGear > 0) && (!_shifting)) //If not currently in bottom gear,
            {
                //Shift to neutral,
                _shifting = true;
                inGear = false;
                int nextGear = _currentGear - 1;
                _currentGear = 1;

                //Wait,
                yield return new WaitForSeconds(shiftDuration);

                //Shift down
                _currentGear = nextGear;
                inGear = true;
                _shifting = false;
            }
        }
    }
}