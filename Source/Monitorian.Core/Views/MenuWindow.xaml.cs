using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Monitorian.Core.Models;
using Monitorian.Core.ViewModels;
using Monitorian.Core.Views.Controls;
using ScreenFrame;
using ScreenFrame.Movers;

namespace Monitorian.Core.Views;

public partial class MenuWindow : Window
{
	private readonly FloatWindowMover _mover;
	public MenuWindowViewModel ViewModel => (MenuWindowViewModel)this.DataContext;

	private enum HotkeyTarget
	{
		None,
		BrightnessDown,
		BrightnessUp
	}

	private HotkeyTarget _capturingTarget = HotkeyTarget.None;
	private bool _isUpdatingDropdowns = false;

	public MenuWindow(AppControllerCore controller, Point pivot)
	{
		LanguageService.Switch();

		InitializeComponent();

		this.DataContext = new MenuWindowViewModel(controller);

		_mover = new FloatWindowMover(this, pivot);
		_mover.ForegroundWindowChanged += OnDeactivated;
		_mover.AppDeactivated += OnDeactivated;

		controller.WindowPainter.Add(this);

		InitializeDropdownSources();
		InitializeDropdownSelectionsFromSettings();

		// Keep dropdowns in sync when settings change elsewhere
		ViewModel.Settings.PropertyChanged += SettingsOnPropertyChanged;
	}

	public UIElementCollection HeadSection => this.HeadItems.Children;
	public UIElementCollection MenuSectionTop => this.MenuItemsTop.Children;
	public UIElementCollection MenuSectionMiddle => this.MenuItemsMiddle.Children;

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		EnsureFlowDirection(this);
	}

	public static void EnsureFlowDirection(ContentControl rootControl)
	{
		if (!LanguageService.IsResourceRightToLeft)
			return;

		var resourceValues = new HashSet<string>(LanguageService.ResourceDictionary.Values);

		foreach (var itemControl in LogicalTreeHelperAddition.EnumerateDescendants<ContentControl>(rootControl)
			.Select(x => x.Content as ButtonBase)
			.Where(x => x is not null))
		{
			TemplateElement.SetVisibility(itemControl, Visibility.Visible);

			if (resourceValues.Contains(itemControl.Content))
				itemControl.FlowDirection = FlowDirection.RightToLeft;
		}
	}

	#region Show/Close

	public void DepartFromForeground()
	{
		this.Topmost = false;
	}

	public async void ReturnToForeground()
	{
		// Wait for this window to be able to be activated.
		await Task.Delay(TimeSpan.FromMilliseconds(100));

		if (_isClosing)
			return;

		// Activate this window. This is necessary to assure this window is foreground.
		this.Activate();

		this.Topmost = true;
	}

	private bool _isClosing = false;

	private void OnDeactivated(object sender, EventArgs e)
	{
		if (!_isClosing && this.IsLoaded)
			this.Close();
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);

		if (!this.Topmost)
			return;

		if (!_isClosing)
			this.Close();
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		if (!e.Cancel)
		{
			_isClosing = true;
			// Unsubscribe to avoid leaks
			if (ViewModel?.Settings is SettingsCore s)
				s.PropertyChanged -= SettingsOnPropertyChanged;
			ViewModel.Dispose();
		}

		base.OnClosing(e);
	}

	#endregion

	#region Hotkey capture

	private void OnChangeBrightnessDownHotkey(object sender, RoutedEventArgs e)
	{
		StartCapture(HotkeyTarget.BrightnessDown);
	}

	private void OnChangeBrightnessUpHotkey(object sender, RoutedEventArgs e)
	{
		StartCapture(HotkeyTarget.BrightnessUp);
	}

	private void StartCapture(HotkeyTarget target)
	{
		_capturingTarget = target;
		this.PreviewKeyDown -= OnPreviewKeyDownCapture;
		this.PreviewKeyDown += OnPreviewKeyDownCapture;
		// Bring window to foreground to ensure key routing
		this.Activate();
	}

	private void StopCapture()
	{
		this.PreviewKeyDown -= OnPreviewKeyDownCapture;
		_capturingTarget = HotkeyTarget.None;
	}

	private void OnPreviewKeyDownCapture(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (_capturingTarget == HotkeyTarget.None)
			return;

		// Allow Esc to cancel capture
		var keyForCheck = (e.Key == System.Windows.Input.Key.System) ? e.SystemKey : e.Key;
		if (keyForCheck == System.Windows.Input.Key.Escape)
		{
			StopCapture();
			e.Handled = true;
			return;
		}

		var mods = System.Windows.Input.Keyboard.Modifiers;
		var shortcutText = BuildHotkeyString(mods, keyForCheck);
		if (string.IsNullOrEmpty(shortcutText))
		{
			// No non-modifier key pressed, keep capturing
			return;
		}

		// Update settings
		switch (_capturingTarget)
		{
			case HotkeyTarget.BrightnessDown:
				ViewModel.Settings.BrightnessDecreaseHotkey = shortcutText;
				break;
			case HotkeyTarget.BrightnessUp:
				ViewModel.Settings.BrightnessIncreaseHotkey = shortcutText;
				break;
		}

		StopCapture();
		e.Handled = true;
	}

	private static string BuildHotkeyString(System.Windows.Input.ModifierKeys mods, System.Windows.Input.Key key)
	{
		// Ignore pure modifier keys
		if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
			key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
			key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
			key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
		{
			return null;
		}

		var parts = new System.Collections.Generic.List<string>(4);
		if ((mods & System.Windows.Input.ModifierKeys.Control) != 0) parts.Add("Ctrl");
		if ((mods & System.Windows.Input.ModifierKeys.Shift) != 0) parts.Add("Shift");
		if ((mods & System.Windows.Input.ModifierKeys.Alt) != 0) parts.Add("Alt");
		if ((mods & System.Windows.Input.ModifierKeys.Windows) != 0) parts.Add("Win");

		string keyText = null;
		if (key >= System.Windows.Input.Key.A && key <= System.Windows.Input.Key.Z)
		{
			keyText = ((char)('A' + (key - System.Windows.Input.Key.A))).ToString();
		}
		else if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
		{
			keyText = ((char)('0' + (key - System.Windows.Input.Key.D0))).ToString();
		}
		else if (key >= System.Windows.Input.Key.F1 && key <= System.Windows.Input.Key.F24)
		{
			keyText = $"F{(key - System.Windows.Input.Key.F1) + 1}";
		}
		else
		{
			switch (key)
			{
				case System.Windows.Input.Key.Up: keyText = "Up"; break;
				case System.Windows.Input.Key.Down: keyText = "Down"; break;
				case System.Windows.Input.Key.Left: keyText = "Left"; break;
				case System.Windows.Input.Key.Right: keyText = "Right"; break;
				case System.Windows.Input.Key.Home: keyText = "Home"; break;
				case System.Windows.Input.Key.End: keyText = "End"; break;
				case System.Windows.Input.Key.PageUp: keyText = "PageUp"; break;
				case System.Windows.Input.Key.PageDown: keyText = "PageDown"; break;
				case System.Windows.Input.Key.Insert: keyText = "Insert"; break;
				case System.Windows.Input.Key.Delete: keyText = "Delete"; break;
				case System.Windows.Input.Key.Tab: keyText = "Tab"; break;
				case System.Windows.Input.Key.Enter: keyText = "Enter"; break;
				case System.Windows.Input.Key.Escape: keyText = "Escape"; break;
				case System.Windows.Input.Key.Space: keyText = "Space"; break;
				case System.Windows.Input.Key.Back: keyText = "Backspace"; break;
			}
		}

		if (string.IsNullOrEmpty(keyText))
			return null;

		parts.Add(keyText);
		return string.Join("+", parts);
	}

	#endregion

	#region Dropdown picker

	private readonly System.Collections.Generic.List<string> _modifierOptions = new System.Collections.Generic.List<string>
	{
		"None","Ctrl","Shift","Alt","Win"
	};

	private readonly System.Collections.Generic.List<string> _keyOptions = BuildKeyOptions();

	private void InitializeDropdownSources()
	{
		DecreaseMod1.ItemsSource = _modifierOptions;
		DecreaseMod2.ItemsSource = _modifierOptions;
		DecreaseKeyCombo.ItemsSource = _keyOptions;

		IncreaseMod1.ItemsSource = _modifierOptions;
		IncreaseMod2.ItemsSource = _modifierOptions;
		IncreaseKeyCombo.ItemsSource = _keyOptions;
	}

	private void InitializeDropdownSelectionsFromSettings()
	{
		ApplySelectionsFromHotkey(ViewModel.Settings.BrightnessDecreaseHotkey,
			DecreaseMod1, DecreaseMod2, DecreaseKeyCombo);
		ApplySelectionsFromHotkey(ViewModel.Settings.BrightnessIncreaseHotkey,
			IncreaseMod1, IncreaseMod2, IncreaseKeyCombo);
	}

	private void SettingsOnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (_isUpdatingDropdowns) return;
		if (e.PropertyName == nameof(SettingsCore.BrightnessDecreaseHotkey))
		{
			_isUpdatingDropdowns = true;
			ApplySelectionsFromHotkey(ViewModel.Settings.BrightnessDecreaseHotkey,
				DecreaseMod1, DecreaseMod2, DecreaseKeyCombo);
			DecreaseLabelDisplay.Text = ViewModel.Settings.BrightnessDecreaseHotkey ?? "Not set";
			_isUpdatingDropdowns = false;
		}
		else if (e.PropertyName == nameof(SettingsCore.BrightnessIncreaseHotkey))
		{
			_isUpdatingDropdowns = true;
			ApplySelectionsFromHotkey(ViewModel.Settings.BrightnessIncreaseHotkey,
				IncreaseMod1, IncreaseMod2, IncreaseKeyCombo);
			IncreaseLabelDisplay.Text = ViewModel.Settings.BrightnessIncreaseHotkey ?? "Not set";
			_isUpdatingDropdowns = false;
		}
	}

	private void OnDecreaseDropdownChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isUpdatingDropdowns) return;
		var text = BuildHotkeyFromSelections(DecreaseMod1, DecreaseMod2, DecreaseKeyCombo);
		if (!string.IsNullOrEmpty(text))
		{
			_isUpdatingDropdowns = true;
			ViewModel.Settings.BrightnessDecreaseHotkey = text;
			_isUpdatingDropdowns = false;
		}
	}

	private void OnIncreaseDropdownChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_isUpdatingDropdowns) return;
		var text = BuildHotkeyFromSelections(IncreaseMod1, IncreaseMod2, IncreaseKeyCombo);
		if (!string.IsNullOrEmpty(text))
		{
			_isUpdatingDropdowns = true;
			ViewModel.Settings.BrightnessIncreaseHotkey = text;
			_isUpdatingDropdowns = false;
		}
	}

	private static string BuildHotkeyFromSelections(ComboBox mod1, ComboBox mod2, ComboBox keyCombo)
	{
		var m1 = mod1.SelectedItem as string;
		var m2 = mod2.SelectedItem as string;
		var k = keyCombo.SelectedItem as string;

		if (string.IsNullOrEmpty(k) || k == "None")
			return null;

		var mods = new System.Collections.Generic.List<string>(2);
		if (!string.IsNullOrEmpty(m1) && m1 != "None") mods.Add(m1);
		if (!string.IsNullOrEmpty(m2) && m2 != "None" && (m2 != m1)) mods.Add(m2);

		mods.Add(k);
		return string.Join("+", mods);
	}

	private static void ApplySelectionsFromHotkey(string text, ComboBox mod1, ComboBox mod2, ComboBox keyCombo)
	{
		mod1.SelectedItem = "None";
		mod2.SelectedItem = "None";
		keyCombo.SelectedItem = null;

		if (string.IsNullOrWhiteSpace(text))
			return;

		var parts = text.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(x => x.Trim())
			.ToArray();

		var mods = parts.Where(IsModifier).Distinct().ToList();
		var key = parts.FirstOrDefault(x => !IsModifier(x));

		if (mods.Count > 0) mod1.SelectedItem = mods[0];
		if (mods.Count > 1) mod2.SelectedItem = mods[1];
		if (!string.IsNullOrEmpty(key)) keyCombo.SelectedItem = key;
	}

	private static bool IsModifier(string s)
	{
		var lower = s?.ToLowerInvariant();
		return lower == "ctrl" || lower == "control" || lower == "shift" || lower == "alt" || lower == "win" || lower == "windows";
	}

	private static System.Collections.Generic.List<string> BuildKeyOptions()
	{
		var list = new System.Collections.Generic.List<string>();
		for (char c = 'A'; c <= 'Z'; c++) list.Add(c.ToString());
		for (char d = '0'; d <= '9'; d++) list.Add(d.ToString());
		for (int f = 1; f <= 24; f++) list.Add($"F{f}");
		list.AddRange(new[] { "Up","Down","Left","Right","Home","End","PageUp","PageDown","Insert","Delete","Tab","Enter","Escape","Space","Backspace" });
		return list;
	}

	#endregion

	#region Reset buttons

	private void OnResetBrightnessDownHotkey(object sender, RoutedEventArgs e)
	{
		_isUpdatingDropdowns = true;
		ViewModel.Settings.BrightnessDecreaseHotkey = "Ctrl+Shift+F9";
		_isUpdatingDropdowns = false;
	}

	private void OnResetBrightnessUpHotkey(object sender, RoutedEventArgs e)
	{
		_isUpdatingDropdowns = true;
		ViewModel.Settings.BrightnessIncreaseHotkey = "Ctrl+Shift+F10";
		_isUpdatingDropdowns = false;
	}

	#endregion

}