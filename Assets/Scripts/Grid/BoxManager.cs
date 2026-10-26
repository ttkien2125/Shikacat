using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxManager : MonoBehaviour
{
    public const float DefaultBoxPadding = 0.22f;
    public const float DefaultMaskWidthInset = 0.356f;
    public const float DefaultMaskBottomMargin = 0.568f;
    public const float DefaultMaskExtraTop = 1.958f;

    [SerializeField] private Sprite _previewBoxSprite;
    [SerializeField] private Sprite _maskSprite;

    [Header("3D Mask & Box Offsets")]
    [Tooltip("Padding deducted from total cell dimensions for the box border")]
    [SerializeField] private float _boxPadding = DefaultBoxPadding;

    [Tooltip("Horizontal inset inside the box where the cat peeks through")]
    [SerializeField] private float _maskWidthInset = DefaultMaskWidthInset;

    [Tooltip("Bottom margin inside the box below which the cat sprite is cut off")]
    [SerializeField] private float _maskBottomMargin = DefaultMaskBottomMargin;

    [Tooltip("Extra vertical extension above the box allowing cat ears/head to pop out")]
    [SerializeField] private float _maskExtraTop = DefaultMaskExtraTop;

    public Sprite PreviewBoxSprite
    {
        get
        {
            if (_previewBoxSprite == null)
            {
                _previewBoxSprite = Resources.Load<Sprite>("Sprites/Preview_Box");
            }
            return _previewBoxSprite;
        }
    }

    public Sprite MaskSprite
    {
        get
        {
            if (_maskSprite == null)
            {
                _maskSprite = Resources.Load<Sprite>("Sprites/Mask_Square");
            }
            return _maskSprite;
        }
    }

    public float BoxPadding => _boxPadding;
    public float MaskWidthInset => _maskWidthInset;
    public float MaskBottomMargin => _maskBottomMargin;
    public float MaskExtraTop => _maskExtraTop;

    [Header("Toys Configuration")]
    [Tooltip("Toy sprites loaded from Assets/Resources/Sprites/Toys")]
    [SerializeField] private Sprite[] _toySprites;

    [Tooltip("Maximum number of toys that can appear on a box's tiles (at most 3)")]
    [SerializeField][Range(1, 3)] private int _maxToysPerBox = 3;

    [Tooltip("If true, tints toys to match the box floor with authentic outlines")]
    [SerializeField] private bool _tintToysToTheme = true;

    [Tooltip("Alpha opacity for toy sprites (around 0.55 blends white fill into the floor while preserving clean outlines)")]
    [SerializeField][Range(0.2f, 1f)] private float _toyAlpha = 0.8f;

    [Tooltip("Scale factor for larger furniture toys (houses, beds, mats, scratchers, bowls)")]
    [SerializeField][Range(0.4f, 1f)] private float _furnitureScaleMultiplier = 0.68f;

    [Tooltip("Scale factor for small loose toys (fish toys, fish bones, wool balls, balls)")]
    [SerializeField][Range(0.3f, 0.8f)] private float _looseToyScaleMultiplier = 0.48f;

    [Header("Animation")]
    [Tooltip("Duration in seconds for placed boxes to fade in")]
    [SerializeField] private float _boxFadeInDuration = 0.18f;

    private struct FadeState
    {
        public Coroutine Coroutine;
        public Cell ClueCell;
        public Vector3 CellBaseScale;
    }

    private readonly Dictionary<int, SpriteRenderer> _placedBoxes = new();
    private readonly Dictionary<int, FadeState> _fadeCoroutines = new();
    private SpriteRenderer _previewBox;
    private Transform _boxContainer;
    private Coroutine _shakeCoroutine;

    private static readonly Color ValidColor = new(0.35f, 0.90f, 0.45f, 0.95f);    // Crisp Green
    private static readonly Color IncompleteColor = new(1.0f, 0.85f, 0.25f, 0.95f); // Crisp Amber
    private static readonly Color InvalidColor = new(0.95f, 0.35f, 0.35f, 0.95f);   // Crisp Red

    public void Init()
    {
        _boxContainer = new GameObject("PlacedBoxes").transform;
        _boxContainer.SetParent(transform);

        GameObject previewObj = new("PreviewBox");
        previewObj.transform.SetParent(transform);
        _previewBox = previewObj.AddComponent<SpriteRenderer>();
        _previewBox.drawMode = SpriteDrawMode.Sliced;
        _previewBox.sortingOrder = 2;
        _previewBox.sprite = PreviewBoxSprite;
        _previewBox.color = Color.white;
        previewObj.SetActive(false);
    }

    public void ShowPreview(Cell[,] cells, Vector2Int min, Vector2Int max, float gridSize)
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }

        _previewBox.gameObject.SetActive(true);
        PositionBoxRenderer(_previewBox, min, max, gridSize);

        int area = (max.x - min.x + 1) * (max.y - min.y + 1);
        if (ShikakuValidator.TryGetSingleEnclosedNumber(cells, min, max, out int numberId))
        {
            Cell clueCell = default;
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    if (cells[x, y].IsNumber && cells[x, y].Id == numberId)
                    {
                        clueCell = cells[x, y];
                        break;
                    }
                }
            }

            _previewBox.color = (area == clueCell.Value) ? ValidColor : IncompleteColor;
        }
        else
        {
            _previewBox.color = InvalidColor;
        }
    }

    public void HidePreview()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }

        if (_previewBox != null)
        {
            _previewBox.gameObject.SetActive(false);
        }
    }

    public void PlayInvalidMoveShake(Vector2Int min, Vector2Int max, float gridSize)
    {
        if (_previewBox == null)
        {
            return;
        }

        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
        }

        _shakeCoroutine = StartCoroutine(ShakeRoutine(min, max, gridSize));
    }

    private IEnumerator ShakeRoutine(Vector2Int min, Vector2Int max, float gridSize)
    {
        _previewBox.gameObject.SetActive(true);
        PositionBoxRenderer(_previewBox, min, max, gridSize);

        Vector3 basePos = _previewBox.transform.position;
        Color baseColor = InvalidColor;
        _previewBox.color = baseColor;

        float duration = 0.28f;
        float elapsed = 0f;
        float frequency = 32f;
        float maxOffset = 0.12f * gridSize;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float decay = 1f - t;
            float offset = Mathf.Sin(elapsed * frequency) * maxOffset * decay;
            _previewBox.transform.position = basePos + new Vector3(offset, 0f, 0f);

            if (t > 0.45f)
            {
                float fadeT = (t - 0.45f) / 0.55f;
                Color c = baseColor;
                c.a = Mathf.Lerp(baseColor.a, 0f, fadeT);
                _previewBox.color = c;
            }

            yield return null;
        }

        _previewBox.transform.position = basePos;
        _previewBox.gameObject.SetActive(false);
        _shakeCoroutine = null;
    }

    public void SpawnOrUpdateBox(
        int id,
        Vector2Int min,
        Vector2Int max,
        CatTheme theme,
        float gridSize,
        Cell[,] cells,
        bool animate = true)
    {
        int catSortingOrder = SortingOrderConfig.GetCatOrder(id);
        int maskBackOrder = SortingOrderConfig.GetMaskBackOrder(id);

        if (!_placedBoxes.TryGetValue(id, out SpriteRenderer box))
        {
            GameObject boxObj = new($"Box_{id}");
            boxObj.transform.SetParent(_boxContainer);
            box = boxObj.AddComponent<SpriteRenderer>();
            box.drawMode = SpriteDrawMode.Sliced;
            box.sortingOrder = 1;

            GameObject maskObj = new("BoxMask");
            maskObj.transform.SetParent(boxObj.transform);
            SpriteMask mask = maskObj.AddComponent<SpriteMask>();
            mask.sprite = MaskSprite;
            mask.isCustomRangeActive = true;
            mask.frontSortingOrder = catSortingOrder;
            mask.backSortingOrder = maskBackOrder;
            mask.enabled = false;

            _placedBoxes[id] = box;
        }

        box.sprite = theme.boxSprite;
        PositionBoxRenderer(box, min, max, gridSize);

        int widthCount = max.x - min.x + 1;
        int heightCount = max.y - min.y + 1;
        bool isMultiCell = widthCount > 1 || heightCount > 1;

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (cells[x, y].IsNumber && cells[x, y].Id == id)
                {
                    cells[x, y].SetMaskActive(!isMultiCell);
                    if (!isMultiCell)
                    {
                        ConfigureCellMask(cells[x, y], gridSize);
                    }
                }
            }
        }

        SpriteMask boxMask = box.GetComponentInChildren<SpriteMask>(true);
        if (boxMask != null)
        {
            boxMask.sprite = MaskSprite;
            boxMask.isCustomRangeActive = true;
            boxMask.frontSortingOrder = catSortingOrder;
            boxMask.backSortingOrder = maskBackOrder;
            boxMask.enabled = isMultiCell;
            if (isMultiCell)
            {
                UpdateBoxMask(boxMask, min, max, gridSize);
            }
        }

        if (isMultiCell)
        {
            BoxToySpawner.Config toyConfig = new(
                _toySprites,
                _maxToysPerBox,
                _tintToysToTheme,
                _toyAlpha,
                _furnitureScaleMultiplier,
                _looseToyScaleMultiplier
            );
            BoxToySpawner.SpawnOrUpdateToys(box.gameObject, id, min, max, theme, gridSize, cells, toyConfig);
        }
        else
        {
            BoxToySpawner.ClearToys(box.gameObject);
        }

        Cell clueCell = null;
        if (cells != null)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    if (cells[x, y] != null && cells[x, y].IsNumber && cells[x, y].Id == id)
                    {
                        clueCell = cells[x, y];
                        break;
                    }
                }
                if (clueCell != null)
                {
                    break;
                }

            }
        }

        if (animate)
        {
            StartBoxFadeIn(id, box, clueCell);
        }
        else
        {
            SetBoxAlphaImmediate(id, box, clueCell);
        }
    }

    private void StartBoxFadeIn(int id, SpriteRenderer box, Cell clueCell)
    {
        Vector3 cellBaseScale = clueCell != null ? clueCell.transform.localScale : Vector3.one;

        if (_fadeCoroutines.TryGetValue(id, out FadeState existing))
        {
            if (existing.Coroutine != null)
            {
                StopCoroutine(existing.Coroutine);
            }
            if (existing.ClueCell != null)
            {
                existing.ClueCell.ResetAlpha();
                existing.ClueCell.transform.localScale = existing.CellBaseScale;
                if (existing.ClueCell == clueCell)
                {
                    cellBaseScale = existing.CellBaseScale;
                }
            }
        }

        Coroutine routine = StartCoroutine(BoxFadeInRoutine(id, box, clueCell, cellBaseScale));
        _fadeCoroutines[id] = new FadeState
        {
            Coroutine = routine,
            ClueCell = clueCell,
            CellBaseScale = cellBaseScale
        };
    }

    private void SetBoxAlphaImmediate(int id, SpriteRenderer box, Cell clueCell)
    {
        if (_fadeCoroutines.TryGetValue(id, out FadeState state))
        {
            if (state.Coroutine != null)
            {
                StopCoroutine(state.Coroutine);
            }


            if (state.ClueCell != null)
            {
                state.ClueCell.ResetAlpha();
                state.ClueCell.transform.localScale = state.CellBaseScale;
            }
            _ = _fadeCoroutines.Remove(id);
        }

        if (box != null)
        {
            SpriteRenderer[] renderers = box.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer r in renderers)
            {
                if (r != null)
                {
                    Color c = r.color;
                    c.a = 1f;
                    r.color = c;
                }
            }
            box.transform.localScale = Vector3.one;
        }

        if (clueCell != null)
        {
            clueCell.ResetAlpha();
        }
    }

    private IEnumerator BoxFadeInRoutine(int id, SpriteRenderer box, Cell clueCell, Vector3 cellBaseScale)
    {
        if (box == null)
        {
            if (clueCell != null)
            {
                clueCell.ResetAlpha();
                clueCell.transform.localScale = cellBaseScale;
            }
            _ = _fadeCoroutines.Remove(id);
            yield break;
        }

        SpriteRenderer[] renderers = box.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            if (clueCell != null)
            {
                clueCell.ResetAlpha();
                clueCell.transform.localScale = cellBaseScale;
            }
            _ = _fadeCoroutines.Remove(id);
            yield break;
        }

        Color[] targetColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            targetColors[i] = renderers[i].color;
            Color start = renderers[i].color;
            start.a = 0f;
            renderers[i].color = start;
        }

        Vector3 baseScale = Vector3.one;
        box.transform.localScale = baseScale * 0.94f;

        if (clueCell != null)
        {
            clueCell.SetAlpha(0f);
            clueCell.transform.localScale = cellBaseScale * 0.94f;
        }

        float duration = _boxFadeInDuration;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (box == null)
            {
                break;
            }


            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float progress = Mathf.Sin(t * Mathf.PI * 0.5f);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    Color c = targetColors[i];
                    c.a = targetColors[i].a * progress;
                    renderers[i].color = c;
                }
            }

            float scaleProgress = 0.94f + (0.06f * progress);
            box.transform.localScale = baseScale * scaleProgress;

            if (clueCell != null)
            {
                clueCell.SetAlpha(progress);
                clueCell.transform.localScale = cellBaseScale * scaleProgress;
            }

            yield return null;
        }

        if (box != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = targetColors[i];
                }
            }
            box.transform.localScale = baseScale;
        }

        if (clueCell != null)
        {
            clueCell.ResetAlpha();
            clueCell.transform.localScale = cellBaseScale;
        }

        _ = _fadeCoroutines.Remove(id);
    }

    public void RemoveBox(int id)
    {
        if (_fadeCoroutines.TryGetValue(id, out FadeState state))
        {
            if (state.Coroutine != null)
            {
                StopCoroutine(state.Coroutine);
            }


            if (state.ClueCell != null)
            {
                state.ClueCell.ResetAlpha();
                state.ClueCell.transform.localScale = state.CellBaseScale;
            }
            _ = _fadeCoroutines.Remove(id);
        }

        if (_placedBoxes.TryGetValue(id, out SpriteRenderer box))
        {
            if (box != null)
            {
                Destroy(box.gameObject);
            }

            _ = _placedBoxes.Remove(id);
        }
    }

    public void ClearAllBoxes()
    {
        foreach ((_, FadeState state) in _fadeCoroutines)
        {
            if (state.Coroutine != null)
            {
                StopCoroutine(state.Coroutine);
            }


            if (state.ClueCell != null)
            {
                state.ClueCell.ResetAlpha();
                state.ClueCell.transform.localScale = state.CellBaseScale;
            }
        }
        _fadeCoroutines.Clear();

        foreach ((_, SpriteRenderer box) in _placedBoxes)
        {
            if (box != null)
            {
                Destroy(box.gameObject);
            }
        }

        _placedBoxes.Clear();
    }

    private void OnDisable()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }

        foreach ((_, FadeState state) in _fadeCoroutines)
        {
            if (state.Coroutine != null)
            {
                StopCoroutine(state.Coroutine);
            }


            if (state.ClueCell != null)
            {
                state.ClueCell.ResetAlpha();
                state.ClueCell.transform.localScale = state.CellBaseScale;
            }
        }
        _fadeCoroutines.Clear();
    }

    public bool TryGetPlacedBox(int id, out SpriteRenderer box)
    {
        return _placedBoxes.TryGetValue(id, out box) && box != null;
    }

    public void CompleteAllFadeInsImmediate()
    {
        foreach ((_, FadeState state) in _fadeCoroutines)
        {
            if (state.Coroutine != null)
            {
                StopCoroutine(state.Coroutine);
            }


            if (state.ClueCell != null)
            {
                state.ClueCell.ResetAlpha();
                state.ClueCell.transform.localScale = state.CellBaseScale;
            }
        }
        _fadeCoroutines.Clear();

        foreach ((_, SpriteRenderer box) in _placedBoxes)
        {
            if (box != null)
            {
                box.transform.localScale = Vector3.one;
                SpriteRenderer[] renderers = box.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (SpriteRenderer r in renderers)
                {
                    if (r != null)
                    {
                        Color c = r.color;
                        c.a = 1f;
                        r.color = c;
                    }
                }
            }
        }
    }

    public IEnumerator PopBoxRoutine(int regionId, float duration = 0.24f)
    {
        if (!_placedBoxes.TryGetValue(regionId, out SpriteRenderer box) || box == null)
        {

            yield break;
        }


        Vector3 baseScale = Vector3.one;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(t * Mathf.PI);
            box.transform.localScale = baseScale * (1.0f + (punch * 0.12f));
            yield return null;
        }

        if (box != null)
        {
            box.transform.localScale = baseScale;
        }

    }

    public void CalculateMaskBounds(Vector2Int min, Vector2Int max, float gridSize, out Vector3 center, out Vector2 size)
    {
        Vector3 boxCenter = GetBoxCenter(min, max, gridSize);
        Vector2 boxSize = GetBoxSize(min, max, gridSize);

        float maskWidth = boxSize.x - MaskWidthInset;
        float maskHeight = boxSize.y - MaskBottomMargin + MaskExtraTop;

        float bottomY = boxCenter.y - (boxSize.y * 0.5f) + MaskBottomMargin;
        float maskCenterY = bottomY + (maskHeight * 0.5f);

        center = new Vector3(boxCenter.x, maskCenterY, 0f);
        size = new Vector2(maskWidth, maskHeight);
    }

    private void UpdateBoxMask(SpriteMask mask, Vector2Int min, Vector2Int max, float gridSize)
    {
        CalculateMaskBounds(min, max, gridSize, out Vector3 center, out Vector2 size);
        mask.transform.position = center;
        mask.transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    public void ConfigureCellMask(Cell cell, float gridSize)
    {
        if (cell == null || !cell.IsNumber)
        {
            return;
        }


        cell.UpdateMaskBounds(MaskSprite, gridSize, BoxPadding, MaskWidthInset, MaskBottomMargin, MaskExtraTop);
    }

    public void InitializeCellMasks(Cell[,] cells, float gridSize)
    {
        if (cells == null)
        {
            return;
        }


        int rows = cells.GetLength(0);
        int cols = cells.GetLength(1);

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                Cell cell = cells[x, y];
                if (cell != null && cell.IsNumber)
                {
                    ConfigureCellMask(cell, gridSize);
                }
            }
        }
    }

    public void PositionBoxRenderer(SpriteRenderer box, Vector2Int min, Vector2Int max, float gridSize)
    {
        box.transform.position = GetBoxCenter(min, max, gridSize);
        box.size = GetBoxSize(min, max, gridSize);
    }

    public Vector3 GetBoxCenter(Vector2Int min, Vector2Int max, float gridSize)
    {
        return new(
            (min.x + max.x + 1) * 0.5f * gridSize,
            (min.y + max.y + 1) * 0.5f * gridSize,
            0f
        );
    }

    public Vector2 GetBoxSize(Vector2Int min, Vector2Int max, float gridSize)
    {
        int widthInCells = max.x - min.x + 1;
        int heightInCells = max.y - min.y + 1;
        return new(
            (widthInCells * gridSize) - BoxPadding,
            (heightInCells * gridSize) - BoxPadding
        );
    }
}