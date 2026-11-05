using System;
using GlassDesk.Core.Input;

namespace GlassDesk.AppServices;

public interface IHotkeyRegistry : IDisposable
{
	bool TryRegister(int id, HotkeyChord chord, out int errorCode);

	bool Unregister(int id);
}
