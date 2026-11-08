using UnityEngine;

namespace TrackGenerator.TrackGenerator
{
    public class TrackPiece : MonoBehaviour
    {
        [SerializeField] private int xAxis;
        [SerializeField] private int zAxis;

        [Header("Connection points (local space)")]
        [Tooltip("Position of the piece's entry connection, relative to this object.")]
        [SerializeField]
        private Vector3 entryPoint;

        [Tooltip("Outward direction at the entry connection.")] [SerializeField]
        private Vector3 entryDirection = Vector3.left;

        [Tooltip("Position of the piece's exit connection, relative to this object.")] [SerializeField]
        private Vector3 exitPoint;

        [Tooltip("Outward direction at the exit connection.")] [SerializeField]
        private Vector3 exitDirection = Vector3.right;

        public Vector3 EntryPoint => entryPoint;
        public Vector3 EntryDirection => entryDirection.normalized;
        public Vector3 ExitPoint => exitPoint;
        public Vector3 ExitDirection => exitDirection.normalized;

        public bool HasValidConnections =>
            entryDirection.sqrMagnitude > 0.0001f && exitDirection.sqrMagnitude > 0.0001f;

        private void OnDrawGizmosSelected()
        {
            DrawConnection(entryPoint, entryDirection, Color.cyan);
            DrawConnection(exitPoint, exitDirection, Color.yellow);
        }

        private void DrawConnection(Vector3 point, Vector3 direction, Color color)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 worldPoint = transform.TransformPoint(point);
            Vector3 worldDirection = transform.TransformDirection(direction.normalized);
            Gizmos.color = color;
            Gizmos.DrawSphere(worldPoint, 0.4f);
            Gizmos.DrawLine(worldPoint, worldPoint + worldDirection * 2f);
        }
    }
}