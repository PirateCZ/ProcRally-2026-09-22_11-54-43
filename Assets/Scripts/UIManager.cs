using System;
using Car;
using TMPro;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{ 
    [SerializeField] private TextMeshProUGUI speedometer; 
    [SerializeField] private TextMeshProUGUI rpmGauge;
    [SerializeField] private Engine engine;
    [SerializeField] private Rigidbody vehicleRigidbody;
    private int _speed;
    private int _rpm;
    
    void Start()
    {
        _speed = 0;
        _rpm = 0;
    }

    void Update()
    {
        _speed = Mathf.RoundToInt(vehicleRigidbody.linearVelocity.magnitude * 3.6f);
        _rpm = Mathf.RoundToInt(engine.engineRpm);
        speedometer.text = $"Speed: {_speed} km/h";
        rpmGauge.text = $"RPM: {_rpm}";
    }
}