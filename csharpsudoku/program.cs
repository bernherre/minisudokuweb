using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MiniSudoku
{
    // Tamaños soportados
    public enum SudokuSize
    {
        Four = 4,   // subcuadros 2x2
        Six = 6     // subcuadros 3x2
    }

    // Reglas y utilidades de Sudoku desacopladas de la UI
    public sealed class SudokuEngine
    {
        public readonly int N;           // 4 o 6
        public readonly int BoxRows;     // 2 (para 4x4) o 2/3 (para 6x6)
        public readonly int BoxCols;
        private readonly Random _rng;

        public SudokuEngine(SudokuSize size, int? seed = null)
        {
            N = (int)size;
            if (N == 4) { BoxRows = 2; BoxCols = 2; }
            else if (N == 6) { BoxRows = 2; BoxCols = 3; }
            else throw new NotSupportedException("Solo 4x4 o 6x6");
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int[,] GenerateSolvedBoard()
        {
            var grid = new int[N, N];
            SolveBacktracking(grid);
            return grid;
        }

        public int[,] GeneratePuzzle(int clues)
        {
            // 1) Generar tablero completo
            var solved = GenerateSolvedBoard();
            var puzzle = (int[,])solved.Clone();

            // 2) Hacer huecos conservando unicidad (estrategia simple para 4x4/6x6)
            // Intentaremos quitar celdas al azar verificando que permanezca resoluble con una única solución.
            var cells = Enumerable.Range(0, N * N).OrderBy(_ => _rng.Next()).ToList();
            foreach (var idx in cells)
            {
                if (CountFilled(puzzle) <= clues) break;
                int r = idx / N, c = idx % N;
                int backup = puzzle[r, c];
                if (backup == 0) continue;
                puzzle[r, c] = 0;

                // Verificar unicidad de solución (rápido por ser tableros pequeños)
                int solutions = CountSolutions((int[,])puzzle.Clone(), limit: 2);
                if (solutions != 1)
                {
                    // Revertir si se rompe unicidad
                    puzzle[r, c] = backup;
                }
            }
            return puzzle;
        }

        public bool Solve(int[,] grid) => SolveBacktracking(grid);

        public bool IsValidPlacement(int[,] grid, int row, int col, int val)
        {
            if (val < 1 || val > N) return false;

            for (int i = 0; i < N; i++)
            {
                if (grid[row, i] == val) return false;
                if (grid[i, col] == val) return false;
            }

            int startRow = (row / BoxRows) * BoxRows;
            int startCol = (col / BoxCols) * BoxCols;
            for (int r = 0; r < BoxRows; r++)
                for (int c = 0; c < BoxCols; c++)
                    if (grid[startRow + r, startCol + c] == val)
                        return false;

            return true;
        }

        public bool IsCompleteAndValid(int[,] grid)
        {
            for (int r = 0; r < N; r++)
            {
                var seenR = new HashSet<int>();
                var seenC = new HashSet<int>();
                for (int c = 0; c < N; c++)
                {
                    int vr = grid[r, c];
                    int vc = grid[c, r];
                    if (vr < 1 || vr > N || !seenR.Add(vr)) return false;
                    if (vc < 1 || vc > N || !seenC.Add(vc)) return false;
                }
            }

            for (int br = 0; br < N; br += BoxRows)
                for (int bc = 0; bc < N; bc += BoxCols)
                {
                    var seen = new HashSet<int>();
                    for (int r = 0; r < BoxRows; r++)
                        for (int c = 0; c < BoxCols; c++)
                        {
                            int v = grid[br + r, bc + c];
                            if (v < 1 || v > N || !seen.Add(v)) return false;
                        }
                }
            return true;
        }

        private bool SolveBacktracking(int[,] grid)
        {
            if (!FindEmpty(grid, out var row, out var col)) return true;

            var candidates = Enumerable.Range(1, N).OrderBy(_ => _rng.Next());
            foreach (int val in candidates)
            {
                if (IsValidPlacement(grid, row, col, val))
                {
                    grid[row, col] = val;
                    if (SolveBacktracking(grid)) return true;
                    grid[row, col] = 0;
                }
            }
            return false;
        }

        private int CountSolutions(int[,] grid, int limit)
        {
            int count = 0;
            void Dfs()
            {
                if (count >= limit) return;
                if (!FindEmpty(grid, out var row, out var col))
                {
                    count++;
                    return;
                }
                for (int v = 1; v <= N && count < limit; v++)
                {
                    if (IsValidPlacement(grid, row, col, v))
                    {
                        grid[row, col] = v;
                        Dfs();
                        grid[row, col] = 0;
                    }
                }
            }
            Dfs();
            return count;
        }

        private static int CountFilled(int[,] grid)
        {
            int n = grid.GetLength(0), m = grid.GetLength(1), sum = 0;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < m; c++)
                    if (grid[r, c] != 0) sum++;
            return sum;
        }

        private static bool FindEmpty(int[,] grid, out int row, out int col)
        {
            int n = grid.GetLength(0), m = grid.GetLength(1);
            for (int r = 0; r < n; r++)
                for (int c = 0; c < m; c++)
                    if (grid[r, c] == 0)
                    {
                        row = r; col = c; return true;
                    }
            row = -1; col = -1; return false;
        }
    }

    // UI WinForms minimalista y limpia
    public sealed class MainForm : Form
    {
        private SudokuSize _currentSize = SudokuSize.Four;
        private SudokuEngine _engine;
        private int[,] _puzzle;    // puzzle actual (con ceros para celdas vacías)
        private int[,] _solution;  // solución del puzzle
        private TextBox[,] _cells;
        private TableLayoutPanel _grid;
        private ComboBox _sizeSelector;
        private ComboBox _difficultySelector;
        private Button _newBtn, _checkBtn, _solveBtn, _clearBtn;
        private Label _status;
        private Stopwatch _timer;
        private Timer _uiTimer;

        public MainForm()
        {
            Text = "MiniSudoku (4x4 / 6x6)";
            MinimumSize = new Size(520, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);

            _timer = new Stopwatch();
            _uiTimer = new Timer { Interval = 500 };
            _uiTimer.Tick += (s, e) => UpdateStatus();

            // Panel superior (controles)
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false
            };

            _sizeSelector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
            _sizeSelector.Items.Add("4x4");
            _sizeSelector.Items.Add("6x6");
            _sizeSelector.SelectedIndex = 0;
            _sizeSelector.SelectedIndexChanged += (s, e) =>
            {
                _currentSize = _sizeSelector.SelectedIndex == 0 ? SudokuSize.Four : SudokuSize.Six;
                InitializeNewGame();
            };

            _difficultySelector = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
            _difficultySelector.Items.AddRange(new[] { "Fácil", "Media", "Difícil" });
            _difficultySelector.SelectedIndex = 1;

            _newBtn = new Button { Text = "Nuevo", AutoSize = true };
            _newBtn.Click += (s, e) => InitializeNewGame();

            _checkBtn = new Button { Text = "Verificar", AutoSize = true };
            _checkBtn.Click += (s, e) => CheckBoard();

            _solveBtn = new Button { Text = "Resolver", AutoSize = true };
            _solveBtn.Click += (s, e) => ShowSolution();

            _clearBtn = new Button { Text = "Limpiar", AutoSize = true };
            _clearBtn.Click += (s, e) => ClearEditableCells();

            _status = new Label { AutoSize = true, Text = "Listo" };

            topPanel.Controls.Add(new Label { Text = "Tamaño:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            topPanel.Controls.Add(_sizeSelector);
            topPanel.Controls.Add(new Label { Text = "Dificultad:", AutoSize = true, Padding = new Padding(10, 6, 0, 0) });
            topPanel.Controls.Add(_difficultySelector);
            topPanel.Controls.Add(_newBtn);
            topPanel.Controls.Add(_checkBtn);
            topPanel.Controls.Add(_solveBtn);
            topPanel.Controls.Add(_clearBtn);
            topPanel.Controls.Add(new Label { Width = 20 });
            topPanel.Controls.Add(_status);

            // Grid
            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.White
            };

            Controls.Add(_grid);
            Controls.Add(topPanel);

            InitializeNewGame();
        }

        private void InitializeNewGame()
        {
            _engine = new SudokuEngine(_currentSize);
            var (minClues, maxClues) = ClueRangeForDifficulty(_currentSize, _difficultySelector.SelectedItem?.ToString() ?? "Media");
            int clues = new Random().Next(minClues, maxClues + 1);

            // Generar puzzle y guardar solución
            _puzzle = _engine.GeneratePuzzle(clues);
            _solution = (int[,])_puzzle.Clone();
            _engine.Solve(_solution);

            BuildGrid();
            FillPuzzle();
            _timer.Restart();
            _uiTimer.Start();
            UpdateStatus("Nuevo juego");
        }

        private static (int min, int max) ClueRangeForDifficulty(SudokuSize size, string difficulty)
        {
            int N = (int)size;
            int total = N * N;
            // rangos sencillos: más pistas (clues) → más fácil
            return difficulty switch
            {
                "Fácil" => ((int)(total * 0.65), (int)(total * 0.8)),
                "Difícil" => ((int)(total * 0.35), (int)(total * 0.5)),
                _ => ((int)(total * 0.5), (int)(total * 0.65)), // Media
            };
        }

        private void BuildGrid()
        {
            _grid.Controls.Clear();
            _grid.ColumnStyles.Clear();
            _grid.RowStyles.Clear();

            int N = (int)_currentSize;
            _grid.ColumnCount = N;
            _grid.RowCount = N;

            for (int i = 0; i < N; i++)
            {
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / N));
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / N));
            }

            _cells = new TextBox[N, N];
            var boxRows = _currentSize == SudokuSize.Four ? 2 : 2;
            var boxCols = _currentSize == SudokuSize.Four ? 2 : 3;

            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                {
                    var tb = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        TextAlign = HorizontalAlignment.Center,
                        Font = new Font("Segoe UI", N == 4 ? 20 : 18, FontStyle.Bold),
                        MaxLength = 1,
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.White
                    };

                    // Bordes más gruesos entre subcuadros
                    int top = (r % boxRows == 0) ? 3 : 1;
                    int left = (c % boxCols == 0) ? 3 : 1;
                    int bottom = (r == N - 1) ? 3 : 1;
                    int right = (c == N - 1) ? 3 : 1;
                    tb.Margin = new Padding(left, top, right, bottom);

                    tb.KeyPress += (s, e) =>
                    {
                        // Solo dígitos válidos 1..N
                        if (char.IsControl(e.KeyChar)) return;
                        int maxDigit = N >= 10 ? 9 : N; // aquí N es 4 o 6 → OK
                        if (!char.IsDigit(e.KeyChar) || e.KeyChar == '0' || (e.KeyChar - '0') > maxDigit)
                            e.Handled = true;
                    };

                    tb.TextChanged += (s, e) =>
                    {
                        if (tb.Tag is not Point p) return;
                        if (string.IsNullOrWhiteSpace(tb.Text)) { tb.BackColor = Color.White; return; }
                        if (!int.TryParse(tb.Text.Trim(), out int val)) { tb.BackColor = Color.MistyRose; return; }

                        // Validación rápida de reglas
                        var temp = ReadGrid();
                        if (_engine.IsValidPlacement(temp, p.Y, p.X, val))
                            tb.BackColor = Color.White;
                        else
                            tb.BackColor = Color.MistyRose;
                    };

                    tb.Tag = new Point(c, r);
                    _cells[r, c] = tb;
                    _grid.Controls.Add(tb, c, r);
                }
        }

        private void FillPuzzle()
        {
            int N = (int)_currentSize;
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                {
                    var tb = _cells[r, c];
                    int val = _puzzle[r, c];
                    if (val == 0)
                    {
                        tb.ReadOnly = false;
                        tb.Text = "";
                        tb.ForeColor = Color.Black;
                        tb.BackColor = Color.White;
                    }
                    else
                    {
                        tb.ReadOnly = true;
                        tb.Text = val.ToString();
                        tb.ForeColor = Color.DimGray;
                        tb.BackColor = Color.Gainsboro;
                    }
                }
        }

        private int[,] ReadGrid()
        {
            int N = (int)_currentSize;
            var g = new int[N, N];
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                {
                    var t = _cells[r, c].Text.Trim();
                    g[r, c] = int.TryParse(t, out int v) ? v : 0;
                }
            return g;
        }

        private void CheckBoard()
        {
            var g = ReadGrid();
            int N = (int)_currentSize;
            bool anyError = false;

            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                {
                    var tb = _cells[r, c];
                    if (tb.ReadOnly) continue;
                    var t = tb.Text.Trim();
                    if (string.IsNullOrEmpty(t)) { tb.BackColor = Color.LightYellow; anyError = true; continue; }
                    if (!int.TryParse(t, out int v) || v < 1 || v > N) { tb.BackColor = Color.MistyRose; anyError = true; continue; }
                    // Chequeo contra la solución
                    if (_solution[r, c] != v) { tb.BackColor = Color.MistyRose; anyError = true; }
                    else tb.BackColor = Color.Honeydew;
                }

            if (!anyError && _engine.IsCompleteAndValid(g))
            {
                _timer.Stop();
                _uiTimer.Stop();
                UpdateStatus("¡Correcto! 🎉");
                MessageBox.Show($"¡Ganaste! Tiempo: {FormatElapsed()}", "MiniSudoku",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                UpdateStatus("Aún hay errores o celdas vacías.");
            }
        }

        private void ShowSolution()
        {
            int N = (int)_currentSize;
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                {
                    _cells[r, c].Text = _solution[r, c].ToString();
                    _cells[r, c].BackColor = _cells[r, c].ReadOnly ? Color.Gainsboro : Color.Honeydew;
                }
            _timer.Stop();
            _uiTimer.Stop();
            UpdateStatus("Solución mostrada");
        }

        private void ClearEditableCells()
        {
            int N = (int)_currentSize;
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                {
                    if (!_cells[r, c].ReadOnly)
                    {
                        _cells[r, c].Text = "";
                        _cells[r, c].BackColor = Color.White;
                    }
                }
            _timer.Restart();
            _uiTimer.Start();
            UpdateStatus("Celdas limpias");
        }

        private void UpdateStatus(string message = null)
        {
            if (message != null)
                _status.Text = $"{message} | Tiempo: {FormatElapsed()}";
            else
                _status.Text = $"Tiempo: {FormatElapsed()}";
        }

        private string FormatElapsed()
        {
            var ts = _timer.Elapsed;
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
        }
    }

    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
