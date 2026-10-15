using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Camera[] cameras;

        private int _activeCameraIndex;

        private void Awake()
        {
            if (cameras == null || cameras.Length == 0)
            {
                cameras = GetComponentsInChildren<Camera>(true);
            }

            if (cameras.Length == 0)
            {
                Debug.LogError($"{nameof(CameraManager)} on {name} requires at least one child camera.", this);
                enabled = false;
                return;
            }

            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] == null || !cameras[i].enabled) continue;

                _activeCameraIndex = i;
                break;
            }

            SetActiveCamera(_activeCameraIndex);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f5Key.wasPressedThisFrame)
            {
                SetActiveCamera((_activeCameraIndex + 1) % cameras.Length);
            }
        }

        private void SetActiveCamera(int index)
        {
            _activeCameraIndex = index;
            for (var i = 0; i < cameras.Length; i++)
            {
                var cameraComponent = cameras[i];
                if (cameraComponent == null) continue;

                var isActive = i == _activeCameraIndex;
                cameraComponent.enabled = isActive;

                var followCamera = cameraComponent.GetComponent<FollowCamera>();
                if (followCamera != null)
                {
                    followCamera.enabled = isActive;
                }
            }
        }
    }
}
