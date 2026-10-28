using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<int, Action> OnGameWon;

    [SerializeField] private float _gridSize = 2f;
    [SerializeField] private ThemeDatabase _themes;
    [SerializeField] private LevelManager _levelManager;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private BoxManager _boxManager;
    [SerializeField] private InputHandler _inputHandler;
    [SerializeField] private UndoManager _undoManager;
    [SerializeField] private TutorialGhostManager _tutorialGhost;

    private struct SolvedCatItem
    {
        public int RegionId;
        public Vector2Int CluePos;
        public int TopY;
        public int LeftX;
        public Cell ClueCell;
    }

    private Camera _mainCamera;
    private Coroutine _winSequenceCoroutine;

    private void Awake()
    {
        Instance = this;

        _mainCamera = Camera.main;
        _boxManager.Init();


        _tutorialGhost = GetComponent<TutorialGhostManager>() ?? gameObject.AddComponent<TutorialGhostManager>();
        _tutorialGhost.Init(_boxManager, _inputHandler, _themes, _gridSize);

        _inputHandler.OnSelectionChanged += HandleSelectionChanged;
        _inputHandler.OnSelectionConfirmed += HandleSelectionConfirmed;
        _inputHandler.OnCellTapped += HandleCellTapped;

        LevelManager.OnLevelStarted += HandleLevelStarted;
        _levelManager.Init();

        UIManager.OnRestartRequested += RestartCurrentLevel;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void OnDestroy()
    {
        UIManager.OnRestartRequested -= RestartCurrentLevel;

        LevelManager.OnLevelStarted -= HandleLevelStarted;

        if (_winSequenceCoroutine != null)
        {
            StopCoroutine(_winSequenceCoroutine);
            _winSequenceCoroutine = null;
        }

        if (_inputHandler != null)
        {
            _inputHandler.OnSelectionChanged -= HandleSelectionChanged;
            _inputHandler.OnSelectionConfirmed -= HandleSelectionConfirmed;
            _inputHandler.OnCellTapped -= HandleCellTapped;
        }
    }

    private void HandleSelectionChanged(Vector2Int min, Vector2Int max)
    {
        _boxManager.ShowPreview(_gridManager.Cells, min, max, _gridSize);
    }

    private void HandleSelectionConfirmed(Vector2Int min, Vector2Int max)
    {
        // Only place a box if exactly one number is enclosed
        if (ShikakuValidator.TryGetSingleEnclosedNumber(_gridManager.Cells, min, max, out int numberId))
        {
            _boxManager.HidePreview();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.BoxPlaced);
            }
            _undoManager.RecordStateBeforeMove();

            RemoveBoxFromGame(numberId);

            // Remove any overlapping existing boxes
            HashSet<int> overlapping = _gridManager.GetOverlappingRegions(min, max, numberId);
            foreach (int existingId in overlapping)
            {
                RemoveBoxFromGame(existingId);
            }
            _undoManager.RegisterBoxesRemoved(overlapping);

            _undoManager.RegisterBoxPlaced(numberId, min, max);
            _gridManager.SetRegion(min, max, numberId);
            CatTheme theme = _themes.GetTheme(numberId);
            _boxManager.SpawnOrUpdateBox(numberId, min, max, theme, _gridSize, _gridManager.Cells);

            if (_tutorialGhost != null)
            {
                _tutorialGhost.OnBoardUpdated(_gridManager.Cells);
            }

            Level level = _levelManager.CurrentLevel;
            if (ShikakuValidator.CheckWin(_gridManager.Cells, level))
            {
                _inputHandler.IsInputBlocked = true;
                if (_winSequenceCoroutine != null)
                {
                    StopCoroutine(_winSequenceCoroutine);
                }
                _winSequenceCoroutine = StartCoroutine(PlayWinSequenceCoroutine(level));
            }
        }
        else
        {
            int cellCount = (max.x - min.x + 1) * (max.y - min.y + 1);
            if (cellCount > 1)
            {
                _boxManager.PlayInvalidMoveShake(min, max, _gridSize);
                HapticsManager.Trigger();
            }
            else
            {
                _boxManager.HidePreview();
            }
        }
    }

    private List<SolvedCatItem> GetSortedSolvedItems(Level level)
    {
        List<SolvedCatItem> items = new();
        if (level == null || level.Clues == null || _gridManager.Cells == null)
        {

            return items;
        }

        int rows = level.Rows;
        int cols = level.Cols;

        foreach (Level.Clue clue in level.Clues)
        {
            int regId = clue.RegionId;

            // Find bounds of this region from current grid cells
            int minX = rows, maxX = -1;
            int minY = cols, maxY = -1;

            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < cols; y++)
                {
                    if (_gridManager.Cells[x, y] != null && _gridManager.Cells[x, y].Id == regId)
                    {
                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }
            }

            // Fallback to solution bounds if needed
            if (maxX < 0 && level.Solution != null)
            {
                foreach (Level.SolutionBox sol in level.Solution)
                {
                    if (sol.RegionId == regId)
                    {
                        minX = sol.Min.x;
                        maxX = sol.Max.x;
                        minY = sol.Min.y;
                        maxY = sol.Max.y;
                        break;
                    }
                }
            }

            Cell clueCell = null;
            if (clue.Position.x >= 0 && clue.Position.x < rows &&
                clue.Position.y >= 0 && clue.Position.y < cols)
            {
                clueCell = _gridManager.Cells[clue.Position.x, clue.Position.y];
            }

            items.Add(new SolvedCatItem
            {
                RegionId = regId,
                CluePos = clue.Position,
                TopY = maxY >= 0 ? maxY : clue.Position.y,
                LeftX = minX < rows ? minX : clue.Position.x,
                ClueCell = clueCell
            });
        }

        // Sort: Top to bottom (TopY descending), then Left to right (LeftX ascending)
        items.Sort((a, b) =>
        {
            int cmpY = b.TopY.CompareTo(a.TopY);
            return cmpY != 0 ? cmpY : a.LeftX.CompareTo(b.LeftX);
        });

        return items;
    }

    private static readonly WaitForSeconds _winPopupDelay = new(0.6f);
    private IEnumerator PlayWinSequenceCoroutine(Level level)
    {
        _inputHandler.IsInputBlocked = true;

        if (_tutorialGhost != null)
        {
            _tutorialGhost.OnLevelCompleted();
        }

        if (_boxManager != null)
        {
            _boxManager.CompleteAllFadeInsImmediate();
        }

        // 1. Gather all solved boxes and their clue cells, sorted left-to-right, top-to-bottom
        List<SolvedCatItem> items = GetSortedSolvedItems(level);

        float popDuration = 0.24f;
        float delayBetweenPops = items.Count > 6
            ? Mathf.Clamp(1.0f / items.Count, 0.08f, 0.16f)
            : 0.16f;

        // 2. Pop each cat & corresponding box one-by-one
        for (int i = 0; i < items.Count; i++)
        {
            SolvedCatItem item = items[i];

            // Turn cat sprite to solved sprite & pop cat
            if (item.ClueCell != null)
            {
                _ = StartCoroutine(item.ClueCell.PopSolvedRoutine(popDuration));
            }

            // Pop corresponding cat's box
            if (_boxManager != null)
            {
                _ = StartCoroutine(_boxManager.PopBoxRoutine(item.RegionId, popDuration));
            }

            // Pop Sound & Haptic for each cat
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.BoxPlaced);
            }
            HapticsManager.Trigger();

            // Wait before popping the next cat/box
            if (i < items.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenPops);
            }
        }

        // 3. Ensure all cats are marked solved and allow final pop animation to settle
        if (_gridManager != null)
        {
            _gridManager.SetAllCatsSolved(level);
        }

        yield return new WaitForSeconds(popDuration + 0.15f);

        // 4. Play grand level completed sound & haptic
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.LevelCompleted);
        }
        HapticsManager.Trigger();

        yield return _winPopupDelay;

        // 5. Open Win popup
        int levelNumber = (_levelManager != null ? _levelManager.CurrentLevelIndex : 0) + 1;
        OnGameWon?.Invoke(levelNumber, () =>
        {
            if (_levelManager != null)
            {
                _levelManager.AdvanceToNextLevel();
            }
        });

        _winSequenceCoroutine = null;
    }

    private void HandleCellTapped(Vector2Int pos)
    {
        _boxManager.HidePreview();

        int existingId = _gridManager.Cells[pos.x, pos.y].Id;
        if (existingId >= 0)
        {
            _undoManager.RecordStateBeforeMove();
            RemoveBoxFromGame(existingId);
            _undoManager.RegisterBoxRemoved(existingId);

            if (_tutorialGhost != null)
            {
                _tutorialGhost.OnBoardUpdated(_gridManager.Cells);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.BoxRemoved);
            }

            HapticsManager.Trigger();
        }
    }

    private void HandleLevelStarted()
    {
        Level level = _levelManager.CurrentLevel;
        if (level == null)
        {
            Debug.LogWarning("[GameManager] Attempted to load a null level.");
            return;
        }

        _inputHandler.IsInputBlocked = true;

        _boxManager.ClearAllBoxes();
        _boxManager.HidePreview();
        _undoManager.ClearHistory();

        _gridManager.SpawnGrid(level, _gridSize, _themes);
        _boxManager.InitializeCellMasks(_gridManager.Cells, _gridSize);

        _inputHandler.Init(_mainCamera, _gridSize, level.Rows, level.Cols);
        _inputHandler.IsInputBlocked = false;

        if (_tutorialGhost != null)
        {
            _tutorialGhost.OnLevelStarted(level, _gridSize, _gridManager.Cells);
        }
    }

    public bool UseHint()
    {
        Level level = _levelManager != null ? _levelManager.CurrentLevel : null;
        if (level == null || level.Solution == null || level.Solution.Count == 0)
        {
            return false;
        }

        // 1. Find the first solution box that is not yet correctly placed
        Level.SolutionBox targetBox = default;
        bool found = false;

        foreach (Level.SolutionBox sol in level.Solution)
        {
            bool isCorrect = true;
            for (int x = sol.Min.x; x <= sol.Max.x; x++)
            {
                for (int y = sol.Min.y; y <= sol.Max.y; y++)
                {
                    if (_gridManager.Cells[x, y].Id != sol.RegionId)
                    {
                        isCorrect = false;
                        break;
                    }
                }

                if (!isCorrect)
                {
                    break;
                }
            }

            if (!isCorrect)
            {
                targetBox = sol;
                found = true;
                break;
            }
        }

        if (!found)
        {
            return false; // Board is already fully correct
        }

        // 2. Prepare undo snapshot
        _undoManager.RecordStateBeforeMove();

        // 3. Remove existing box with this RegionId (if player placed an incorrect shape)
        RemoveBoxFromGame(targetBox.RegionId);

        // 4. Remove any overlapping boxes
        HashSet<int> overlapping = _gridManager.GetOverlappingRegions(targetBox.Min, targetBox.Max, targetBox.RegionId);
        foreach (int existingId in overlapping)
        {
            RemoveBoxFromGame(existingId);
        }
        _undoManager.RegisterBoxesRemoved(overlapping);

        // 5. Place the correct hint box
        _undoManager.RegisterBoxPlaced(targetBox.RegionId, targetBox.Min, targetBox.Max);
        _gridManager.SetRegion(targetBox.Min, targetBox.Max, targetBox.RegionId);
        CatTheme theme = _themes.GetTheme(targetBox.RegionId);
        _boxManager.SpawnOrUpdateBox(targetBox.RegionId, targetBox.Min, targetBox.Max, theme, _gridSize, _gridManager.Cells);

        if (_tutorialGhost != null)
        {
            _tutorialGhost.OnBoardUpdated(_gridManager.Cells);
        }

        // 6. Audio & Haptics
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.BoxPlaced);
        }
        HapticsManager.Trigger();

        // 7. Check if applying the hint completed the level
        if (ShikakuValidator.CheckWin(_gridManager.Cells, level))
        {
            _inputHandler.IsInputBlocked = true;
            if (_winSequenceCoroutine != null)
            {
                StopCoroutine(_winSequenceCoroutine);
            }
            _winSequenceCoroutine = StartCoroutine(PlayWinSequenceCoroutine(level));
        }

        return true;
    }

    public void RestartCurrentLevel()
    {
        if (_winSequenceCoroutine != null)
        {
            StopCoroutine(_winSequenceCoroutine);
            _winSequenceCoroutine = null;
        }

        if (_levelManager != null)
        {
            _levelManager.RestartCurrentLevel();
        }
    }

    [ContextMenu("Reset Level Progress to Level 1")]
    public void ResetLevelProgress()
    {
        if (_levelManager != null)
        {
            _levelManager.LoadLevelByIndex(0);
        }
    }

    private void RemoveBoxFromGame(int id)
    {
        _gridManager.CleanRegion(id);
        _boxManager.RemoveBox(id);
    }
}