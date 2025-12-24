using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Monitorian.Core
{

	[Flags]
	public enum ModifierKeyCodes : uint
	{
		Alt = 1,
		Control = 2,
		Shift = 4,
		Windows = 8
	}

	/// <summary>
	/// Virtual Key Codes
	/// </summary>
	public enum VirtualKeyCodes : uint
	{
		Backspace = 0x08,
		Tab = 0x09,
		Enter = 0x0D,
		Escape = 0x1B,
		Space = 0x20,
		PageUp = 0x21,
		PageDown = 0x22,
		End = 0x23,
		Home = 0x24,
		Left = 0x25,
		Up = 0x26,
		Right = 0x27,
		Down = 0x28,
		Insert = 0x2D,
		Delete = 0x2E,
		D0 = 0x30,
		D1 = 0x31,
		D2 = 0x32,
		D3 = 0x33,
		D4 = 0x34,
		D5 = 0x35,
		D6 = 0x36,
		D7 = 0x37,
		D8 = 0x38,
		D9 = 0x39,
		A = 65,
		B = 66,
		C = 67,
		D = 68,
		E = 69,
		F = 70,
		G = 71,
		H = 72,
		I = 73,
		J = 74,
		K = 75,
		L = 76,
		M = 77,
		N = 78,
		O = 79,
		P = 80,
		Q = 81,
		R = 82,
		S = 83,
		T = 84,
		U = 85,
		V = 86,
		W = 87,
		X = 88,
		Y = 89,
		Z = 90,
		F1 = 0x70,
		F2 = 0x71,
		F3 = 0x72,
		F4 = 0x73,
		F5 = 0x74,
		F6 = 0x75,
		F7 = 0x76,
		F8 = 0x77,
		F9 = 0x78,
		F10 = 0x79,
		F11 = 0x7A,
		F12 = 0x7B,
		F13 = 0x7C,
		F14 = 0x7D,
		F15 = 0x7E,
		F16 = 0x7F,
		F17 = 0x80,
		F18 = 0x81,
		F19 = 0x82,
		F20 = 0x83,
		F21 = 0x84,
		F22 = 0x85,
		F23 = 0x86,
		F24 = 0x87
	}

	class KeyboardHook : IDisposable
	{
		[DllImport("user32.dll")]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		[DllImport("user32.dll")]
		public static extern bool RegisterHotKey(IntPtr hWnd, int id, ModifierKeyCodes fdModifiers, VirtualKeyCodes vk);

		#region Fields
		WindowInteropHelper host;
		bool IsDisposed = false;
		int Identifier;

		public Window Window { get; private set; }

		public VirtualKeyCodes Key { get; private set; }

		public ModifierKeyCodes Modifiers { get; private set; }
		#endregion

		public KeyboardHook(Window Window, VirtualKeyCodes Key, ModifierKeyCodes Modifiers)
		{
			this.Key = Key;
			this.Modifiers = Modifiers;

			this.Window = Window;
			host = new WindowInteropHelper(Window);

			Identifier = new object().GetHashCode();

			RegisterHotKey(host.Handle, Identifier, Modifiers, Key);

			ComponentDispatcher.ThreadPreprocessMessage += ProcessMessage;
		}

		void ProcessMessage(ref MSG msg, ref bool handled)
		{
			if ((msg.message == 786) && (msg.wParam.ToInt32() == Identifier) && (Triggered != null))
				Triggered();
		}

		public event Action Triggered;

		public void Dispose()
		{
			if (!IsDisposed)
			{
				ComponentDispatcher.ThreadPreprocessMessage -= ProcessMessage;

				UnregisterHotKey(host.Handle, Identifier);
				Window = null;
				host = null;
			}
			IsDisposed = true;
		}
	}
}
