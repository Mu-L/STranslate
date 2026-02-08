using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Input;

namespace STranslate.Helpers;

public static class TextContextMenuFix
{
    private static bool _installed;

    public static void Install()
    {
        if (_installed) return;
        _installed = true;

        EventManager.RegisterClassHandler(
            typeof(TextBoxBase),
            FrameworkElement.ContextMenuOpeningEvent,
            new ContextMenuEventHandler(OnContextMenuOpening),
            true);

        EventManager.RegisterClassHandler(
            typeof(PasswordBox),
            FrameworkElement.ContextMenuOpeningEvent,
            new ContextMenuEventHandler(OnContextMenuOpening),
            true);
    }

    private static void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;

        var desiredMenu = ResolveDefaultTextContextMenu();
        if (desiredMenu is null) return;

        if (fe.ContextMenu is ContextMenu existingMenu &&
            !ReferenceEquals(existingMenu, desiredMenu) &&
            !LooksLikeTextContextMenu(existingMenu))
            return;

        if (!ReferenceEquals(fe.ContextMenu, desiredMenu))
            fe.ContextMenu = desiredMenu;

        EnsureSeparatorCleanupHandler(desiredMenu);
    }

    private static ContextMenu? ResolveDefaultTextContextMenu()
    {
        if (Application.Current is null) return null;
        return Application.Current.TryFindResource("DefaultTextBoxContextMenu") as ContextMenu;
    }

    private static void EnsureSeparatorCleanupHandler(ContextMenu menu)
    {
        if (!LooksLikeTextContextMenu(menu)) return;
        menu.Opened -= OnTextContextMenuOpened;
        menu.Opened += OnTextContextMenuOpened;
    }

    private static bool LooksLikeTextContextMenu(ContextMenu menu)
    {
        var hasSelectAll = false;
        var hasCopyLike = false;

        foreach (var item in menu.Items)
        {
            if (item is not MenuItem mi) continue;

            if (ReferenceEquals(mi.Command, ApplicationCommands.SelectAll))
                hasSelectAll = true;

            if (ReferenceEquals(mi.Command, ApplicationCommands.Cut) ||
                ReferenceEquals(mi.Command, ApplicationCommands.Copy) ||
                ReferenceEquals(mi.Command, ApplicationCommands.Paste))
                hasCopyLike = true;
        }

        return hasSelectAll && hasCopyLike;
    }

    private static void OnTextContextMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu) return;
        menu.Dispatcher.BeginInvoke(
            () => CleanupRedundantSeparators(menu),
            DispatcherPriority.Render);
        menu.Dispatcher.BeginInvoke(
            () => CleanupRedundantSeparators(menu),
            DispatcherPriority.ApplicationIdle);
    }

    private static void CleanupRedundantSeparators(ContextMenu menu)
    {
        for (var i = 0; i < menu.Items.Count; i++)
        {
            if (menu.Items[i] is not Separator sep) continue;

            var prev = FindVisibleNonSeparator(menu, i, -1);
            var next = FindVisibleNonSeparator(menu, i, 1);

            sep.Visibility = (prev is null || next is null) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private static FrameworkElement? FindVisibleNonSeparator(ContextMenu menu, int startIndex, int step)
    {
        for (var i = startIndex + step; i >= 0 && i < menu.Items.Count; i += step)
        {
            var item = menu.Items[i];
            if (item is Separator) continue;
            if (item is not FrameworkElement fe) continue;
            if (fe.Visibility != Visibility.Visible) continue;
            return fe;
        }

        return null;
    }
}
