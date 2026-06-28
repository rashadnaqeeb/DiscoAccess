using System.Collections.Generic;
using DiscoAccess.Core.UI.Nav;
using Xunit;

namespace DiscoAccess.Tests
{
    public class NavigatorTests
    {
        // A focusable leaf with a label, a "button" role, and a recorded activation.
        private sealed class Button : UIElement
        {
            private readonly string _label;
            public int Activations;
            public Button(string label) { _label = label; }
            public override string Label => _label;
            public override string Role => "button";
            public override IEnumerable<ElementAction> GetActions()
            {
                yield return new ElementAction(ActionIds.Activate, () => Activations++);
            }
        }

        private readonly List<string> _spoken = new List<string>();
        private TraditionalNavigator NewNav() => new TraditionalNavigator((t, i) => _spoken.Add(t));

        // Panel root > labeled vertical list "main menu" of [Continue, New Game, Load Game].
        private static (Container root, Container list, Button[] items) MainMenuTree()
        {
            var root = new Container(ContainerShape.Panel);
            var list = new Container(ContainerShape.VerticalList, "main menu");
            var items = new[] { new Button("Continue"), new Button("New Game"), new Button("Load Game") };
            foreach (var b in items) list.Add(b);
            root.Add(list);
            return (root, list, items);
        }

        [Fact]
        public void Attach_LandsOnFirstItem_AnnounceReadsPathThenLeaf()
        {
            var (root, _, items) = MainMenuTree();
            var nav = NewNav();
            nav.Attach(root);
            Assert.Same(items[0], nav.Current);

            nav.AnnounceCurrent();
            Assert.Equal(new[] { "main menu, list, Continue, button" }, _spoken);
        }

        [Fact]
        public void Down_MovesToNextItem_AnnouncesOnlyTheLeaf()
        {
            var (root, _, items) = MainMenuTree();
            var nav = NewNav();
            nav.Attach(root);

            Assert.True(nav.Handle(UiActions.Down));
            Assert.Same(items[1], nav.Current);
            Assert.Equal(new[] { "New Game, button" }, _spoken);
        }

        [Fact]
        public void Up_AtTop_DoesNotMove_AndIsNotConsumed()
        {
            var (root, _, items) = MainMenuTree();
            var nav = NewNav();
            nav.Attach(root);

            Assert.False(nav.Handle(UiActions.Up)); // nothing above the first item
            Assert.Same(items[0], nav.Current);
            Assert.Empty(_spoken);
        }

        [Fact]
        public void End_JumpsToLast_Home_JumpsToFirst()
        {
            var (root, _, items) = MainMenuTree();
            var nav = NewNav();
            nav.Attach(root);

            Assert.True(nav.Handle(UiActions.End));
            Assert.Same(items[2], nav.Current);
            Assert.Equal("Load Game, button", _spoken[^1]);

            Assert.True(nav.Handle(UiActions.Home));
            Assert.Same(items[0], nav.Current);
            Assert.Equal("Continue, button", _spoken[^1]);
        }

        [Fact]
        public void Activate_FiresFocusedLeafAction()
        {
            var (root, _, items) = MainMenuTree();
            var nav = NewNav();
            nav.Attach(root);

            Assert.True(nav.Handle(UiActions.Activate));
            Assert.Equal(1, items[0].Activations);
            Assert.Equal(0, items[1].Activations);
        }

        [Fact]
        public void Tab_MovesBetweenLists_InAPanel()
        {
            var root = new Container(ContainerShape.Panel);
            var menu = new Container(ContainerShape.VerticalList, "main menu");
            menu.Add(new Button("Continue"));
            var side = new Container(ContainerShape.VerticalList, "details");
            var detail = new Button("Version");
            side.Add(detail);
            root.Add(menu);
            root.Add(side);

            var nav = NewNav();
            nav.Attach(root);
            _spoken.Clear();

            Assert.True(nav.Handle(UiActions.Next));
            Assert.Same(detail, nav.Current);
            Assert.Equal(new[] { "details, list, Version, button" }, _spoken);

            Assert.True(nav.Handle(UiActions.Prev));
            Assert.Equal("main menu, list, Continue, button", _spoken[^1]);
        }

        [Fact]
        public void Back_ConsumedOnlyWhenRootAdvertisesBack()
        {
            var (root, _, _) = MainMenuTree();
            var nav = NewNav();
            nav.Attach(root);
            Assert.False(nav.Handle(UiActions.Back)); // plain root has no back action

            int backs = 0;
            var withBack = new BackContainer(() => backs++);
            var list = new Container(ContainerShape.VerticalList, "settings");
            list.Add(new Button("Volume"));
            withBack.Add(list);
            var nav2 = NewNav();
            nav2.Attach(withBack);
            Assert.True(nav2.Handle(UiActions.Back));
            Assert.Equal(1, backs);
        }

        [Fact]
        public void EnsureFocusValid_NoOp_WhenFocusStillReachable()
        {
            var (root, _, items) = MainMenuTree();
            var nav = NewNav();
            nav.Attach(root);

            Assert.False(nav.EnsureFocusValid()); // focus intact, nothing to do
            Assert.Same(items[0], nav.Current);
        }

        [Fact]
        public void EnsureFocusValid_Rehomes_WhenFocusOrphanedByRebuild()
        {
            var root = new Container(ContainerShape.Panel);
            var list = new Container(ContainerShape.VerticalList, "settings");
            list.Add(new Button("A"));
            list.Add(new Button("B"));
            root.Add(list);
            var nav = NewNav();
            nav.Attach(root);
            Assert.True(nav.Handle(UiActions.Down)); // focus the second item
            Assert.Equal("B", nav.Current!.Label);

            // A dynamic rebuild clears the list and refills it, orphaning the focused element.
            list.Clear();
            list.Add(new Button("C"));

            Assert.True(nav.EnsureFocusValid()); // orphaned -> re-homed
            Assert.Equal("C", nav.Current!.Label); // re-landed on the rebuilt list's first focusable
        }

        [Fact]
        public void EnsureFocusValid_Rehomes_WhenAncestorReplacedButImmediateParentIntact()
        {
            // The options-screen rebuild shape: a content Panel holds an inner list which holds the controls.
            // A tab switch replaces the WHOLE inner list (clear the panel, add a fresh list), so the focused
            // control's immediate parent (the old list) still lists it, but that old list is no longer under
            // the panel. An immediate-parent-only orphan check misses this; the full-chain walk catches it.
            var root = new Container(ContainerShape.Panel);
            var content = new Container(ContainerShape.Panel);
            var oldList = new Container(ContainerShape.VerticalList, "settings");
            oldList.Add(new Button("A"));
            oldList.Add(new Button("B"));
            content.Add(oldList);
            root.Add(content);
            var nav = NewNav();
            nav.Attach(root);
            Assert.True(nav.Handle(UiActions.Down));
            Assert.Equal("B", nav.Current!.Label);

            // Tab switch: the content panel drops the old list (whose children keep their back-pointers) and
            // gets a brand-new list. oldList still .Contains the focused "B", but content no longer .Contains oldList.
            content.Clear();
            var newList = new Container(ContainerShape.VerticalList, "controls");
            newList.Add(new Button("C"));
            content.Add(newList);

            Assert.True(nav.EnsureFocusValid()); // ancestor link broken -> orphaned -> re-homed
            Assert.Equal("C", nav.Current!.Label);
        }

        // A screen root that advertises a back action (Escape closes it).
        private sealed class BackContainer : Container
        {
            private readonly System.Action _back;
            public BackContainer(System.Action back) : base(ContainerShape.Panel) { _back = back; }
            public override IEnumerable<ElementAction> GetActions()
            {
                yield return new ElementAction(ActionIds.Back, _back);
            }
        }
    }
}
