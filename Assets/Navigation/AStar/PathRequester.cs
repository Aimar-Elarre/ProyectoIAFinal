using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathRequester : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Transform hacia el que se calculará el camino")]
    public Transform target;

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    [Tooltip("Distancia a un nodo para considerarlo alcanzado y avanzar al siguiente")]
    public float nodeReachDistance = 0.1f;
    [Tooltip("Tecla para iniciar/detener el seguimiento del camino")]
    public Key followKey = Key.Space;

    Vector3    _previousTargetPos;
    List<Node> _currentPath;
    int        _pathIndex;
    bool       _isFollowing;


    void Start()
    {
        if (target != null)
            RequestPath();
    }

    void Update()
    {
        if (target == null) return;

        if (Vector3.Distance(target.position, _previousTargetPos) > 0.5f)
        {
            RequestPath();
            _isFollowing = false;
        }

        if (Keyboard.current != null && Keyboard.current[followKey].wasPressedThisFrame)
            ToggleFollow();

        if (_isFollowing)
            FollowPath();
    }


    void RequestPath()
    {
        _previousTargetPos = target.position;
        _currentPath = AStarPathfinder.instance.FindPath(
            transform.position, target.position);
        _pathIndex   = 0;
        _isFollowing = false;
    }

    void ToggleFollow()
    {
        if (_currentPath == null || _currentPath.Count == 0)
        {
            Debug.LogWarning("PathRequester: No hay camino calculado. Espera a que A* encuentre uno.");
            return;
        }

        _isFollowing = !_isFollowing;
        if (_isFollowing) _pathIndex = 0;
    }

    void FollowPath()
    {
        if (_currentPath == null || _pathIndex >= _currentPath.Count)
        {
            _isFollowing = false;
            return;
        }

        Vector3 nodePos = _currentPath[_pathIndex].worldPosition;
        Vector3 target3D = new Vector3(nodePos.x, transform.position.y, nodePos.z);

        transform.position = Vector3.MoveTowards(
            transform.position, target3D, moveSpeed * Time.deltaTime);

        Vector3 dir = new Vector3(
            target3D.x - transform.position.x, 0f,
            target3D.z - transform.position.z);
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime);

        if (Vector3.Distance(transform.position, target3D) < nodeReachDistance)
            _pathIndex++;
    }


    void OnDrawGizmos()
    {
        if (_currentPath == null || _currentPath.Count == 0) return;

        for (int i = 0; i < _currentPath.Count; i++)
        {
            Gizmos.color = (i < _pathIndex) ? Color.gray : Color.cyan;

            if (i == 0)
                Gizmos.DrawLine(transform.position, _currentPath[i].worldPosition);
            else
                Gizmos.DrawLine(
                    _currentPath[i - 1].worldPosition,
                    _currentPath[i].worldPosition);

            Gizmos.DrawSphere(_currentPath[i].worldPosition, 0.15f);
        }
    }
}
