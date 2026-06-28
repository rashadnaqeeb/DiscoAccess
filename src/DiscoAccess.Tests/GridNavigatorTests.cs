using System.Collections.Generic;
using DiscoAccess.Core.UI.Nav;
using Xunit;

namespace DiscoAccess.Tests
{
    public class GridNavigatorTests
    {
        // A grid cell named for its skill, recording activations and focus lands. Self-describing: its
        // focus text is just its name (a real skill cell composes name, value, signature, description).
        private sealed class Cell : UIElement
        {
            private readonly string _name;
            private readonly bool _focusable;
            public int Activations;
            public int Focuses;

            public Cell(string name, bool focusable = true)
            {
                _name = name;
                _focusable = focusable;
            }

            public override bool CanFocus => _focusable;
            public override string? Label => _name;
            public override void OnFocused() => Focuses++;

            public override IEnumerable<ElementAction> GetActions()
            {
                yield return new ElementAction(ActionIds.Activate, () => Activations++);
            }
        }

        private readonly List<string> _spoken = new List<string>();
        private TraditionalNavigator NewNav() => new TraditionalNavigator((t, i) => _spoken.Add(t));

        // Panel root > a grid of the given names laid out row by row. A name prefixed with "!" is a
        // non-focusable cell (a skill panel that dropped out), kept in position so the geometry holds.
        private static (Container root, Grid grid, Cell[][] cells) GridOf(params string[][] rows)
        {
            var root = new Container(ContainerShape.Panel);
            var grid = new Grid();
            var cells = new Cell[rows.Length][];
            for (int r = 0; r < rows.Length; r++)
            {
                cells[r] = new Cell[rows[r].Length];
                var rowCells = new UIElement[rows[r].Length];
                for (int c = 0; c < rows[r].Length; c++)
                {
                    bool focusable = !rows[r][c].StartsWith("!");
                    var cell = new Cell(rows[r][c].TrimStart('!'), focusable);
                    cells[r][c] = cell;
                    rowCells[c] = cell;
                }
                grid.AddRow(rowCells);
            }
            root.Add(grid);
            return (root, grid, cells);
        }

        // The 4x6 signature skill grid shape (attribute rows by skill columns), abbreviated.
        private static (Container root, Grid grid, Cell[][] cells) SkillGrid() => GridOf(
            new[] { "Logic", "Encyclopedia", "Rhetoric", "Drama", "Conceptualization", "Visual Calculus" },
            new[] { "Volition", "Inland Empire", "Empathy", "Authority", "Esprit de Corps", "Suggestion" },
            new[] { "Endurance", "Pain Threshold", "Physical Instrument", "Electrochemistry", "Shivers", "Half Light" },
            new[] { "Hand Eye Coordination", "Perception", "Reaction Speed", "Savoir Faire", "Interfacing", "Composure" });

        [Fact]
        public void Attach_LandsOnFirstCell_AnnouncesIt()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);

            Assert.Same(cells[0][0], nav.Current);
            nav.AnnounceCurrent();
            Assert.Equal(new[] { "Logic" }, _spoken);
            Assert.Equal(1, cells[0][0].Focuses);
        }

        [Fact]
        public void Down_MovesRow_KeepsColumn_AnnouncesCell()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);
            nav.Handle(UiActions.Right); // column 1 (Encyclopedia)
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Down));
            Assert.Same(cells[1][1], nav.Current); // Inland Empire, same column
            Assert.Equal(new[] { "Inland Empire" }, _spoken);
            Assert.Equal(1, cells[1][1].Focuses);
        }

        [Fact]
        public void RightAndLeft_MoveColumn_KeepRow()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Right));
            Assert.Same(cells[0][1], nav.Current);
            Assert.Equal(new[] { "Encyclopedia" }, _spoken);

            Assert.True(nav.Handle(UiActions.Left));
            Assert.Same(cells[0][0], nav.Current);
            Assert.Equal("Logic", _spoken[^1]);
        }

        [Fact]
        public void UpAtTop_Consumes_DoesNotMove()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Up)); // grid consumes even at the edge (no wrap)
            Assert.Same(cells[0][0], nav.Current);
            Assert.Empty(_spoken);
        }

        [Fact]
        public void LeftAtFirstColumn_Consumes_DoesNotMove()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Left));
            Assert.Same(cells[0][0], nav.Current);
            Assert.Empty(_spoken);
        }

        [Fact]
        public void Down_SkipsNonFocusableCell_InColumn()
        {
            // Column 0: row 1 dropped out -> Down from row 0 lands on row 2.
            var (root, _, cells) = GridOf(
                new[] { "A0", "A1" },
                new[] { "!B0", "B1" },
                new[] { "C0", "C1" });
            var nav = NewNav();
            nav.Attach(root);
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Down));
            Assert.Same(cells[2][0], nav.Current);
            Assert.Equal(new[] { "C0" }, _spoken);
        }

        [Fact]
        public void HomeAndEnd_JumpWithinRow()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);
            nav.Handle(UiActions.Down); // row 1, column 0 (Volition)
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.End));
            Assert.Same(cells[1][5], nav.Current); // Suggestion, end of the Psyche row
            Assert.Equal("Suggestion", _spoken[^1]);

            Assert.True(nav.Handle(UiActions.Home));
            Assert.Same(cells[1][0], nav.Current); // back to Volition
            Assert.Equal("Volition", _spoken[^1]);
        }

        [Fact]
        public void Activate_FiresFocusedCellAction()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);
            nav.Handle(UiActions.Right);
            nav.Handle(UiActions.Down); // Inland Empire

            Assert.True(nav.Handle(UiActions.Activate));
            Assert.Equal(1, cells[1][1].Activations);
            Assert.Equal(0, cells[0][0].Activations);
        }

        [Fact]
        public void TypeSearch_CollectsCellsRowMajor_MatchesByName()
        {
            var (root, _, cells) = SkillGrid();
            var nav = NewNav();
            nav.Attach(root);

            nav.TypeSearchChar('s');
            nav.TypeSearchChar('h'); // "sh" -> Shivers (row 2, col 4)
            Assert.Same(cells[2][4], nav.Current);
        }

        // Panel root > vertical content of [a small skill grid, a bottom bar]. The bar is a HorizontalList of
        // buttons (a name prefixed "!" is unfocusable, e.g. Begin before a signature is set). Mirrors the
        // signature screen so the vertical flow between the grid and the bar can be exercised.
        private static (Container root, Grid grid, Cell[][] cells, Cell[] bar) GridWithBar(
            string[][] gridRows, params string[] barNames)
        {
            var root = new Container(ContainerShape.Panel);
            var content = new Container(ContainerShape.VerticalList);
            var grid = new Grid();
            var cells = new Cell[gridRows.Length][];
            for (int r = 0; r < gridRows.Length; r++)
            {
                cells[r] = new Cell[gridRows[r].Length];
                var rowCells = new UIElement[gridRows[r].Length];
                for (int c = 0; c < gridRows[r].Length; c++)
                {
                    var cell = new Cell(gridRows[r][c], true);
                    cells[r][c] = cell;
                    rowCells[c] = cell;
                }
                grid.AddRow(rowCells);
            }
            content.Add(grid);

            var barList = new Container(ContainerShape.HorizontalList);
            var bar = new Cell[barNames.Length];
            for (int i = 0; i < barNames.Length; i++)
            {
                bar[i] = new Cell(barNames[i].TrimStart('!'), !barNames[i].StartsWith("!"));
                barList.Add(bar[i]);
            }
            content.Add(barList);
            root.Add(content);
            return (root, grid, cells, bar);
        }

        private static readonly string[][] TwoRowGrid =
        {
            new[] { "A0", "A1", "A2" },
            new[] { "B0", "B1", "B2" },
        };

        // Down from the grid's bottom row spills into the bottom bar (one vertical flow), and Up returns to
        // the grid on the cell it was left from (the grid's remembered child).
        [Fact]
        public void Down_FromGridBottom_SpillsIntoBar_UpReturnsToRememberedCell()
        {
            var (root, _, cells, bar) = GridWithBar(TwoRowGrid, "Confirm");
            var nav = NewNav();
            nav.Attach(root);
            nav.Handle(UiActions.Right); // grid col 1 (A1)
            nav.Handle(UiActions.Down);  // grid bottom row, col 1 (B1)
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Down)); // off the grid bottom -> spill into the bar
            Assert.Same(bar[0], nav.Current);
            Assert.Equal(new[] { "Confirm" }, _spoken);

            Assert.True(nav.Handle(UiActions.Up)); // back up into the grid, on the cell we left
            Assert.Same(cells[1][1], nav.Current);
        }

        // Left/Right move between the bar's buttons once focus has spilled into it.
        [Fact]
        public void InBar_LeftRight_MoveBetweenButtons()
        {
            var (root, _, _, bar) = GridWithBar(TwoRowGrid, "Confirm", "Back");
            var nav = NewNav();
            nav.Attach(root);
            nav.Handle(UiActions.Down); // grid bottom row (B0)
            nav.Handle(UiActions.Down); // spill into the bar (Confirm)
            Assert.Same(bar[0], nav.Current);
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Right));
            Assert.Same(bar[1], nav.Current);
            Assert.Equal(new[] { "Back" }, _spoken);

            Assert.True(nav.Handle(UiActions.Left));
            Assert.Same(bar[0], nav.Current);
        }

        // The skill portraits are instantiated a frame after the view transition, so the grid can be built
        // empty: focus must not strand on the empty grid container, and once it is populated EnsureFocusValid
        // re-homes onto the first cell (the rich screen's OnUpdate path).
        [Fact]
        public void EmptyGrid_NotLanded_ThenPopulate_ReHomesToFirstCell()
        {
            var root = new Container(ContainerShape.Panel);
            var grid = new Grid();
            root.Add(grid);
            var nav = NewNav();
            nav.Attach(root);
            Assert.Null(nav.Current); // an empty grid is not a landing target

            var alpha = new Cell("Alpha");
            grid.AddRow(alpha, new Cell("Bravo"));
            _spoken.Clear();

            Assert.True(nav.EnsureFocusValid()); // populated -> re-home
            Assert.Same(alpha, nav.Current);
            nav.AnnounceCurrent();
            Assert.Equal("Alpha", _spoken[^1]);
        }

        // The Begin button is unfocusable until a signature is set, so the bar is an empty container: Down
        // from the grid's bottom row skips it and stays in the grid (consumes, no spill).
        [Fact]
        public void Down_FromGridBottom_SkipsEmptyBar_StaysInGrid()
        {
            var (root, _, cells, _) = GridWithBar(TwoRowGrid, "!Begin"); // bar button inactive
            var nav = NewNav();
            nav.Attach(root);
            nav.Handle(UiActions.Down); // grid bottom row (B0)
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Down)); // empty bar skipped -> consume, stay
            Assert.Same(cells[1][0], nav.Current);
            Assert.Empty(_spoken);
        }
    }
}
