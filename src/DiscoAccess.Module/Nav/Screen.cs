using DiscoAccess.Core.Modularity;
using DiscoAccess.Core.UI.Nav;
using Sunshine.Views;

namespace DiscoAccess.Module.Nav
{
    /// <summary>
    /// Base for a navigable screen: it matches a game <see cref="ViewType"/>, carries an authored name
    /// spoken on entry, and builds its element tree fresh each time it is entered (read live, never
    /// cached). The ScreenManager resolves the active screen from <c>ViewsPagesBridge.Current</c> and
    /// attaches the navigator to the built root.
    /// </summary>
    public abstract class Screen
    {
        /// <summary>The game view this screen reads.</summary>
        public abstract ViewType ViewType { get; }

        /// <summary>Authored screen name spoken when the screen is entered.</summary>
        public abstract string ScreenName { get; }

        /// <summary>Build the navigable tree from live game state. Called on each entry.</summary>
        public abstract Container BuildRoot(IModHost host);

        /// <summary>Called every frame while this screen stands (the view is unchanged). A rich screen
        /// overrides it to refresh dynamic content in place - rebuilding a sub-tree when the game state it
        /// mirrors changes (e.g. an options tab switch) and re-homing the navigator. The default does
        /// nothing, so a static screen needs no per-frame work.</summary>
        public virtual void OnUpdate(IModHost host, TraditionalNavigator nav) { }
    }
}
