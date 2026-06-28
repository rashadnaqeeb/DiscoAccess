using System;
using System.Collections.Generic;

namespace DiscoAccess.Core.UI.Nav
{
    /// <summary>
    /// Windows-screen-reader-style navigation: Tab / Shift-Tab traverse Panel tab-stops (a list counts as
    /// one stop), arrows move within a list (or adjust a focused slider/stepper), Enter activates, Escape
    /// asks the screen to go back, Home/End jump to a list's ends. Entering a container auto-focuses its
    /// representative child.
    /// </summary>
    public sealed class TraditionalNavigator : Navigator
    {
        public TraditionalNavigator(Action<string, bool> speak) : base(speak) { }

        protected override void BuildInitialFocus()
        {
            if (Root == null) return;
            var first = RepresentativeChild(Root);
            if (first == null) return;
            Root.SetFocusedChild(first);
            AppendWithDescend(first);
        }

        public override bool Handle(string actionKey)
        {
            switch (actionKey)
            {
                case UiActions.Up: return Arrow(NavDirection.Up);
                case UiActions.Down: return Arrow(NavDirection.Down);
                case UiActions.Left: return Arrow(NavDirection.Left);
                case UiActions.Right: return Arrow(NavDirection.Right);
                case UiActions.Next: return Tab(1);
                case UiActions.Prev: return Tab(-1);
                case UiActions.Home: return JumpEdge(first: true);
                case UiActions.End: return JumpEdge(first: false);
                case UiActions.Activate:
                {
                    if (Current == null) return false;
                    // Consume only when something actually activated; a focused element with no Activate
                    // action leaves the key unconsumed rather than silently eating it.
                    bool activated = Current.InvokeAction(ActionIds.Activate);
                    if (activated && Current.ReannounceOnActivate)
                        Speak(Current.GetValueText(), interrupt: true);
                    return activated;
                }
                case UiActions.Back:
                    // Screen-level back/close: consume only if the root advertises a back action.
                    return Root != null && Root.InvokeAction(ActionIds.Back);
                default:
                    return false; // not a nav key
            }
        }

        private bool Arrow(NavDirection dir)
        {
            if (Current == null) return false;

            // A focused slider/stepper advertises increase/decrease; Left/Right adjust it and re-announce
            // just the new value.
            if (dir == NavDirection.Left && Current.InvokeAction(ActionIds.Decrease))
            { Speak(Current.GetValueText(), interrupt: true); return true; }
            if (dir == NavDirection.Right && Current.InvokeAction(ActionIds.Increase))
            { Speak(Current.GetValueText(), interrupt: true); return true; }

            // A grid moves a 2-D cell cursor and announces only the axis that changed.
            if (Current.Parent is Grid grid) return GridArrow(dir, grid);

            var snapshot = new List<UIElement>(Path);
            if (!Move(dir)) return false;
            AnnounceDelta(snapshot, interrupt: true);
            return true;
        }

        // Move the cell cursor in a grid and announce Excel-style: the column header when the column
        // changed, the row text when the row changed, then the cell's own value if any. A non-focusable
        // cell (e.g. a non-deletable save's Delete) is scanned past in the move direction; running off the
        // grid consumes the key without wrapping.
        private bool GridArrow(NavDirection dir, Grid grid)
        {
            if (!grid.TryCoords(Current!, out int r, out int c)) return false;
            int nr = r, nc = c;
            UIElement? next = null;
            while (true)
            {
                switch (dir)
                {
                    case NavDirection.Down: nr++; break;
                    case NavDirection.Up: nr--; break;
                    case NavDirection.Right: nc++; break;
                    case NavDirection.Left: nc--; break;
                }
                var cell = grid.CellAt(nr, nc);
                if (cell == null) return true; // off the edge: consume, no wrap
                if (cell.CanFocus) { next = cell; break; }
            }

            LandOnCell(next, announceColumn: nc != c, announceRow: nr != r);
            return true;
        }

        // Move focus onto a grid cell and announce Excel-style: only the axis text the caller says changed
        // (the column header on a column move, the row text on a row move), falling back to the cell's full
        // focus text if neither axis has anything to say. Then sync the platform cursor. Shared by the grid
        // arrow and Home/End jumps so they land and read consistently.
        private void LandOnCell(UIElement cell, bool announceColumn, bool announceRow)
        {
            BuildPathTo(cell);
            var parts = new List<string>(2);
            if (cell is GridCell gc)
            {
                if (announceColumn && !string.IsNullOrEmpty(gc.ColumnHeader)) parts.Add(gc.ColumnHeader!);
                if (announceRow && !string.IsNullOrEmpty(gc.RowText)) parts.Add(gc.RowText!);
            }
            if (parts.Count == 0)
            {
                var text = cell.GetFocusText();
                if (!string.IsNullOrEmpty(text)) parts.Add(text);
            }
            if (parts.Count > 0) Speak(string.Join(", ", parts), interrupt: true);
            cell.OnFocused();
        }

        // Arrow movement within list-shaped containers, spilling into a same-shape parent at the edge.
        private bool Move(NavDirection dir)
        {
            var movingFrom = Current;
            var container = movingFrom?.Parent;
            while (container != null && movingFrom != null)
            {
                var next = container.GetNeighbor(movingFrom, dir);
                if (next != null)
                {
                    int idx = Path.IndexOf(movingFrom);
                    if (idx >= 0) Path.RemoveRange(idx, Path.Count - idx);
                    AppendWithDescend(next);
                    container.SetFocusedChild(next);
                    return true;
                }
                var parent = container.Parent;
                if (parent != null && parent.Shape == container.Shape)
                {
                    movingFrom = container;
                    container = parent;
                    continue;
                }
                return false;
            }
            return false;
        }

        private bool Tab(int step)
        {
            var stops = ComputeTabStops();
            if (stops.Count == 0) return false;

            // Current may be deeper than its tab-stop (an item inside a list whose stop is the list's
            // representative), so walk up to the nearest element that IS a stop.
            int idx = -1;
            for (var e = Current; e != null && idx < 0; e = e.Parent)
                idx = stops.IndexOf(e);

            int ni = idx < 0 ? (step >= 0 ? 0 : stops.Count - 1) : idx + step;
            if (ni < 0 || ni >= stops.Count) return true; // at an end; consume, no wrap

            var snapshot = new List<UIElement>(Path);
            BuildPathTo(stops[ni]);
            // Re-descend so re-entering a list restores its remembered/representative item.
            var landed = Current;
            if (landed != null) { Path.RemoveAt(Path.Count - 1); AppendWithDescend(landed); }
            AnnounceDelta(snapshot, interrupt: true);
            return true;
        }

        // Home/End: jump to the first/last item of the list the focus is in.
        private bool JumpEdge(bool first)
        {
            var container = Current?.Parent;

            // In a grid, jump to the first/last focusable cell of the current column (only the row
            // changes, so just the row text is announced).
            if (container is Grid grid)
            {
                if (!grid.TryCoords(Current!, out _, out int col)) return true;
                int step = first ? 1 : -1;
                int start = first ? 0 : grid.RowCount - 1;
                UIElement? edge = null;
                for (int rr = start; rr >= 0 && rr < grid.RowCount; rr += step)
                {
                    var cell = grid.CellAt(rr, col);
                    if (cell != null && cell.CanFocus) { edge = cell; break; }
                }
                if (edge == null || edge == Current) return true;
                // A Home/End jump stays in the column, so only the row changed: announce just the row text.
                LandOnCell(edge, announceColumn: false, announceRow: true);
                return true;
            }

            if (container == null
                || (container.Shape != ContainerShape.VerticalList && container.Shape != ContainerShape.HorizontalList))
                return true; // focused but in no jumpable list - consume, do nothing

            var target = first ? container.FirstFocusable() : container.LastFocusable();
            if (target == null || target == Current) return true;

            var snapshot = new List<UIElement>(Path);
            int idx = Path.IndexOf(Current!);
            if (idx >= 0) Path.RemoveRange(idx, Path.Count - idx);
            AppendWithDescend(target);
            container.SetFocusedChild(target);
            AnnounceDelta(snapshot, interrupt: true);
            return true;
        }
    }
}
