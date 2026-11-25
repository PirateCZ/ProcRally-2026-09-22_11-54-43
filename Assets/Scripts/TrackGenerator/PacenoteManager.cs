using System;
using System.Collections.Generic;
using Car;
using UnityEngine;
using UnityEngine.UI;
using TrackGenerator.TrackGenerator;
using TrackGeneratorComponent = TrackGenerator.TrackGenerator.TrackGenerator;

namespace TrackGenerator
{
    public class PacenoteManager : MonoBehaviour
    {
        [SerializeField] private TrackGeneratorComponent trackGenerator;
        [SerializeField] private Image pacenoteImage;
        [SerializeField] private Transform car;
        [SerializeField] private Sprite straightPacenote;
        [SerializeField] private Sprite sharpLeftPacenote;
        [SerializeField] private Sprite normalLeftPacenote;
        [SerializeField] private Sprite relaxedLeftPacenote;
        [SerializeField] private Sprite sharpRightPacenote;
        [SerializeField] private Sprite normalRightPacenote;
        [SerializeField] private Sprite relaxedRightPacenote;

        private readonly List<Vector3> _pieceCenters = new List<Vector3>();
        private IReadOnlyList<TrackPiece> _pieces = Array.Empty<TrackPiece>();
        private Rigidbody _carRigidbody;
        private int _currentPieceIndex = -1;

        private void Awake()
        {
            if (trackGenerator == null)
            {
                trackGenerator = FindFirstObjectByType<TrackGeneratorComponent>();
            }

            if (trackGenerator == null)
            {
                Debug.LogError("Pacenote Manager needs a Track Generator in the scene.", this);
            }

            if (car == null)
            {
                var vehicle = FindFirstObjectByType<Vehicle>();
                if (vehicle != null)
                {
                    car = vehicle.transform;
                }
            }

            if (car == null)
            {
                Debug.LogError("Pacenote Manager needs the car Transform assigned or a Vehicle in the scene.", this);
            }
            else
            {
                _carRigidbody = car.GetComponentInParent<Rigidbody>();
            }

            if (pacenoteImage == null)
            {
                CreatePacenoteImage();
            }

            ValidatePacenoteSprites();
        }

        private void OnEnable()
        {
            if (trackGenerator != null)
            {
                trackGenerator.TrackGenerated += UpdatePacenotes;
            }
        }

        private void Start()
        {
            if (trackGenerator != null)
            {
                SetTrackPieces(trackGenerator.GeneratedPieces);
            }
        }

        private void Update()
        {
            if (car == null || pacenoteImage == null || _currentPieceIndex < 0 ||
                _currentPieceIndex + 1 >= _pieces.Count)
            {
                return;
            }

            var carPosition = _carRigidbody != null ? _carRigidbody.worldCenterOfMass : car.position;
            var closestPieceIndex = _currentPieceIndex;
            var closestDistance = Vector3.Distance(carPosition, _pieceCenters[_currentPieceIndex]);

            for (var index = _currentPieceIndex + 1; index < _pieceCenters.Count; index++)
            {
                var distance = Vector3.Distance(carPosition, _pieceCenters[index]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPieceIndex = index;
                }
            }

            if (closestPieceIndex > _currentPieceIndex)
            {
                _currentPieceIndex = closestPieceIndex;
                ShowCurrentPacenote();
            }
        }

        private void OnDisable()
        {
            if (trackGenerator != null)
            {
                trackGenerator.TrackGenerated -= UpdatePacenotes;
            }
        }

        private void UpdatePacenotes(IReadOnlyList<TrackPiece> pieces)
        {
            SetTrackPieces(pieces);
        }

        private void SetTrackPieces(IReadOnlyList<TrackPiece> pieces)
        {
            _pieces = pieces;
            _pieceCenters.Clear();
            _currentPieceIndex = pieces.Count == 0 ? -1 : 0;

            foreach (var piece in pieces)
            {
                var renderers = piece.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    var entry = piece.transform.TransformPoint(piece.EntryPoint);
                    var exit = piece.transform.TransformPoint(piece.ExitPoint);
                    _pieceCenters.Add((entry + exit) * 0.5f);
                    continue;
                }

                var bounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }

                _pieceCenters.Add(bounds.center);
            }

            ShowCurrentPacenote();
        }

        private void CreatePacenoteImage()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("Pacenote Manager needs a Canvas to create its image.", this);
                return;
            }

            var imageObject = new GameObject("Pacenote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvas.transform, false);
            var rectTransform = (RectTransform)imageObject.transform;
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -80f);
            rectTransform.sizeDelta = new Vector2(128f, 128f);

            pacenoteImage = imageObject.GetComponent<Image>();
            pacenoteImage.preserveAspect = true;
            pacenoteImage.raycastTarget = false;
        }

        private void ValidatePacenoteSprites()
        {
            var missingSprites = new List<string>();
            AddMissingSprite(missingSprites, nameof(straightPacenote), straightPacenote);
            AddMissingSprite(missingSprites, nameof(sharpLeftPacenote), sharpLeftPacenote);
            AddMissingSprite(missingSprites, nameof(normalLeftPacenote), normalLeftPacenote);
            AddMissingSprite(missingSprites, nameof(relaxedLeftPacenote), relaxedLeftPacenote);
            AddMissingSprite(missingSprites, nameof(sharpRightPacenote), sharpRightPacenote);
            AddMissingSprite(missingSprites, nameof(normalRightPacenote), normalRightPacenote);
            AddMissingSprite(missingSprites, nameof(relaxedRightPacenote), relaxedRightPacenote);

            if (missingSprites.Count > 0)
            {
                Debug.LogError(
                    $"Pacenote Manager is missing sprite assignments: {string.Join(", ", missingSprites)}.",
                    this);
            }
        }

        private static void AddMissingSprite(List<string> missingSprites, string fieldName, Sprite sprite)
        {
            if (sprite == null)
            {
                missingSprites.Add(fieldName);
            }
        }

        private void ShowCurrentPacenote()
        {
            if (pacenoteImage == null)
            {
                return;
            }

            if (_currentPieceIndex < 0)
            {
                pacenoteImage.sprite = null;
                return;
            }

            var pacenoteIndex = Mathf.Min(_currentPieceIndex + 1, _pieces.Count - 1);
            pacenoteImage.sprite = GetPacenoteSprite(_pieces[pacenoteIndex]);
        }

        private Sprite GetPacenoteSprite(TrackPiece piece)
        {
            var pieceName = piece.name.Replace("(Clone)", string.Empty).Trim();
            if (pieceName.StartsWith("RoadStraight", StringComparison.OrdinalIgnoreCase))
            {
                return straightPacenote;
            }

            if (pieceName.EndsWith("1R", StringComparison.OrdinalIgnoreCase)) return sharpRightPacenote;
            if (pieceName.EndsWith("2R", StringComparison.OrdinalIgnoreCase)) return normalRightPacenote;
            if (pieceName.EndsWith("3R", StringComparison.OrdinalIgnoreCase)) return relaxedRightPacenote;
            if (pieceName.EndsWith("1L", StringComparison.OrdinalIgnoreCase)) return sharpLeftPacenote;
            if (pieceName.EndsWith("2L", StringComparison.OrdinalIgnoreCase)) return normalLeftPacenote;
            if (pieceName.EndsWith("3L", StringComparison.OrdinalIgnoreCase)) return relaxedLeftPacenote;

            var entryDirection = Vector3.ProjectOnPlane(
                piece.transform.TransformDirection(-piece.EntryDirection), Vector3.up);
            var exitDirection = Vector3.ProjectOnPlane(
                piece.transform.TransformDirection(piece.ExitDirection), Vector3.up);

            if (entryDirection.sqrMagnitude <= 0.0001f || exitDirection.sqrMagnitude <= 0.0001f)
            {
                return straightPacenote;
            }

            var angle = Vector3.SignedAngle(entryDirection, exitDirection, Vector3.up);
            var turn = Mathf.Abs(angle);
            if (turn < 15f)
            {
                return straightPacenote;
            }

            if (turn >= 60f)
            {
                return angle > 0f ? sharpRightPacenote : sharpLeftPacenote;
            }

            if (turn >= 30f)
            {
                return angle > 0f ? normalRightPacenote : normalLeftPacenote;
            }

            return angle > 0f ? relaxedRightPacenote : relaxedLeftPacenote;
        }
    }
}