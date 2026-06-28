namespace DiscoAccess.Core.UI.Nav
{
    /// <summary>A focus-move direction (arrow keys).</summary>
    public enum NavDirection { Up, Down, Left, Right }

    /// <summary>
    /// Container shape - how a navigator traverses it.
    /// VerticalList/HorizontalList: arrows move among items; the whole container is one Tab-stop.
    /// Grid: a 2-D cell cursor (Up/Down change row, Left/Right change column); the whole grid is one
    /// Tab-stop, and the navigator announces only the axis that changed (the column header on a column
    /// move, the row text on a row move).
    /// Panel: Tab/Shift-Tab traverse its focusable descendants (WinForms-style); arrows do nothing.
    /// Tree exists in the reference design and will be added when a screen needs it.
    /// </summary>
    public enum ContainerShape { VerticalList, HorizontalList, Grid, Panel }
}
