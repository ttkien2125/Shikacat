using System.Collections.Generic;
using UnityEngine;

public class TutorialGhostManager : MonoBehaviour
{
    private class GhostBoxInstance
    {
        public int RegionId;
        public Vector2Int Min;
        public Vector2Int Max;
        public Vector2Int CluePos;
        public SpriteRenderer Renderer;
        public bool IsPlaced;
        public float CurrentAlpha;
        public Color BaseColor;
    }

    [Header("Ghost Visuals")]
    [SerializeField] private Color _ghostFallbackColor = new(0.40f, 0.85f, 1.0f, 0.45f);
    [SerializeField] private float _pulseSpeed = 3.2f;
    [SerializeField] private float _pulseMinAlpha = 0.22f;
    [SerializeField] private float _pulseMaxAlpha = 0.52f;

    [Header("Guide Indicator")]
    [SerializeField] private bool _showGuidePointer = true;

    private BoxManager _boxManager;
    private InputHandler _inputHandler;
    private ThemeDatabase _themes;
    private float _gridSize = 2f;

    private Transform _ghostContainer;
    private readonly List<GhostBoxInstance> _activeGhosts = new();

    private GameObject _guidePointerObj;
    private SpriteRenderer _pointerRenderer;
    private SpriteRenderer _pointerRipple;
    private float _pointerTimer;
    private GhostBoxInstance _currentGuideTarget;
    private bool _isTutorialActive;

    public void Init(
        BoxManager boxManager,
        InputHandler inputHandler,
        ThemeDatabase themes,
        float gridSize)
    {
        _boxManager = boxManager;
        _inputHandler = inputHandler;
        _themes = themes;
        _gridSize = gridSize;

        if (_ghostContainer == null)
        {
            GameObject container = new("TutorialGhostContainer");
            container.transform.SetParent(transform);
            _ghostContainer = container.transform;
        }

        CreateGuidePointer();
    }

    private void CreateGuidePointer()
    {
        if (_guidePointerObj != null)
        {
            return;
        }


        _guidePointerObj = new GameObject("TutorialGuidePointer");
        _guidePointerObj.transform.SetParent(transform);

        _pointerRenderer = _guidePointerObj.AddComponent<SpriteRenderer>();
        _pointerRenderer.sprite = UIAssets.GetSprite("UI_Circle");
        _pointerRenderer.sortingOrder = 5;
        _pointerRenderer.color = new Color(1f, 1f, 1f, 0.9f);
        _guidePointerObj.transform.localScale = Vector3.one * (_gridSize * 0.28f);

        GameObject rippleObj = new("Ripple");
        rippleObj.transform.SetParent(_guidePointerObj.transform);
        rippleObj.transform.localPosition = Vector3.zero;
        rippleObj.transform.localScale = Vector3.one;

        _pointerRipple = rippleObj.AddComponent<SpriteRenderer>();
        _pointerRipple.sprite = UIAssets.GetSprite("UI_Circle");
        _pointerRipple.sortingOrder = 4;
        _pointerRipple.color = new Color(1f, 1f, 1f, 0.45f);

        _guidePointerObj.SetActive(false);
    }

    public void OnLevelStarted(Level level, float gridSize, Cell[,] cells)
    {
        _gridSize = gridSize;
        ClearGhosts();

        if (level == null || !level.IsTutorial || !level.ShowGhostPreview)
        {
            _isTutorialActive = false;
            if (_guidePointerObj != null)
            {
                _guidePointerObj.SetActive(false);
            }


            return;
        }

        _isTutorialActive = true;

        if (_ghostContainer == null)
        {
            GameObject container = new("TutorialGhostContainer");
            container.transform.SetParent(transform);
            container.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            container.transform.localScale = Vector3.one;
            _ghostContainer = container.transform;
        }

        // Show friendly tutorial hint toast
        if (!string.IsNullOrEmpty(level.TutorialHint) && UIManager.Instance != null)
        {
            UIManager.Instance.ShowToast(level.TutorialHint);
        }

        if (_boxManager == null)
        {
            _boxManager = FindObjectOfType<BoxManager>();
        }

        // Spawn ghost preview boxes for all solution regions
        if (level.Solution != null && _boxManager != null)
        {
            Sprite ghostSprite = _boxManager.PreviewBoxSprite != null
                ? _boxManager.PreviewBoxSprite
                : Resources.Load<Sprite>("Sprites/Preview_Box");

            foreach (Level.SolutionBox sol in level.Solution)
            {
                // Find clue position for this region
                Vector2Int cluePos = sol.Min;
                if (level.Clues != null)
                {
                    foreach (Level.Clue clue in level.Clues)
                    {
                        if (clue.RegionId == sol.RegionId)
                        {
                            cluePos = clue.Position;
                            break;
                        }
                    }
                }

                GameObject ghostObj = new($"GhostBox_{sol.RegionId}");
                ghostObj.transform.SetParent(_ghostContainer);
                ghostObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                ghostObj.transform.localScale = Vector3.one;

                SpriteRenderer sr = ghostObj.AddComponent<SpriteRenderer>();
                sr.sprite = ghostSprite;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.sortingOrder = 2;

                _boxManager.PositionBoxRenderer(sr, sol.Min, sol.Max, _gridSize);

                Color baseColor = _ghostFallbackColor;
                if (_themes != null && _themes.Count > 0)
                {
                    CatTheme theme = _themes.GetTheme(sol.RegionId);
                    if (!string.IsNullOrEmpty(theme.name))
                    {
                        Color themeColor = ThemePalette.GetThemeColor(theme.name);
                        baseColor = Color.Lerp(themeColor, _ghostFallbackColor, 0.45f);
                    }
                }

                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, _pulseMinAlpha);

                _activeGhosts.Add(new GhostBoxInstance
                {
                    RegionId = sol.RegionId,
                    Min = sol.Min,
                    Max = sol.Max,
                    CluePos = cluePos,
                    Renderer = sr,
                    IsPlaced = false,
                    CurrentAlpha = _pulseMinAlpha,
                    BaseColor = baseColor
                });
            }
        }

        OnBoardUpdated(cells);
    }

    public void OnBoardUpdated(Cell[,] cells)
    {
        if (!_isTutorialActive || cells == null)
        {
            return;
        }


        foreach (GhostBoxInstance ghost in _activeGhosts)
        {
            if (ghost == null)
            {
                continue;
            }


            bool isCurrentlyPlaced = true;
            for (int x = ghost.Min.x; x <= ghost.Max.x; x++)
            {
                for (int y = ghost.Min.y; y <= ghost.Max.y; y++)
                {
                    if (x < 0 || x >= cells.GetLength(0) || y < 0 || y >= cells.GetLength(1) ||
                        cells[x, y] == null || cells[x, y].Id != ghost.RegionId)
                    {
                        isCurrentlyPlaced = false;
                        break;
                    }
                }
                if (!isCurrentlyPlaced)
                {
                    break;
                }

            }

            ghost.IsPlaced = isCurrentlyPlaced;
        }

        UpdateGuideTarget();
    }

    private void UpdateGuideTarget()
    {
        _currentGuideTarget = null;
        foreach (GhostBoxInstance ghost in _activeGhosts)
        {
            if (ghost != null && !ghost.IsPlaced)
            {
                _currentGuideTarget = ghost;
                break;
            }
        }

        _pointerTimer = 0f;
    }

    public void OnLevelCompleted()
    {
        _isTutorialActive = false;
        ClearGhosts();
        if (_guidePointerObj != null)
        {
            _guidePointerObj.SetActive(false);
        }

    }

    public void ClearGhosts()
    {
        foreach (GhostBoxInstance ghost in _activeGhosts)
        {
            if (ghost?.Renderer != null)
            {
                Destroy(ghost.Renderer.gameObject);
            }
        }
        _activeGhosts.Clear();
        _currentGuideTarget = null;
        if (_guidePointerObj != null)
        {
            _guidePointerObj.SetActive(false);
        }

    }

    private void Update()
    {
        if (!_isTutorialActive)
        {
            return;
        }

        // 1. Animate Ghost Boxes breathing / pulsing

        float pulseFactor = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
        float pulseAlpha = Mathf.Lerp(_pulseMinAlpha, _pulseMaxAlpha, pulseFactor);

        foreach (GhostBoxInstance ghost in _activeGhosts)
        {
            if (ghost == null || ghost.Renderer == null)
            {
                continue;
            }


            float targetA = ghost.IsPlaced ? 0f : pulseAlpha;
            ghost.CurrentAlpha = Mathf.MoveTowards(ghost.CurrentAlpha, targetA, Time.deltaTime * 3.5f);

            Color c = ghost.BaseColor;
            c.a = ghost.CurrentAlpha;
            ghost.Renderer.color = c;
            ghost.Renderer.gameObject.SetActive(ghost.CurrentAlpha > 0.005f);
        }

        // 2. Animate Guide Pointer
        UpdateGuidePointerAnimation();
    }

    private void UpdateGuidePointerAnimation()
    {
        if (!_showGuidePointer || _guidePointerObj == null)
        {
            return;
        }


        bool isDragging = _inputHandler != null && _inputHandler.IsDragging;
        if (isDragging || _currentGuideTarget == null)
        {
            _guidePointerObj.SetActive(false);
            return;
        }

        _guidePointerObj.SetActive(true);
        _pointerTimer += Time.deltaTime;

        // Slide from one end of the solution region to the other
        Vector2Int startCorner = _currentGuideTarget.Min;
        Vector2Int endCorner = _currentGuideTarget.Max;

        Vector3 startWorld = new Vector3(startCorner.x + 0.5f, startCorner.y + 0.5f, 0f) * _gridSize;
        Vector3 endWorld = new Vector3(endCorner.x + 0.5f, endCorner.y + 0.5f, 0f) * _gridSize;

        float cycleDuration = 2.2f;
        float t = _pointerTimer % cycleDuration / cycleDuration;

        Vector3 currentPos;
        float alpha;
        float rippleScale = 1f;
        float rippleAlpha;

        if (t < 0.20f)
        {
            // Touch down at start cell
            float phaseT = t / 0.20f;
            currentPos = startWorld;
            alpha = Mathf.Clamp01(phaseT * 1.5f);

            // Ripple expands on touch
            rippleScale = Mathf.Lerp(1f, 2.5f, phaseT);
            rippleAlpha = (1f - phaseT) * 0.5f;
        }
        else if (t < 0.70f)
        {
            // Slide smoothly from start to end corner
            float phaseT = (t - 0.20f) / 0.50f;
            float smooth = Mathf.SmoothStep(0f, 1f, phaseT);
            currentPos = Vector3.Lerp(startWorld, endWorld, smooth);
            alpha = 1f;
            rippleAlpha = 0f;
        }
        else if (t < 0.85f)
        {
            // Hold at destination
            currentPos = endWorld;
            alpha = 1f;
            rippleAlpha = 0f;
        }
        else
        {
            // Fade out
            float phaseT = (t - 0.85f) / 0.15f;
            currentPos = endWorld;
            alpha = 1f - phaseT;
            rippleAlpha = 0f;
        }

        _guidePointerObj.transform.position = currentPos;

        if (_pointerRenderer != null)
        {
            Color c = _pointerRenderer.color;
            c.a = alpha * 0.95f;
            _pointerRenderer.color = c;
        }

        if (_pointerRipple != null)
        {
            _pointerRipple.transform.localScale = Vector3.one * rippleScale;
            Color rc = _pointerRipple.color;
            rc.a = rippleAlpha;
            _pointerRipple.color = rc;
        }
    }
}

