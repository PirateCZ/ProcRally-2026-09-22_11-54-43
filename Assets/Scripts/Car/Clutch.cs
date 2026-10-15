using UnityEngine;

public class Clutch : MonoBehaviour
{
    public float clutchTorqueCapacity;
    public float clutchStiffness;
    [Range(0.0f, 1.0f)]
    public float clutchDamping;

    private bool _inGear;
    public float clutchEngagement;
    private float _velocityEngine;
    private float _velocityTransmission;
    public float slip;
    public float clutchTorque;

    public void UpdatePhysics(float clutchInput, bool argInGear, float argVelocityIn, float argVelocityOut)
    {
        _inGear = argInGear;
        clutchEngagement = 1.0f - clutchInput;
        _velocityEngine = argVelocityIn;
        _velocityTransmission = argVelocityOut;
        
        //Calculate slip
        if (_inGear)
        {
            slip = _velocityEngine - _velocityTransmission;
        }
        else
        {
            slip = 0.0f;
        }

        //Calculate torque
        var torque = clutchEngagement * slip * clutchStiffness; //tau = omega * k
        clutchTorque += (torque - clutchTorque) * clutchDamping; //Damping
        clutchTorque = Mathf.Clamp(clutchTorque, -clutchTorqueCapacity, clutchTorqueCapacity); //Make sure it doesn't exceed the torque capacity of the clutch
    }
}