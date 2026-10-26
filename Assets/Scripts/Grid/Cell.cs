using System.Collections;
using TMPro;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public const int EmptyId = -2;
    public const int PreviewId = -1;

    [Header("Renderers")]
    [SerializeField] private SpriteRenderer _boxRenderer;
    [SerializeField] private SpriteRenderer _catRenderer;
    [SerializeField] private SpriteMask _cellMask;
    [SerializeField] private TMP_Text _numbersText;

    [Header("Tile Styling")]
    [SerializeField] private Sprite _emptyTileSprite;
    [SerializeField] private Color _emptyTileColor = new(0.925f, 0.784f, 0.698f); // Warm peach (#ECC8B2)

    private Canvas _numbersCanvas;
    private Renderer _numbersRenderer;

    public int Id { get; set; } = EmptyId;
    public bool IsNumber { get; private set; }
    public int Value { get; private set; }

    private CatTheme _currentTheme;

    private void Awake()
    {
        if (_numbersText != null)
        {
            _numbersCanvas = _numbersText.canvas;
            _numbersRenderer = _numbersText.GetComponent<Renderer>();
        }
    }

    public void Init(bool isNumber, int regionId, int numberValue, CatTheme theme)
    {
        IsNumber = isNumber;
        Id = isNumber ? regionId : EmptyId;
        _currentTheme = theme;

        _boxRenderer.sprite = _emptyTileSprite;
        _boxRenderer.color = _emptyTileColor;
        _boxRenderer.sortingOrder = 0;

        int catSortingOrder = SortingOrderConfig.GetCatOrder(Id);

        if (_catRenderer != null)
        {
            _catRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            _catRenderer.sortingOrder = catSortingOrder;
        }

        if (_cellMask != null)
        {
            _cellMask.enabled = IsNumber;
            _cellMask.isCustomRangeActive = true;
            _cellMask.frontSortingOrder = catSortingOrder;
            _cellMask.backSortingOrder = SortingOrderConfig.GetMaskBackOrder(Id);
        }

        if (IsNumber)
        {
            Value = numberValue;

            _catRenderer.gameObject.SetActive(true);
            _catRenderer.sprite = _currentTheme.catSprite;

            _numbersText.gameObject.SetActive(true);
            _numbersText.SetText("{0}", Value);
            _numbersText.color = Color.white;
            _numbersText.raycastTarget = false;

            int textOrder = SortingOrderConfig.GetTextOrder(Id);
            if (_numbersCanvas != null)
            {
                _numbersCanvas.overrideSorting = true;
                _numbersCanvas.sortingOrder = textOrder;
            }
            else if (_numbersRenderer != null)
            {
                _numbersRenderer.sortingOrder = textOrder;
            }
        }
        else
        {
            Value = 0;
            _catRenderer.gameObject.SetActive(false);
            _numbersText.gameObject.SetActive(false);
        }

        UpdateTileVisual();
    }

    public SpriteMask CellMask => _cellMask;

    public void SetMaskActive(bool active)
    {
        if (_cellMask != null)
        {
            _cellMask.enabled = active;
        }
    }

    public void UpdateMaskBounds(
        Sprite maskSprite,
        float gridSize,
        float boxPadding,
        float maskWidthInset,
        float maskBottomMargin,
        float maskExtraTop)
    {
        if (_cellMask == null)
        {
            return;
        }

        if (maskSprite != null)
        {
            _cellMask.sprite = maskSprite;
        }

        Vector3 scale = transform.localScale;
        float parentScaleX = Mathf.Abs(scale.x) > 0.0001f ? scale.x : 1f;
        float parentScaleY = Mathf.Abs(scale.y) > 0.0001f ? scale.y : 1f;

        float boxWidth = gridSize - boxPadding;
        float boxHeight = gridSize - boxPadding;

        float maskWidth = boxWidth - maskWidthInset;
        float maskHeight = boxHeight - maskBottomMargin + maskExtraTop;

        float bottomOffset = (-boxHeight * 0.5f) + maskBottomMargin;
        float centerYOffset = bottomOffset + (maskHeight * 0.5f);

        _cellMask.transform.localPosition = new Vector3(0f, centerYOffset / parentScaleY, 0f);
        _cellMask.transform.localScale = new Vector3(maskWidth / parentScaleX, maskHeight / parentScaleY, 1f);
    }

    public void UpdateMaskFromBoxManager(BoxManager boxManager, float gridSize)
    {
        if (boxManager != null)
        {
            UpdateMaskBounds(
                boxManager.MaskSprite,
                gridSize,
                boxManager.BoxPadding,
                boxManager.MaskWidthInset,
                boxManager.MaskBottomMargin,
                boxManager.MaskExtraTop
            );
        }
        else
        {
            UpdateMaskBounds(
                null,
                gridSize,
                BoxManager.DefaultBoxPadding,
                BoxManager.DefaultMaskWidthInset,
                BoxManager.DefaultMaskBottomMargin,
                BoxManager.DefaultMaskExtraTop
            );
        }
    }

    public void SetAlpha(float alpha)
    {
        if (_catRenderer != null)
        {
            Color c = _catRenderer.color;
            c.a = alpha;
            _catRenderer.color = c;
        }

        if (_boxRenderer != null)
        {
            Color c = _boxRenderer.color;
            c.a = alpha;
            _boxRenderer.color = c;
        }

        if (_numbersText != null)
        {
            _numbersText.alpha = alpha;
        }
    }

    public void ResetAlpha()
    {
        SetAlpha(1f);
    }

    public void Clear()
    {
        if (IsNumber)
        {
            SetMaskActive(true);
            ResetAlpha();
        }
        else
        {
            Id = EmptyId;
            UpdateTileVisual();
        }
    }

    public void SetSolved()
    {
        if (!IsNumber)
        {
            return;
        }

        if (_currentTheme.catSolvedSprite != null)
        {
            _catRenderer.sprite = _currentTheme.catSolvedSprite;
        }
    }

    public IEnumerator PopSolvedRoutine(float duration = 0.24f)
    {
        SetSolved();

        Vector3 baseScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float punch = Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseScale * (1.0f + (punch * 0.18f));
            yield return null;
        }

        transform.localScale = baseScale;
    }

    public void UpdateTileVisual()
    {
        if (IsNumber)
        {
            _boxRenderer.sprite = _currentTheme.boxSprite;
            _boxRenderer.color = Color.white;
        }
        else
        {
            // When covered by a box, can show base tile (the 9-slice box sits above on layer 1)
            _boxRenderer.sprite = _emptyTileSprite;
            _boxRenderer.color = _emptyTileColor;
        }
    }
}
