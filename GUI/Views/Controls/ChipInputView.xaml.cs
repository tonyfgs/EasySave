using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MauiApp = Microsoft.Maui.Controls.Application;

namespace GUI.Views.Controls;

public partial class ChipInputView : ContentView
{
    private bool _suppressUnfocus;

    #region BindableProperties

    public static readonly BindableProperty AvailableItemsProperty =
        BindableProperty.Create(
            nameof(AvailableItems),
            typeof(IList<string>),
            typeof(ChipInputView),
            defaultValue: null,
            propertyChanged: OnAvailableItemsChanged);

    public static readonly BindableProperty SelectedItemsProperty =
        BindableProperty.Create(
            nameof(SelectedItems),
            typeof(ObservableCollection<string>),
            typeof(ChipInputView),
            defaultValue: null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: OnSelectedItemsChanged);

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(
            nameof(Placeholder),
            typeof(string),
            typeof(ChipInputView),
            defaultValue: "Search...",
            propertyChanged: OnPlaceholderChanged);

    public static readonly BindableProperty AllowCustomItemsProperty =
        BindableProperty.Create(
            nameof(AllowCustomItems),
            typeof(bool),
            typeof(ChipInputView),
            defaultValue: true);

    public static readonly BindableProperty MaxItemsProperty =
        BindableProperty.Create(
            nameof(MaxItems),
            typeof(int),
            typeof(ChipInputView),
            defaultValue: 0);

    public IList<string>? AvailableItems
    {
        get => (IList<string>?)GetValue(AvailableItemsProperty);
        set => SetValue(AvailableItemsProperty, value);
    }

    public ObservableCollection<string>? SelectedItems
    {
        get => (ObservableCollection<string>?)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool AllowCustomItems
    {
        get => (bool)GetValue(AllowCustomItemsProperty);
        set => SetValue(AllowCustomItemsProperty, value);
    }

    public int MaxItems
    {
        get => (int)GetValue(MaxItemsProperty);
        set => SetValue(MaxItemsProperty, value);
    }

    #endregion

    public ChipInputView()
    {
        InitializeComponent();
    }

    #region Property Changed Callbacks

    private static void OnAvailableItemsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ChipInputView view)
            view.UpdateDropdownItems();
    }

    private static void OnSelectedItemsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not ChipInputView view) return;

        if (oldValue is ObservableCollection<string> oldCollection)
            oldCollection.CollectionChanged -= view.OnSelectedItemsCollectionChanged;

        if (newValue is ObservableCollection<string> newCollection)
            newCollection.CollectionChanged += view.OnSelectedItemsCollectionChanged;

        view.RebuildChips();
        view.UpdateEntryVisibility();
        view.UpdateClearButtonVisibility();
    }

    private static void OnPlaceholderChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ChipInputView view && newValue is string placeholder)
            view.SearchEntry.Placeholder = placeholder;
    }

    #endregion

    #region Collection Changed

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildChips();
        UpdateEntryVisibility();
        UpdateClearButtonVisibility();
        UpdateDropdownItems();
    }

    #endregion

    #region Chip Management

    private void RebuildChips()
    {
        // Remove all children except the Entry
        var children = ChipsContainer.Children.ToList();
        foreach (var child in children)
        {
            if (child != SearchEntry)
                ChipsContainer.Children.Remove(child);
        }

        // Re-add chips before the Entry
        if (SelectedItems == null) return;

        var entryIndex = ChipsContainer.Children.IndexOf(SearchEntry);
        var insertIndex = 0;

        foreach (var item in SelectedItems)
        {
            var chip = CreateChipView(item);
            ChipsContainer.Children.Insert(insertIndex, chip);
            insertIndex++;
        }
    }

    private View CreateChipView(string item)
    {
        var label = new Label
        {
            Text = item,
            TextColor = Colors.White,
            FontSize = 13,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0)
        };

        var removeButton = new Label
        {
            Text = "\u2715",
            TextColor = Colors.White,
            FontSize = 12,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(6, 0, 0, 0),
            Opacity = 0.8
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (_, _) => OnChipRemoved(item);
        removeButton.GestureRecognizers.Add(tapGesture);

        var stack = new HorizontalStackLayout
        {
            Spacing = 0,
            Children = { label, removeButton }
        };

        var isDarkTheme = MauiApp.Current?.RequestedTheme == AppTheme.Dark;

        var chip = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            BackgroundColor = Color.FromArgb(isDarkTheme ? "#3B82F6" : "#1E293B"),
            Stroke = Colors.Transparent,
            Padding = new Thickness(10, 5),
            Margin = new Thickness(0, 3, 6, 3),
            Content = stack
        };

        return chip;
    }

    private void OnChipRemoved(string item)
    {
        SelectedItems?.Remove(item);
    }

    #endregion

    #region Entry Events

    private void OnSearchEntryCompleted(object? sender, EventArgs e)
    {
        var text = SearchEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        if (!AllowCustomItems)
        {
            // Only allow items from the available list
            if (AvailableItems == null || !AvailableItems.Contains(text))
                return;
        }

        AddItem(text);
        SearchEntry.Text = string.Empty;
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateDropdownItems();

        if (!string.IsNullOrEmpty(e.NewTextValue))
            ShowDropdown();
        else if (!SearchEntry.IsFocused)
            HideDropdown();
    }

    private void OnSearchEntryFocused(object? sender, FocusEventArgs e)
    {
        UpdateDropdownItems();
        ShowDropdown();
    }

    private void OnSearchEntryUnfocused(object? sender, FocusEventArgs e)
    {
        // Delay hiding to allow tap on dropdown items to register
        if (!_suppressUnfocus)
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () =>
            {
                if (!SearchEntry.IsFocused)
                    HideDropdown();
            });
        }
        _suppressUnfocus = false;
    }

    #endregion

    #region Dropdown Management

    private void UpdateDropdownItems()
    {
        SuggestionsList.Children.Clear();

        var available = AvailableItems;
        var selected = SelectedItems;
        var searchText = SearchEntry.Text?.Trim() ?? string.Empty;

        if (available == null) return;

        var filtered = available
            .Where(item => selected == null || !selected.Contains(item))
            .Where(item => string.IsNullOrEmpty(searchText) ||
                           item.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filtered.Count == 0)
        {
            HideDropdown();
            return;
        }

        var isDarkTheme = MauiApp.Current?.RequestedTheme == AppTheme.Dark;

        foreach (var item in filtered)
        {
            var label = new Label
            {
                Text = item,
                FontSize = 14,
                Padding = new Thickness(12, 10),
                TextColor = Color.FromArgb(isDarkTheme ? "#FFFFFF" : "#212121")
            };

            var container = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 4 },
                Stroke = Colors.Transparent,
                BackgroundColor = Colors.Transparent,
                Content = label
            };

            // Hover-like effect on tap
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (_, _) =>
            {
                _suppressUnfocus = true;
                AddItem(item);
                SearchEntry.Text = string.Empty;
                SearchEntry.Focus();
            };
            container.GestureRecognizers.Add(tapGesture);

            // Pointer over visual feedback
            var pointerGesture = new PointerGestureRecognizer();
            pointerGesture.PointerEntered += (_, _) =>
                container.BackgroundColor = Color.FromArgb(isDarkTheme ? "#2A2A2A" : "#F0F0F0");
            pointerGesture.PointerExited += (_, _) =>
                container.BackgroundColor = Colors.Transparent;
            container.GestureRecognizers.Add(pointerGesture);

            SuggestionsList.Children.Add(container);
        }

        if (SearchEntry.IsFocused || !string.IsNullOrEmpty(searchText))
            ShowDropdown();
    }

    private void ShowDropdown()
    {
        if (SuggestionsList.Children.Count == 0) return;

        // Don't show dropdown if max items reached
        if (MaxItems > 0 && SelectedItems != null && SelectedItems.Count >= MaxItems)
            return;

        DropdownBorder.IsVisible = true;
    }

    private void HideDropdown()
    {
        DropdownBorder.IsVisible = false;
    }

    #endregion

    #region Item Management

    private void AddItem(string item)
    {
        if (SelectedItems == null) return;
        if (SelectedItems.Contains(item)) return;

        // Enforce MaxItems
        if (MaxItems > 0 && SelectedItems.Count >= MaxItems)
            return;

        SelectedItems.Add(item);
    }

    private void OnClearAllClicked(object? sender, EventArgs e)
    {
        SelectedItems?.Clear();
        SearchEntry.Text = string.Empty;
    }

    #endregion

    #region Visibility Helpers

    private void UpdateEntryVisibility()
    {
        var maxReached = MaxItems > 0 && SelectedItems != null && SelectedItems.Count >= MaxItems;
        SearchEntry.IsVisible = !maxReached;

        if (maxReached)
            HideDropdown();
    }

    private void UpdateClearButtonVisibility()
    {
        ClearAllButton.IsVisible = SelectedItems != null && SelectedItems.Count > 0;
    }

    #endregion
}
