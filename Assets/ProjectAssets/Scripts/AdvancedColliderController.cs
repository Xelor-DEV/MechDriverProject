using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using System.Collections;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AdvancedColliderController : MonoBehaviour
{
    public enum FaceDirection { Top, Bottom, Left, Right, Front, Back }

    [Title("Collider Configuration")]
    [Required]
    [SerializeField] private Collider _targetCollider;

    [SerializeField] private bool _isTrigger;
    [SerializeField] private string _filterTag = "";

    [Title("Gizmo Settings")]
    [ColorPalette("Gizmo Color")]
    [SerializeField] private Color _gizmoColor = new Color(0, 1, 0, 0.3f);

    [Title("Collision Events")]
    [ShowIfGroup("CollisionEvents", Condition = "@!_isTrigger")]
    [BoxGroup("CollisionEvents/Enter"), HideLabel] public UnityEvent<Collision> onCollisionEnter;
    [BoxGroup("CollisionEvents/Stay"), HideLabel] public UnityEvent<Collision> onCollisionStay;
    [BoxGroup("CollisionEvents/Exit"), HideLabel] public UnityEvent<Collision> onCollisionExit;

    [Title("Trigger Events")]
    [ShowIfGroup("TriggerEvents", Condition = "@_isTrigger")]
    [BoxGroup("TriggerEvents/Enter"), HideLabel] public UnityEvent<Collider> onTriggerEnter;
    [BoxGroup("TriggerEvents/Stay"), HideLabel] public UnityEvent<Collider> onTriggerStay;
    [BoxGroup("TriggerEvents/Exit"), HideLabel] public UnityEvent<Collider> onTriggerExit;

    [Title("Directional Events")]
    [BoxGroup("Collision Faces", ShowLabel = false)]
    [FoldoutGroup("Collision Faces/Collision")] public DirectionalCollisionEvents collisionFaces;
    [FoldoutGroup("Collision Faces/Trigger")] public DirectionalTriggerEvents triggerFaces;

    [Serializable]
    public class DirectionalCollisionEvents
    {
        [BoxGroup("Top"), HideLabel] public UnityEvent<Collision> onTop;
        [BoxGroup("Bottom"), HideLabel] public UnityEvent<Collision> onBottom;
        [BoxGroup("Left"), HideLabel] public UnityEvent<Collision> onLeft;
        [BoxGroup("Right"), HideLabel] public UnityEvent<Collision> onRight;
        [BoxGroup("Front"), HideLabel] public UnityEvent<Collision> onFront;
        [BoxGroup("Back"), HideLabel] public UnityEvent<Collision> onBack;
    }

    [Serializable]
    public class DirectionalTriggerEvents
    {
        [BoxGroup("Top"), HideLabel] public UnityEvent<Collider> onTop;
        [BoxGroup("Bottom"), HideLabel] public UnityEvent<Collider> onBottom;
        [BoxGroup("Left"), HideLabel] public UnityEvent<Collider> onLeft;
        [BoxGroup("Right"), HideLabel] public UnityEvent<Collider> onRight;
        [BoxGroup("Front"), HideLabel] public UnityEvent<Collider> onFront;
        [BoxGroup("Back"), HideLabel] public UnityEvent<Collider> onBack;
    }

    [Title("Editor Tools")]
    [ButtonGroup("FaceButtons")]
    [Button("Show Top Face", ButtonSizes.Medium)] private void ShowTopFace() => StartFaceBlink(FaceDirection.Top);
    [ButtonGroup("FaceButtons")]
    [Button("Show Bottom Face", ButtonSizes.Medium)] private void ShowBottomFace() => StartFaceBlink(FaceDirection.Bottom);
    [ButtonGroup("FaceButtons")]
    [Button("Show Left Face", ButtonSizes.Medium)] private void ShowLeftFace() => StartFaceBlink(FaceDirection.Left);
    [ButtonGroup("FaceButtons")]
    [Button("Show Right Face", ButtonSizes.Medium)] private void ShowRightFace() => StartFaceBlink(FaceDirection.Right);
    [ButtonGroup("FaceButtons")]
    [Button("Show Front Face", ButtonSizes.Medium)] private void ShowFrontFace() => StartFaceBlink(FaceDirection.Front);
    [ButtonGroup("FaceButtons")]
    [Button("Show Back Face", ButtonSizes.Medium)] private void ShowBackFace() => StartFaceBlink(FaceDirection.Back);

    private FaceDirection _currentFace;
    private bool _isBlinking;
    private float _blinkStartTime;
    private const float BLINK_DURATION = 2f;
    private BoxCollider _boxCollider;

    private void Awake()
    {
        if (_targetCollider == null)
        {
            _targetCollider = GetComponent<Collider>();
            if (_targetCollider == null)
            {
                Debug.LogError("No collider assigned or found!", this);
                return;
            }
        }

        _isTrigger = _targetCollider.isTrigger;
        _boxCollider = _targetCollider as BoxCollider;
    }

    #region Collision Handling
    private void OnCollisionEnter(Collision collision)
    {
        if (!ShouldProcess(collision.gameObject)) return;

        onCollisionEnter?.Invoke(collision);
        ProcessCollisionFace(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (ShouldProcess(collision.gameObject))
            onCollisionStay?.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (ShouldProcess(collision.gameObject))
            onCollisionExit?.Invoke(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ShouldProcess(other.gameObject)) return;

        onTriggerEnter?.Invoke(other);
        ProcessTriggerFace(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (ShouldProcess(other.gameObject))
            onTriggerStay?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (ShouldProcess(other.gameObject))
            onTriggerExit?.Invoke(other);
    }
    #endregion

    #region Face Detection
    private void ProcessCollisionFace(Collision collision)
    {
        if (collision.contactCount == 0 || _boxCollider == null) return;

        ContactPoint contact = collision.contacts[0];
        Vector3 localNormal = transform.InverseTransformDirection(contact.normal);
        FaceDirection face = GetFaceFromNormal(localNormal);

        TriggerDirectionalEvent(face, collision);
    }

    private void ProcessTriggerFace(Collider other)
    {
        if (_boxCollider == null) return;

        Vector3 direction = other.bounds.center - _boxCollider.bounds.center;
        Vector3 localDir = transform.InverseTransformDirection(direction);
        FaceDirection face = GetFaceFromDirection(localDir);

        TriggerDirectionalEvent(face, other);
    }

    private FaceDirection GetFaceFromNormal(Vector3 normal)
    {
        if (Mathf.Abs(normal.y) > Mathf.Abs(normal.x) && Mathf.Abs(normal.y) > Mathf.Abs(normal.z))
            return normal.y > 0 ? FaceDirection.Top : FaceDirection.Bottom;

        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.z))
            return normal.x > 0 ? FaceDirection.Right : FaceDirection.Left;

        return normal.z > 0 ? FaceDirection.Front : FaceDirection.Back;
    }

    private FaceDirection GetFaceFromDirection(Vector3 direction)
    {
        Vector3 absDir = new Vector3(
            Mathf.Abs(direction.x),
            Mathf.Abs(direction.y),
            Mathf.Abs(direction.z)
        );

        if (absDir.y > absDir.x && absDir.y > absDir.z)
            return direction.y > 0 ? FaceDirection.Top : FaceDirection.Bottom;

        if (absDir.x > absDir.z)
            return direction.x > 0 ? FaceDirection.Right : FaceDirection.Left;

        return direction.z > 0 ? FaceDirection.Front : FaceDirection.Back;
    }

    private void TriggerDirectionalEvent(FaceDirection face, Collision collision)
    {
        switch (face)
        {
            case FaceDirection.Top: collisionFaces.onTop?.Invoke(collision); break;
            case FaceDirection.Bottom: collisionFaces.onBottom?.Invoke(collision); break;
            case FaceDirection.Left: collisionFaces.onLeft?.Invoke(collision); break;
            case FaceDirection.Right: collisionFaces.onRight?.Invoke(collision); break;
            case FaceDirection.Front: collisionFaces.onFront?.Invoke(collision); break;
            case FaceDirection.Back: collisionFaces.onBack?.Invoke(collision); break;
        }
    }

    private void TriggerDirectionalEvent(FaceDirection face, Collider other)
    {
        switch (face)
        {
            case FaceDirection.Top: triggerFaces.onTop?.Invoke(other); break;
            case FaceDirection.Bottom: triggerFaces.onBottom?.Invoke(other); break;
            case FaceDirection.Left: triggerFaces.onLeft?.Invoke(other); break;
            case FaceDirection.Right: triggerFaces.onRight?.Invoke(other); break;
            case FaceDirection.Front: triggerFaces.onFront?.Invoke(other); break;
            case FaceDirection.Back: triggerFaces.onBack?.Invoke(other); break;
        }
    }
    #endregion

    #region Utility Methods
    private bool ShouldProcess(GameObject other)
    {
        return string.IsNullOrEmpty(_filterTag) || other.CompareTag(_filterTag);
    }

    private void StartFaceBlink(FaceDirection face)
    {
        _currentFace = face;
        _isBlinking = true;
        _blinkStartTime = Time.time;
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_targetCollider == null) return;

        DrawPersistentCollider();
        DrawBlinkingFace();
    }

    private void DrawPersistentCollider()
    {
        if (_targetCollider is BoxCollider box)
        {
            Gizmos.color = _gizmoColor;
            Matrix4x4 matrix = Matrix4x4.TRS(
                _targetCollider.transform.TransformPoint(box.center),
                _targetCollider.transform.rotation,
                Vector3.Scale(box.size, _targetCollider.transform.lossyScale)
            );

            Gizmos.matrix = matrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    private void DrawBlinkingFace()
    {
        if (!_isBlinking || _boxCollider == null) return;

        if (Time.time - _blinkStartTime > BLINK_DURATION)
        {
            _isBlinking = false;
            return;
        }

        float blinkValue = Mathf.PingPong(Time.time * 10, 1);
        Gizmos.color = new Color(1, 0, 0, blinkValue);

        Vector3 size = Vector3.Scale(_boxCollider.size, _targetCollider.transform.lossyScale);
        Vector3 center = _targetCollider.transform.TransformPoint(_boxCollider.center);
        Vector3 halfSize = size * 0.5f;
        Vector3 faceOffset = GetFaceOffset(_currentFace, halfSize);

        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            center + faceOffset,
            _targetCollider.transform.rotation,
            GetFaceScale(_currentFace, size)
        );

        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }

    private Vector3 GetFaceOffset(FaceDirection face, Vector3 halfSize)
    {
        switch (face)
        {
            case FaceDirection.Top: return _targetCollider.transform.up * halfSize.y;
            case FaceDirection.Bottom: return _targetCollider.transform.up * -halfSize.y;
            case FaceDirection.Right: return _targetCollider.transform.right * halfSize.x;
            case FaceDirection.Left: return _targetCollider.transform.right * -halfSize.x;
            case FaceDirection.Front: return _targetCollider.transform.forward * halfSize.z;
            case FaceDirection.Back: return _targetCollider.transform.forward * -halfSize.z;
            default: return Vector3.zero;
        }
    }

    private Vector3 GetFaceScale(FaceDirection face, Vector3 fullSize)
    {
        const float thickness = 0.01f;
        return face switch
        {
            FaceDirection.Top or FaceDirection.Bottom => new Vector3(fullSize.x, thickness, fullSize.z),
            FaceDirection.Left or FaceDirection.Right => new Vector3(thickness, fullSize.y, fullSize.z),
            FaceDirection.Front or FaceDirection.Back => new Vector3(fullSize.x, fullSize.y, thickness),
            _ => Vector3.one
        };
    }
#endif
}