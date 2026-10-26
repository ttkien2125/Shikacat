using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFitter : MonoBehaviour
{
    [Header("Grid & Framing Settings")]
    [SerializeField] private float _gridSize = 2f;
    [Range(0.5f, 0.99f)]
    [SerializeField] private float _screenWidthFraction = 0.94f;

    [Header("Default Grid Scale Reference")]
    [Tooltip("The reference board dimensions (e.g. 5x5) that define the standard cell size on screen. Boards equal to or smaller than this will not zoom in, maintaining this cell size.")]
    [SerializeField] private int _defaultReferenceRows = 5;
    [SerializeField] private int _defaultReferenceCols = 5;

    [Header("HUD Safe Margins (Canvas Pixels)")]
    [SerializeField] private float _topHudHeight = 320f;
    [SerializeField] private float _bottomHudHeight = 320f;
    [SerializeField] private float _canvasRefWidth = 1080f;

    private Camera _camera;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        RefitCurrentLevel();
    }

    private void OnEnable()
    {
        LevelManager.OnLevelStarted += HandleLevelStarted;
    }

    private void OnDisable()
    {
        LevelManager.OnLevelStarted -= HandleLevelStarted;
    }

    private void LateUpdate()
    {
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            RefitCurrentLevel();
        }
    }

    private void HandleLevelStarted()
    {
        RefitCurrentLevel();
    }

    public void RefitCurrentLevel()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.CurrentLevel == null)
        {
            return;
        }


        Level level = LevelManager.Instance.CurrentLevel;
        Fit(level.Rows, level.Cols);
    }

    public float CalculateRequiredSize(int rows, int cols)
    {
        if (_camera == null || rows <= 0 || cols <= 0)
        {
            return 0f;
        }

        // GridManager background sprite includes a 0.05 * gridSize border padding

        float boardWidth = (rows + 0.05f) * _gridSize;
        float boardHeight = (cols + 0.05f) * _gridSize;

        // 1. Calculate camera size required to fit the grid width to screenWidthFraction (e.g. 94%)
        // visibleWidth = 2 * orthographicSize * camera.aspect = boardWidth / screenWidthFraction
        float targetVisibleWidth = boardWidth / Mathf.Clamp(_screenWidthFraction, 0.5f, 0.99f);
        float sizeFromWidth = targetVisibleWidth * 0.5f / _camera.aspect;

        // 2. Vertical safety calculation:
        // Prevent board from overlapping the top header and bottom dock on squarish screens (e.g. iPad) or tall boards.
        // Canvas scale matches width, so canvas height in reference pixels = canvasRefWidth / camera.aspect.
        float totalHudHeight = _topHudHeight + _bottomHudHeight;
        float hudHeightFraction = totalHudHeight / _canvasRefWidth * _camera.aspect;

        // Leave at least a 6% vertical margin between the board and the HUD bars
        float availableHeightFraction = Mathf.Max(0.2f, 1f - hudHeightFraction - 0.06f);
        float sizeFromHeight = boardHeight * 0.5f / availableHeightFraction;

        return Mathf.Max(sizeFromWidth, sizeFromHeight);
    }

    public void Fit(int rows, int cols)
    {
        if (_camera == null || rows <= 0 || cols <= 0)
        {
            return;
        }

        // Center camera horizontally and vertically on the board center

        Vector3 center = new(rows * _gridSize * 0.5f, cols * _gridSize * 0.5f, _camera.transform.position.z);
        _camera.transform.position = center;

        // Required size for current level
        float requiredSize = CalculateRequiredSize(rows, cols);

        // Standard default size based on reference dimensions (e.g. 5x5)
        int refRows = Mathf.Max(1, _defaultReferenceRows);
        int refCols = Mathf.Max(1, _defaultReferenceCols);
        float defaultSize = CalculateRequiredSize(refRows, refCols);

        // Keep the default cell size for smaller grids; scale down (zoom out) only when the grid does not fit
        _camera.orthographicSize = Mathf.Max(requiredSize, defaultSize);
    }
}
