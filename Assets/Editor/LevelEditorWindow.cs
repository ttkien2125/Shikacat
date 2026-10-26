using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelEditorWindow : EditorWindow
{
    private enum ToolMode
    {
        PaintRegion,
        ToggleClue,
        Erase
    }

    [MenuItem("Window/Shikacat/Level Editor", priority = 100)]
    [MenuItem("Tools/Shikacat Level Editor", priority = 100)]
    public static void Open()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Shikacat Editor");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }

    private static readonly Color[] PaletteColors = new Color[]
    {
        new(0.96f, 0.55f, 0.55f), // Coral Red
        new(0.55f, 0.75f, 0.95f), // Sky Blue
        new(0.98f, 0.82f, 0.48f), // Amber
        new(0.58f, 0.88f, 0.68f), // Mint Green
        new(0.80f, 0.65f, 0.95f), // Lavender
        new(0.98f, 0.65f, 0.80f), // Rose Pink
        new(0.50f, 0.88f, 0.88f), // Turquoise
        new(0.98f, 0.70f, 0.50f), // Orange
        new(0.72f, 0.82f, 0.55f), // Sage Green
        new(0.85f, 0.78f, 0.95f), // Lilac
        new(0.95f, 0.88f, 0.55f), // Lemon
        new(0.65f, 0.78f, 0.85f), // Slate Blue
        new(0.90f, 0.60f, 0.60f), // Pastel Brick
        new(0.70f, 0.90f, 0.80f), // Seafoam
        new(0.92f, 0.75f, 0.65f), // Warm Peach
        new(0.82f, 0.82f, 0.82f), // Silver
    };

    private Level _targetLevel;
    private int _rows = 5;
    private int _cols = 5;
    private int _inputRows = 5;
    private int _inputCols = 5;
    private int[,] _grid;      // -1 = empty, >= 0 = regionId
    private bool[,] _clues;    // true = clue cell

    private int _activeRegionId = 0;
    private ToolMode _currentTool = ToolMode.PaintRegion;
    private float _cellSize = 55f;
    private Vector2 _scrollPosition;

    private readonly List<string> _validationErrors = new();
    private bool _hasValidated = false;
    private bool _isValid = false;

    private int _clueDensityPercent = 20;
    private int _batchCount = 20;

    private void OnEnable()
    {
        if (_grid == null || _clues == null)
        {
            InitGrid(_rows, _cols);
        }
    }

    private void InitGrid(int rows, int cols)
    {
        _rows = Mathf.Clamp(rows, 2, 20);
        _cols = Mathf.Clamp(cols, 2, 20);
        _inputRows = _rows;
        _inputCols = _cols;

        _grid = new int[_rows, _cols];
        _clues = new bool[_rows, _cols];

        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _cols; y++)
            {
                _grid[x, y] = -1;
                _clues[x, y] = false;
            }
        }

        _hasValidated = false;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        DrawHeaderSection();
        EditorGUILayout.Space(6);
        DrawToolbarSection();
        EditorGUILayout.Space(6);
        DrawGeneratorSection();
        EditorGUILayout.Space(6);
        DrawPaletteSection();
        EditorGUILayout.Space(6);
        DrawGridSection();
        EditorGUILayout.Space(6);
        DrawValidationSection();
        EditorGUILayout.Space(10);
    }

    private void DrawHeaderSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Level Asset Configuration", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _targetLevel = (Level)EditorGUILayout.ObjectField("Active Level Asset", _targetLevel, typeof(Level), false);
            if (EditorGUI.EndChangeCheck() && _targetLevel != null)
            {
                LoadFromAsset(_targetLevel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create New Level Asset", GUILayout.Height(24)))
                {
                    CreateNewLevelAsset();
                }

                GUI.enabled = _targetLevel != null;
                if (GUILayout.Button("Reload from Asset", GUILayout.Height(24)))
                {
                    if (_targetLevel != null)
                    {
                        LoadFromAsset(_targetLevel);
                    }
                }

                if (GUILayout.Button("Save to Asset", GUILayout.Height(24)))
                {
                    SaveToAsset();
                }
                GUI.enabled = true;
            }
        }
    }

    private void DrawToolbarSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"Grid Dimensions (Current: {_rows}x{_cols})", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Rows (X):", GUILayout.Width(65));
                if (GUILayout.Button("-", GUILayout.Width(22)))
                {
                    _inputRows = Mathf.Max(2, _inputRows - 1);
                    ResizeGrid(_inputRows, _inputCols);
                }
                _inputRows = EditorGUILayout.IntField(_inputRows, GUILayout.Width(45));
                if (GUILayout.Button("+", GUILayout.Width(22)))
                {
                    _inputRows = Mathf.Min(20, _inputRows + 1);
                    ResizeGrid(_inputRows, _inputCols);
                }

                GUILayout.Space(15);

                EditorGUILayout.LabelField("Cols (Y):", GUILayout.Width(65));
                if (GUILayout.Button("-", GUILayout.Width(22)))
                {
                    _inputCols = Mathf.Max(2, _inputCols - 1);
                    ResizeGrid(_inputRows, _inputCols);
                }
                _inputCols = EditorGUILayout.IntField(_inputCols, GUILayout.Width(45));
                if (GUILayout.Button("+", GUILayout.Width(22)))
                {
                    _inputCols = Mathf.Min(20, _inputCols + 1);
                    ResizeGrid(_inputRows, _inputCols);
                }

                GUILayout.Space(15);

                bool sizeDiffers = _inputRows != _rows || _inputCols != _cols;
                Color prevBg = GUI.backgroundColor;
                if (sizeDiffers)
                {
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
                }

                if (GUILayout.Button(sizeDiffers ? $"Apply ({_inputRows}x{_inputCols})" : "Resize Grid", GUILayout.Height(20)))
                {
                    ResizeGrid(_inputRows, _inputCols);
                }
                GUI.backgroundColor = prevBg;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Presets:", GUILayout.Width(65));
                int[] presets = new int[] { 5, 6, 7, 8, 10 };
                foreach (int p in presets)
                {
                    if (GUILayout.Button($"{p}x{p}", GUILayout.Width(45)))
                    {
                        _inputRows = p;
                        _inputCols = p;
                        ResizeGrid(p, p);
                    }
                }
            }

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                if (_inputRows != _rows || _inputCols != _cols)
                {
                    ResizeGrid(_inputRows, _inputCols);
                    currentEvent.Use();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Mode:", GUILayout.Width(50));
                _currentTool = (ToolMode)GUILayout.Toolbar((int)_currentTool, new string[]
                {
                    "🎨 Paint Region",
                    "⭐ Toggle Clue",
                    "🧹 Erase"
                }, GUILayout.Height(24));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _cellSize = EditorGUILayout.Slider("Cell Visual Size", _cellSize, 35f, 80f);

                if (GUILayout.Button("Auto-Place Clues", GUILayout.Width(120)))
                {
                    AutoPlaceClues();
                }

                if (GUILayout.Button("Clear All", GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Clear Grid", "Are you sure you want to clear all cells?", "Yes", "Cancel"))
                    {
                        InitGrid(_rows, _cols);
                    }
                }
            }
        }
    }

    private void DrawGeneratorSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Procedural Random Generator", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                int targetClues = Mathf.Max(2, Mathf.RoundToInt(_rows * _cols * (_clueDensityPercent / 100f)));
                _clueDensityPercent = EditorGUILayout.IntSlider(new GUIContent($"Clue Density: {_clueDensityPercent}% ({targetClues} clues)", "Target percentage of total grid cells that are clues (~20%)"), _clueDensityPercent, 15, 25);

                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.6f, 0.95f, 0.7f); // Mint green
                if (GUILayout.Button("🎲 Generate into Canvas", GUILayout.Height(22)))
                {
                    GenerateIntoCanvas();
                }
                GUI.backgroundColor = prevColor;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _batchCount = EditorGUILayout.IntField("Batch Count", _batchCount, GUILayout.Width(130));

                Color prevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.75f, 0.85f, 1f); // Soft blue
                if (GUILayout.Button($"⚡ Batch Generate {_batchCount} Levels to Assets/Data/Levels/", GUILayout.Height(22)))
                {
                    BatchGenerateLevels();
                }
                GUI.backgroundColor = prevColor;
            }
        }
    }

    private void GenerateIntoCanvas()
    {
        int targetClues = Mathf.Max(2, Mathf.RoundToInt(_rows * _cols * (_clueDensityPercent / 100f)));
        ShikakuGenerator.GenerationResult result = ShikakuGenerator.Generate(_rows, _cols, targetClues);
        _grid = result.Grid;
        _clues = result.Clues;
        _hasValidated = false;
        Repaint();
        ShowNotification(new GUIContent($"Generated {_rows}x{_cols} puzzle ({result.SolutionList.Count} regions)!"));
    }

    private void BatchGenerateLevels()
    {
        string targetFolder = "Assets/Data/Levels";
        List<Level> generated = ShikakuGenerator.BatchGenerateLevels(targetFolder, _batchCount);
        ShowNotification(new GUIContent($"Generated {generated.Count} levels!"));
        _ = EditorUtility.DisplayDialog("Batch Generation Complete",
            $"Successfully generated {generated.Count} valid Shikaku levels in:\n{targetFolder}\n\nLevels range from 5x5 to 10x10 with progressive difficulty.", "OK");
    }

    private void DrawPaletteSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Region Palette (Select region to paint)", EditorStyles.boldLabel);

            int maxRegions = 12;
            int buttonsPerRow = 6;

            for (int row = 0; row < Mathf.CeilToInt((float)maxRegions / buttonsPerRow); row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int col = 0; col < buttonsPerRow; col++)
                    {
                        int id = (row * buttonsPerRow) + col;
                        if (id >= maxRegions) { break; }

                        Color color = GetColorForRegion(id);
                        Color prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = color;

                        bool isSelected = _activeRegionId == id && _currentTool == ToolMode.PaintRegion;
                        string label = isSelected ? $"▶ Region {id} ◀" : $"Region {id}";

                        if (GUILayout.Button(label, GUILayout.Height(26)))
                        {
                            _activeRegionId = id;
                            _currentTool = ToolMode.PaintRegion;
                        }

                        GUI.backgroundColor = prevColor;
                    }
                }
            }

            EditorGUILayout.HelpBox("Tip: Left-click paints the active tool. Right-click any cell to instantly toggle Clue status!", MessageType.None);
        }
    }

    private void DrawGridSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"Grid Canvas ({_rows} x {_cols})", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(400));

            // Render columns from 0..Rows-1 and rows from Cols-1 down to 0 (Cartesian Y up)
            for (int y = _cols - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Y={y:D2}", GUILayout.Width(35), GUILayout.Height(_cellSize));

                    for (int x = 0; x < _rows; x++)
                    {
                        DrawCellButton(x, y);
                    }
                }
            }

            // X-axis coordinate labels at bottom
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("", GUILayout.Width(35));
                for (int x = 0; x < _rows; x++)
                {
                    GUILayout.Label($"X={x:D2}", GUILayout.Width(_cellSize));
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawCellButton(int x, int y)
    {
        int regionId = _grid[x, y];
        bool isClue = _clues[x, y];

        Color cellColor = regionId >= 0 ? GetColorForRegion(regionId) : new Color(0.28f, 0.28f, 0.28f);
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = cellColor;

        string content;
        if (isClue)
        {
            int regionSize = CalculateRegionSize(regionId);
            content = $"⭐ {regionSize}\n[R{regionId}]";
        }
        else
        {
            content = regionId >= 0 ? $"R{regionId}\n({x},{y})" : $"·\n({x},{y})";
        }

        GUIStyle style = new(GUI.skin.button)
        {
            fontSize = isClue ? 12 : 9,
            fontStyle = isClue ? FontStyle.Bold : FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter
        };

        Rect cellRect = GUILayoutUtility.GetRect(_cellSize, _cellSize, style);

        Event currentEvent = Event.current;
        if (cellRect.Contains(currentEvent.mousePosition))
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                if (currentEvent.button == 0) // Left click
                {
                    ApplyToolToCell(x, y);
                    currentEvent.Use();
                }
                else if (currentEvent.button == 1) // Right click -> instant clue toggle
                {
                    ToggleClueAt(x, y);
                    currentEvent.Use();
                }
            }
            else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
            {
                if (_currentTool is ToolMode.PaintRegion or ToolMode.Erase)
                {
                    ApplyToolToCell(x, y);
                    currentEvent.Use();
                }
            }
        }

        GUI.Box(cellRect, content, style);
        GUI.backgroundColor = prevColor;
    }

    private void DrawValidationSection()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Puzzle Validation", EditorStyles.boldLabel);
                if (GUILayout.Button("Validate Puzzle Now", GUILayout.Width(150), GUILayout.Height(22)))
                {
                    _ = RunValidation();
                }
            }

            if (_hasValidated)
            {
                if (_isValid)
                {
                    EditorGUILayout.HelpBox($"✓ Puzzle is valid! All {_rows * _cols} cells are covered by valid rectangles with exactly 1 clue each.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Puzzle validation failed with {_validationErrors.Count} error(s):", MessageType.Error);
                    foreach (string err in _validationErrors)
                    {
                        EditorGUILayout.LabelField($"• {err}", EditorStyles.wordWrappedMiniLabel);
                    }
                }
            }
        }
    }

    private void ApplyToolToCell(int x, int y)
    {
        switch (_currentTool)
        {
            case ToolMode.PaintRegion:
                _grid[x, y] = _activeRegionId;
                break;
            case ToolMode.ToggleClue:
                ToggleClueAt(x, y);
                break;
            case ToolMode.Erase:
                _grid[x, y] = -1;
                _clues[x, y] = false;
                break;
            default:
                break;
        }

        _hasValidated = false;
        Repaint();
    }

    private void ToggleClueAt(int x, int y)
    {
        if (_grid[x, y] < 0)
        {
            return; // Cannot put clue on empty cell
        }

        _clues[x, y] = !_clues[x, y];
        _hasValidated = false;
        Repaint();
    }

    private int CalculateRegionSize(int regionId)
    {
        if (regionId < 0)
        {
            return 0;
        }

        int count = 0;
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _cols; y++)
            {
                if (_grid[x, y] == regionId)
                {
                    count++;
                }
            }
        }
        return count;
    }

    private Color GetColorForRegion(int id)
    {
        return id < 0 ? Color.gray : PaletteColors[id % PaletteColors.Length];
    }

    private void ResizeGrid(int newRows, int newCols)
    {
        newRows = Mathf.Clamp(newRows, 2, 20);
        newCols = Mathf.Clamp(newCols, 2, 20);

        int[,] oldGrid = _grid;
        bool[,] oldClues = _clues;
        int oldRows = _rows;
        int oldCols = _cols;

        InitGrid(newRows, newCols);

        int copyRows = Mathf.Min(oldRows, newRows);
        int copyCols = Mathf.Min(oldCols, newCols);

        for (int x = 0; x < copyRows; x++)
        {
            for (int y = 0; y < copyCols; y++)
            {
                _grid[x, y] = oldGrid[x, y];
                _clues[x, y] = oldClues[x, y];
            }
        }

        Repaint();
    }

    private void AutoPlaceClues()
    {
        HashSet<int> regionsFound = new();
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _cols; y++)
            {
                int id = _grid[x, y];
                if (id >= 0)
                {
                    _ = regionsFound.Add(id);
                }
            }
        }

        // Clear existing clues
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _cols; y++)
            {
                _clues[x, y] = false;
            }
        }

        // For each region, find the first cell and set it as clue
        foreach (int regionId in regionsFound)
        {
            bool placed = false;
            for (int x = 0; x < _rows && !placed; x++)
            {
                for (int y = 0; y < _cols && !placed; y++)
                {
                    if (_grid[x, y] == regionId)
                    {
                        _clues[x, y] = true;
                        placed = true;
                    }
                }
            }
        }

        _hasValidated = false;
        Repaint();
    }

    private bool RunValidation()
    {
        _validationErrors.Clear();
        _hasValidated = true;

        // 1. Check all cells filled
        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _cols; y++)
            {
                if (_grid[x, y] < 0)
                {
                    _validationErrors.Add($"Cell ({x}, {y}) is empty. All cells must belong to a region.");
                }
            }
        }

        // 2. Identify all regions
        Dictionary<int, List<Vector2Int>> regionCells = new();
        Dictionary<int, List<Vector2Int>> regionClues = new();

        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _cols; y++)
            {
                int id = _grid[x, y];
                if (id >= 0)
                {
                    if (!regionCells.ContainsKey(id))
                    {
                        regionCells[id] = new List<Vector2Int>();
                        regionClues[id] = new List<Vector2Int>();
                    }

                    regionCells[id].Add(new Vector2Int(x, y));

                    if (_clues[x, y])
                    {
                        regionClues[id].Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        if (regionCells.Count == 0)
        {
            _validationErrors.Add("Grid has no regions painted.");
        }

        // 3. Verify each region is a solid rectangle and has exactly 1 clue
        foreach ((int id, List<Vector2Int> cells) in regionCells)
        {
            List<Vector2Int> clues = regionClues[id];

            // Find bounds
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (Vector2Int pos in cells)
            {
                if (pos.x < minX) { minX = pos.x; }
                if (pos.x > maxX) { maxX = pos.x; }
                if (pos.y < minY) { minY = pos.y; }
                if (pos.y > maxY) { maxY = pos.y; }
            }

            int expectedArea = (maxX - minX + 1) * (maxY - minY + 1);

            if (cells.Count != expectedArea)
            {
                _validationErrors.Add($"Region {id} is not a solid rectangle! (Spans {maxX - minX + 1}x{maxY - minY + 1} = {expectedArea} cells, but contains {cells.Count} painted cells).");
            }
            else
            {
                // Verify all cells in bounding box have ID
                for (int bx = minX; bx <= maxX; bx++)
                {
                    for (int by = minY; by <= maxY; by++)
                    {
                        if (_grid[bx, by] != id)
                        {
                            _validationErrors.Add($"Region {id} has gaps or overlaps inside its bounding box at ({bx}, {by}).");
                        }
                    }
                }
            }

            // Check clue count
            if (clues.Count == 0)
            {
                _validationErrors.Add($"Region {id} (size {cells.Count}) has NO clue cell assigned.");
            }
            else if (clues.Count > 1)
            {
                _validationErrors.Add($"Region {id} has {clues.Count} clues. Each region must have exactly ONE clue.");
            }
        }

        _isValid = _validationErrors.Count == 0;
        return _isValid;
    }

    private void CreateNewLevelAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create New Level", "Level_New", "asset", "Save new level asset", "Assets/Data/Levels");
        if (string.IsNullOrEmpty(path)) { return; }

        Level newLevel = CreateInstance<Level>();
        newLevel.Rows = _rows;
        newLevel.Cols = _cols;

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        _targetLevel = newLevel;
        Selection.activeObject = newLevel;
        ShowNotification(new GUIContent($"Created {newLevel.name}"));
    }

    private void LoadFromAsset(Level level)
    {
        if (level == null) { return; }

        InitGrid(level.Rows, level.Cols);

        // Load clues
        if (level.Clues != null)
        {
            foreach (Level.Clue clue in level.Clues)
            {
                if (clue.Position.x >= 0 && clue.Position.x < _rows &&
                    clue.Position.y >= 0 && clue.Position.y < _cols)
                {
                    _clues[clue.Position.x, clue.Position.y] = true;
                    _grid[clue.Position.x, clue.Position.y] = clue.RegionId;
                }
            }
        }

        // Load solution boxes
        if (level.Solution != null)
        {
            foreach (Level.SolutionBox box in level.Solution)
            {
                for (int x = box.Min.x; x <= box.Max.x; x++)
                {
                    for (int y = box.Min.y; y <= box.Max.y; y++)
                    {
                        if (x >= 0 && x < _rows && y >= 0 && y < _cols)
                        {
                            _grid[x, y] = box.RegionId;
                        }
                    }
                }
            }
        }

        _hasValidated = false;
        Repaint();
        ShowNotification(new GUIContent($"Loaded {level.name}"));
    }

    private void SaveToAsset()
    {
        if (_targetLevel == null)
        {
            _ = EditorUtility.DisplayDialog("Error", "Please assign or create a Level asset first.", "OK");
            return;
        }

        if (!RunValidation())
        {
            bool proceed = EditorUtility.DisplayDialog("Validation Warnings",
                $"Level has {_validationErrors.Count} validation error(s). Do you still want to save?", "Save Anyway", "Cancel");
            if (!proceed) { return; }
        }

        // Bake data into level asset
        _targetLevel.Rows = _rows;
        _targetLevel.Cols = _cols;
        _targetLevel.Clues.Clear();
        _targetLevel.Solution.Clear();

        // 1. Reconstruct Solution boxes from regions
        Dictionary<int, List<Vector2Int>> regionCells = new();
        Dictionary<int, Vector2Int> regionCluePos = new();

        for (int x = 0; x < _rows; x++)
        {
            for (int y = 0; y < _cols; y++)
            {
                int id = _grid[x, y];
                if (id >= 0)
                {
                    if (!regionCells.ContainsKey(id))
                    {
                        regionCells[id] = new List<Vector2Int>();
                    }
                    regionCells[id].Add(new Vector2Int(x, y));

                    if (_clues[x, y])
                    {
                        regionCluePos[id] = new Vector2Int(x, y);
                    }
                }
            }
        }

        foreach ((int id, List<Vector2Int> cells) in regionCells)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (Vector2Int pos in cells)
            {
                if (pos.x < minX) { minX = pos.x; }
                if (pos.x > maxX) { maxX = pos.x; }
                if (pos.y < minY) { minY = pos.y; }
                if (pos.y > maxY) { maxY = pos.y; }
            }

            Vector2Int min = new(minX, minY);
            Vector2Int max = new(maxX, maxY);
            int area = (maxX - minX + 1) * (maxY - minY + 1);

            _targetLevel.Solution.Add(new Level.SolutionBox(id, min, max));

            // Clue
            if (regionCluePos.TryGetValue(id, out Vector2Int cluePos))
            {
                _targetLevel.Clues.Add(new Level.Clue(cluePos, area, id));
            }
            else
            {
                // Fallback: use first cell
                _targetLevel.Clues.Add(new Level.Clue(min, area, id));
            }
        }

        _targetLevel.InvalidateCache();
        EditorUtility.SetDirty(_targetLevel);
        AssetDatabase.SaveAssets();

        ShowNotification(new GUIContent($"Saved {_targetLevel.name} successfully!"));
    }
}

