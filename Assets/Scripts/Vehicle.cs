using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Vehicle : MonoBehaviour
{
    float deltaTime;
    const int SUBSTEPS = 100; //(physics stable @ physics freq. * substeps = 5000+ Hz)
    float subDeltaTime;

    [SerializeField] private InputActionAsset inputActions;
    private InputActionMap carActionMap;
    private InputAction horizontalAction;
    private InputAction verticalAction;
    private InputAction clutchAction;
    private InputAction starterAction;
    private InputAction shiftUpAction;
    private InputAction shiftDownAction;

    public Engine engine;
    public Clutch clutch;
    public Gearbox gearbox;
    public Differential differential;
    public Wheel[] wheels;
    public Steering[] steerings;
    public Visuals[] visuals;
    public EngineAudio engineAudio;

    [Header("Inputs")]
    public float throttleInput;
    public float throttleSensitivity;
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

    void OnEnable()
    {
        if (inputActions == null)
        {
            throw new System.InvalidOperationException("Vehicle requires an InputActionAsset.");
        }

        carActionMap = inputActions.FindActionMap("Car", true);
        horizontalAction = carActionMap.FindAction("Horizontal", true);
        verticalAction = carActionMap.FindAction("Vertical", true);
        clutchAction = carActionMap.FindAction("Clutch", true);
        starterAction = carActionMap.FindAction("Starter", true);
        shiftUpAction = carActionMap.FindAction("ShiftUp", true);
        shiftDownAction = carActionMap.FindAction("ShiftDown", true);
        carActionMap.Enable();
    }

    void OnDisable()
    {
        if (carActionMap != null)
        {
            carActionMap.Disable();
        }
    }

    void Start()
    {
        for (int i = 0; i < wheels.Length; i++)
        {
            steerings[i].Initialize(wheelbase, rearTrackLength, turningRadius);
            visuals[i].Initialize(wheels[i], steerings[i]);
        }

        engine.Initialize();
        gearbox.Initialize();
        engineAudio.Initialize();
    }

    void Update()
    {
        //Update inputs
        throttleInput = Mathf.MoveTowards(throttleInput, Mathf.Max(verticalAction.ReadValue<float>(), 0.0f), Time.deltaTime * throttleSensitivity);
        clutchInput = Mathf.MoveTowards(clutchInput, clutchAction.IsPressed() ? 1.0f : 0.0f, Time.deltaTime * clutchSensitivity);
        steeringInput = Mathf.MoveTowards(steeringInput, horizontalAction.ReadValue<float>(), Time.deltaTime * steeringSensitivity);
        starterInput = starterAction.IsPressed() ? 1.0f : 0.0f;
        shiftUpInput = shiftUpAction.WasPressedThisFrame();
        shiftDownInput = shiftDownAction.WasPressedThisFrame();
        if (shiftUpInput)
        {
            StartCoroutine(gearbox.ShiftUp());
        }
        if (shiftDownInput)
        {
            StartCoroutine(gearbox.ShiftDown());
        }
    }

    void FixedUpdate()
    {
        deltaTime = Time.fixedDeltaTime;
        subDeltaTime = deltaTime / (float)SUBSTEPS;
        
        //Pre-Drivetrain loop
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].UpdatePhysicsPre(deltaTime);
            steerings[i].UpdatePhysics(steeringInput);
        }

        //Drivetrain loop (RWD)
        for (int i = 0; i < SUBSTEPS; i++)
        {
            engine.UpdatePhysics(subDeltaTime, throttleInput, starterInput, clutch.clutchTorque);
            clutch.UpdatePhysics(clutchInput, gearbox.inGear, engine.angularVelocity, gearbox.GetUpstreamAngularVelocity(differential.GetUpstreamAngularVelocity(new Vector2(wheels[2].wheelAngularVelocity, wheels[3].wheelAngularVelocity))));
            gearbox.UpdatePhysics();
            wheels[0].UpdatePhysicsDrivetrain(subDeltaTime, 0.0f);
            wheels[1].UpdatePhysicsDrivetrain(subDeltaTime, 0.0f);
            wheels[2].UpdatePhysicsDrivetrain(subDeltaTime, differential.GetDownstreamTorque(gearbox.GetDownstreamTorque(clutch.clutchTorque)).x);
            wheels[3].UpdatePhysicsDrivetrain(subDeltaTime, differential.GetDownstreamTorque(gearbox.GetDownstreamTorque(clutch.clutchTorque)).y);
        }

        //Post-Drivetrain loop
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].UpdatePhysicsPost();
        }
    }

    void LateUpdate()
    {
        //Update misc.
        for (int i = 0; i < visuals.Length; i++)
        {
            visuals[i].UpdateVisuals(Time.deltaTime);
        }
        engineAudio.UpdateAudio();
    }
}