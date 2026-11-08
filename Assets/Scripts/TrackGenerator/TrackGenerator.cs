using System.Collections.Generic;
using UnityEngine;

namespace TrackGenerator.TrackGenerator
{
    public class TrackGenerator : MonoBehaviour
    {
        private const int MaxPlacementAttempts = 20000;

        [SerializeField] private TrackPiece[] availablePieces;
        [SerializeField] private int pieceCount = 20;
        [SerializeField] private int seed;
        [SerializeField] private Vector3 startDirection = Vector3.forward;
        [SerializeField] private bool generateOnStart = true;
        [SerializeField, HideInInspector] private Transform generatedRoot;

        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        [ContextMenu("Generate Track")]
        public void Generate()
        {
            if (availablePieces == null || availablePieces.Length == 0)
            {
                Debug.LogError("Track Generator needs at least one piece prefab.", this);
                return;
            }

            if (pieceCount < 1)
            {
                Debug.LogError("Track Generator piece count must be at least one.", this);
                return;
            }

            if (startDirection.sqrMagnitude <= 0.0001f)
            {
                Debug.LogError("Track Generator start direction cannot be zero.", this);
                return;
            }

            var validPieces = GetValidPieces();
            if (validPieces.Count == 0)
            {
                Debug.LogError("Track Generator has no piece prefabs with valid entry and exit directions.", this);
                return;
            }

            ClearGeneratedTrack();
            EnsureGeneratedRoot();

            var random = new System.Random(seed == 0 ? Random.Range(1, int.MaxValue) : seed);
            var placedPieces = new List<TrackPiece>(pieceCount);
            var attempts = 0;
            var completed = TryGenerateTrack(
                validPieces,
                placedPieces,
                transform.position,
                -transform.TransformDirection(startDirection.normalized),
                random,
                ref attempts);

            if (!completed)
            {
                ClearGeneratedTrack();
                Debug.LogError(
                    $"Could not generate a non-intersecting track of {pieceCount} pieces after {attempts} placement attempts. " +
                    "Check that the road colliders and connection points allow a clear route.",
                    this);
            }
        }

        private bool TryGenerateTrack(
            List<TrackPiece> validPieces,
            List<TrackPiece> placedPieces,
            Vector3 entryPosition,
            Vector3 entryDirection,
            System.Random random,
            ref int attempts)
        {
            if (placedPieces.Count >= pieceCount)
            {
                return true;
            }

            var candidatePrefabs = new List<TrackPiece>(validPieces);
            Shuffle(candidatePrefabs, random);
            foreach (var prefab in candidatePrefabs)
            {
                if (attempts >= MaxPlacementAttempts)
                {
                    return false;
                }

                attempts++;
                var candidate = PlacePiece(prefab, entryPosition, entryDirection, out var exitPosition,
                    out var exitDirection);

                if (IntersectsTrack(candidate, placedPieces))
                {
                    RemovePiece(candidate);
                    continue;
                }

                placedPieces.Add(candidate);
                if (TryGenerateTrack(
                        validPieces,
                        placedPieces,
                        exitPosition,
                        -exitDirection,
                        random,
                        ref attempts))
                {
                    return true;
                }

                placedPieces.RemoveAt(placedPieces.Count - 1);
                RemovePiece(candidate);
            }

            return false;
        }

        private static void Shuffle(List<TrackPiece> pieces, System.Random random)
        {
            for (var index = pieces.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (pieces[index], pieces[swapIndex]) = (pieces[swapIndex], pieces[index]);
            }
        }

        private static bool IntersectsTrack(TrackPiece candidate, List<TrackPiece> placedPieces)
        {
            var candidateColliders = candidate.GetComponentsInChildren<Collider>();
            for (var pieceIndex = 0; pieceIndex < placedPieces.Count - 1; pieceIndex++)
            {
                var existingColliders = placedPieces[pieceIndex].GetComponentsInChildren<Collider>();
                foreach (var candidateCollider in candidateColliders)
                {
                    if (!candidateCollider.enabled || !candidateCollider.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    foreach (var existingCollider in existingColliders)
                    {
                        if (!existingCollider.enabled || !existingCollider.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        if (Physics.ComputePenetration(
                                candidateCollider,
                                candidateCollider.transform.position,
                                candidateCollider.transform.rotation,
                                existingCollider,
                                existingCollider.transform.position,
                                existingCollider.transform.rotation,
                                out _,
                                out _))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void RemovePiece(TrackPiece piece)
        {
            if (Application.isPlaying)
            {
                piece.gameObject.SetActive(false);
                Destroy(piece.gameObject);
            }
            else
            {
                DestroyImmediate(piece.gameObject);
            }
        }

        private List<TrackPiece> GetValidPieces()
        {
            var result = new List<TrackPiece>(availablePieces.Length);
            foreach (var piece in availablePieces)
            {
                if (piece == null)
                {
                    Debug.LogError("Track Generator's piece list contains an empty entry.", this);
                    continue;
                }

                if (!piece.HasValidConnections)
                {
                    Debug.LogError($"Track piece '{piece.name}' needs non-zero entry and exit directions.", piece);
                    continue;
                }

                if (piece.GetComponentsInChildren<Collider>().Length == 0)
                {
                    Debug.LogError(
                        $"Track piece '{piece.name}' needs colliders so track intersections can be detected.", piece);
                    continue;
                }

                result.Add(piece);
            }

            return result;
        }

        private TrackPiece PlacePiece(
            TrackPiece prefab,
            Vector3 targetEntryPosition,
            Vector3 targetEntryDirection,
            out Vector3 exitPosition,
            out Vector3 exitDirection)
        {
            var entryDirection = prefab.EntryDirection;
            var directionsAreOppositeAndFlat =
                Vector3.Dot(entryDirection, targetEntryDirection) < -0.9999f &&
                Mathf.Abs(entryDirection.y) < 0.0001f &&
                Mathf.Abs(targetEntryDirection.y) < 0.0001f;
            var rotation = directionsAreOppositeAndFlat
                ? Quaternion.AngleAxis(180f, Vector3.up)
                : Quaternion.FromToRotation(entryDirection, targetEntryDirection);
            var position = targetEntryPosition - rotation * prefab.EntryPoint;
            var instance = Instantiate(prefab, position, rotation, generatedRoot);

            exitPosition = instance.transform.TransformPoint(instance.ExitPoint);
            exitDirection = instance.transform.TransformDirection(instance.ExitDirection).normalized;
            return instance;
        }

        private void EnsureGeneratedRoot()
        {
            if (generatedRoot != null)
            {
                return;
            }

            var root = new GameObject($"{name} Generated Track");
            generatedRoot = root.transform;
            generatedRoot.SetParent(transform, true);
        }

        private void ClearGeneratedTrack()
        {
            if (generatedRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedRoot.gameObject);
            }
            else
            {
                DestroyImmediate(generatedRoot.gameObject);
            }

            generatedRoot = null;
        }
    }
}