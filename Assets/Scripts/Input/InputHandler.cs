using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputHandler : MonoBehaviour
{
    public event Action<Vector2Int, Vector2Int> OnSelectionChanged;
    public event Action<Vector2Int, Vector2Int> OnSelectionConfirmed;
    public event Action<Vector2Int> OnCellTapped;

    public bool IsInputBlocked;
    public bool IsDragging => _isDragging;

    private static readonly List<RaycastResult> _raycastResults = new();
    private PointerEventData _cachedEventData;

    private Camera _camera;
    private float _gridSize;
    private int _rows;
    private int _cols;
    private Vector2Int _startGrid;
    private Vector2Int _endGrid;
    private bool _isDragging;
    private bool _hasDragged;

    public void Init(Camera camera, float gridSize, int rows, int cols)
    {
        _camera = camera;
        _gridSize = gridSize;
        _rows = rows;
        _cols = cols;
    }

    private void Update()
    {
        if (IsInputBlocked || _camera == null)
        {
            return;
        }


        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverUI())
            {
                return;
            }


            if (!TryGetGridPosition(out Vector2Int gridPos))
            {
                return;
            }


            if (!IsValid(gridPos))
            {
                return;
            }


            _startGrid = gridPos;
            _endGrid = gridPos;
            _isDragging = true;
            _hasDragged = false;

            NotifySelectionChanged();
        }
        else if (Input.GetMouseButton(0) && _isDragging)
        {
            if (TryGetGridPosition(out Vector2Int gridPos) &&
                gridPos != _endGrid && IsValid(gridPos))
            {
                _endGrid = gridPos;
                _hasDragged = true;
                NotifySelectionChanged();
            }
        }
        else if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            _isDragging = false;

            if (!_hasDragged && _startGrid == _endGrid)
            {
                OnCellTapped?.Invoke(_startGrid);
            }
            else
            {
                NotifySelectionConfirmed();
            }
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        _cachedEventData ??= new(EventSystem.current);

        Vector2 screenPos = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
        _cachedEventData.position = screenPos;

        _raycastResults.Clear();
        EventSystem.current.RaycastAll(_cachedEventData, _raycastResults);

        foreach (RaycastResult result in _raycastResults)
        {
            // Only block input if raycast hit a screen-space UI element (like mobile HUD, Undo button)
            // and ignore world-space canvas elements (like cell numbers on the board)
            Canvas canvas = result.gameObject.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0
            && pos.x < _rows && pos.y < _cols;
    }

    private bool TryGetGridPosition(out Vector2Int gridPos)
    {
        Vector3 mousePos = Input.mousePosition;

        if (float.IsNaN(mousePos.x) || float.IsInfinity(mousePos.x) ||
            float.IsNaN(mousePos.y) || float.IsInfinity(mousePos.y))
        {
            gridPos = default;
            return false;
        }

        Vector3 worldPos = _camera.ScreenToWorldPoint(mousePos);
        gridPos = new(
            Mathf.FloorToInt(worldPos.x / _gridSize),
            Mathf.FloorToInt(worldPos.y / _gridSize)
        );

        return true;
    }

    private void NotifySelectionChanged()
    {
        Vector2Int min = Vector2Int.Min(_startGrid, _endGrid);
        Vector2Int max = Vector2Int.Max(_startGrid, _endGrid);
        OnSelectionChanged?.Invoke(min, max);
    }

    private void NotifySelectionConfirmed()
    {
        Vector2Int min = Vector2Int.Min(_startGrid, _endGrid);
        Vector2Int max = Vector2Int.Max(_startGrid, _endGrid);
        OnSelectionConfirmed?.Invoke(min, max);
    }
}