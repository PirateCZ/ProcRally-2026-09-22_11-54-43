using System.Collections;
using System;
using Car;
using TMPro;
using Unity.AppUI.Navigation;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speedometer;
    [SerializeField] private TextMeshProUGUI rpmGauge;
    [SerializeField] private TextMeshProUGUI gearSelector;
    [SerializeField] private Engine engine;
    [SerializeField] private Gearbox gearbox;
    [SerializeField] private Rigidbody vehicleRigidbody;
    [SerializeField] private GameObject screenOverlay;
    [SerializeField] private Sprite overlaySprite;
    [SerializeField] private float overlayDuration = 5f;
    private Image _screenOverlayImage;
    private int _speed;
    private int _rpm;
    private Coroutine _hideOverlayCoroutine;

    private void Awake()
    {
        if (screenOverlay == null)
        {
            Debug.LogError("UIManager needs a Screen Overlay object assigned in the Inspector.", this);
            return;
        }

        _screenOverlayImage = screenOverlay.GetComponent<Image>();
        if (_screenOverlayImage == null)
        {
            Debug.LogError("The assigned Screen Overlay object needs an Image component.", screenOverlay);
            return;
        }

        if (overlaySprite != null)
        {
            _screenOverlayImage.sprite = overlaySprite;
        }

        _screenOverlayImage.color = new Color(1f, 1f, 1f, 1f);
        _screenOverlayImage.raycastTarget = false;
        screenOverlay.SetActive(false);
    }

    void Start()
    {
        _speed = 0;
        _rpm = 0;
    }

    public void ShowScreenOverlay()
    {
        if (screenOverlay == null || _screenOverlayImage == null)
        {
            Debug.LogError("UIManager has no Screen Overlay object assigned.", this);
            return;
        }

        Color overlayColor = _screenOverlayImage.color;
        overlayColor.a = 1f;
        _screenOverlayImage.color = overlayColor;
        screenOverlay.SetActive(true);

        if (_hideOverlayCoroutine != null)
        {
            StopCoroutine(_hideOverlayCoroutine);
        }

        _hideOverlayCoroutine = StartCoroutine(FadeOutScreenOverlay());
    }

    private IEnumerator FadeOutScreenOverlay()
    {
        float elapsed = 0f;
        while (elapsed < overlayDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / overlayDuration);
            Color overlayColor = _screenOverlayImage.color;
            overlayColor.a = 1f - normalizedTime * normalizedTime;
            _screenOverlayImage.color = overlayColor;
            yield return null;
        }

        screenOverlay.SetActive(false);
        _hideOverlayCoroutine = null;
    }

    void Update()
    {
        _speed = Mathf.RoundToInt(vehicleRigidbody.linearVelocity.magnitude * 3.6f);
        _rpm = Mathf.RoundToInt(engine.engineRpm);
        speedometer.text = $"Speed: {_speed} km/h";
        rpmGauge.text = $"RPM: {_rpm}";
        gearSelector.text = $"Gear: {ResolveGear(gearbox.CurrentGear)}";
    }

    private string ResolveGear(int gear)
    {
        if (gear == 1)
        {
            return "N";
        }
        else if (gear <= 0)
        {
            return "R";
        }
        else
        {
            return (gear - 1).ToString();
        }
    }
}