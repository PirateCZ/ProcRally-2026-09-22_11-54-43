using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Car
{
    public class Vehicle : MonoBehaviour
    {
        private float _deltaTime;
        private const int Substeps = 100; //(physics stable @ physics freq. * substeps = 5000+ Hz)
        private float _subDeltaTime;

        [SerializeField] private InputActionAsset inputActions;
        private InputActionMap _carActionMap;
        private InputAction _horizontalAction;
        private InputAction _gasAction;
        private InputAction _brakeAction;
        private InputAction _handbrakeAction;
        private InputAction _clutchAction;
        private InputAction _starterAction;
        private InputAction _shiftUpAction;
        private InputAction _shiftDownAction;

        public Engine engine;
        public Clutch clutch;
        public Gearbox gearbox;
        public Differential differential;
        public Wheel[] wheels;
        [FormerlySerializedAs("steerings")] public Steering[] steering;
        public Visuals[] visuals;
        public EngineAudio engineAudio;

        [Header("Inputs")]
        public float throttleInput;
        public float throttleSensitivity;
        public float brakeInput;
        public float handbrakeInput;
        public float clutchInput;
        public float clutchSensitivity;
        public float steeringInput;
        public float steeringSensitivity;
        public float starterInput;
        public bool shiftUpInput;
        public bool shiftDownInput;

        [Header("Dimensions")]
        public float wheelbase;
        public float rearTrackLength;
        public float turningRadius;

        private void OnEnable()
        {
            if (inputActions == null)
            {
                throw new System.InvalidOperationException("Vehicle requires an InputActionAsset.");
            }

            _carActionMap = inputActions.FindActionMap("Car", true);
            _horizontalAction = _carActionMap.FindAction("Horizontal", true);
            _gasAction = _carActionMap.FindAction("Gas", true);
            _brakeAction = _carActionMap.FindAction("Brake", true);
            _handbrakeAction = _carActionMap.FindAction("Handbrake", true);
            _clutchAction = _carActionMap.FindAction("Clutch", true);
            _starterAction = _carActionMap.FindAction("Starter", true);
            _shiftUpAction = _carActionMap.FindAction("ShiftUp", true);
            _shiftDownAction = _carActionMap.FindAction("ShiftDown", true);
            _carActionMap.Enable();
        }

        private void OnDisable()
        {
            _carActionMap?.Disable();
        }

        private void Start()
        {
            for (var i = 0; i < wheels.Length; i++)
            {
                steering[i].Initialize(wheelbase, rearTrackLength, turningRadius);
                visuals[i].Initialize(wheels[i], steering[i]);
            }

            engine.Initialize();
            gearbox.Initialize();
            engineAudio.Initialize();
        }

        private void Update()
        {
            //Update inputs
            throttleInput = Mathf.MoveTowards(throttleInput, _gasAction.IsPressed() ? 1.0f : 0.0f, Time.deltaTime * throttleSensitivity);
            brakeInput = _brakeAction.IsPressed() ? 1.0f : 0.0f;
            handbrakeInput = _handbrakeAction.IsPressed() ? 1.0f : 0.0f;
            clutchInput = Mathf.MoveTowards(clutchInput, _clutchAction.IsPressed() ? 1.0f : 0.0f, Time.deltaTime * clutchSensitivity);
            steeringInput = Mathf.MoveTowards(steeringInput, _horizontalAction.ReadValue<float>(), Time.deltaTime * steeringSensitivity);
            starterInput = _starterAction.IsPressed() ? 1.0f : 0.0f;
            shiftUpInput = _shiftUpAction.WasPressedThisFrame();
            shiftDownInput = _shiftDownAction.WasPressedThisFrame();
            if (shiftUpInput)
            {
                StartCoroutine(gearbox.ShiftUp());
            }
            if (shiftDownInput)
            {
                StartCoroutine(gearbox.ShiftDown());
            }
        }

        private void FixedUpdate()
        {
            _deltaTime = Time.fixedDeltaTime;
            _subDeltaTime = _deltaTime / Substeps;
        
            for (var i = 0; i < wheels.Length; i++)
            {
                wheels[i].UpdatePhysicsPre(_deltaTime);
                steering[i].UpdatePhysics(steeringInput);
            }

            //Drivetrain loop (RWD)
            for (var i = 0; i < Substeps; i++)
            {
                engine.UpdatePhysics(_subDeltaTime, throttleInput, starterInput, clutch.clutchTorque);
                clutch.UpdatePhysics(clutchInput, gearbox.inGear, engine.angularVelocity, gearbox.GetUpstreamAngularVelocity(differential.GetUpstreamAngularVelocity(new Vector2(wheels[2].wheelAngularVelocity, wheels[3].wheelAngularVelocity))));
                gearbox.UpdatePhysics();
                wheels[0].UpdatePhysicsDrivetrain(_subDeltaTime, 0.0f, brakeInput, 0.0f);
                wheels[1].UpdatePhysicsDrivetrain(_subDeltaTime, 0.0f, brakeInput, 0.0f);
                wheels[2].UpdatePhysicsDrivetrain(_subDeltaTime, differential.GetDownstreamTorque(gearbox.GetDownstreamTorque(clutch.clutchTorque)).x, brakeInput, handbrakeInput);
                wheels[3].UpdatePhysicsDrivetrain(_subDeltaTime, differential.GetDownstreamTorque(gearbox.GetDownstreamTorque(clutch.clutchTorque)).y, brakeInput, handbrakeInput);
            }

            //Post-Drivetrain loop
            foreach (var t in wheels)
            {
                t.UpdatePhysicsPost();
            }
        }

        private void LateUpdate()
        {
            //Update misc.
            foreach (var t in visuals)
            {
                t.UpdateVisuals(Time.deltaTime);
            }

            engineAudio.UpdateAudio();
        }
    }
}