using System;
using System.Collections.Generic;

namespace Sodalite.Utilities;

/// <summary>
///     Utility class that allows multiple mods to request something be locked
/// </summary>
public class SafeMultiLock
{
	private readonly HashSet<Lock> _locks = new();

	/// <summary>
	///     True if one or more locks are held for this instance
	/// </summary>
	public bool IsLocked => _locks.Count > 0;

	/// <summary>
	///     Returns a disposable lock which can be disposed to return
	/// </summary>
	/// <returns></returns>
	public IDisposable TakeLock()
	{
		return new Lock(_locks);
	}

	private class Lock : IDisposable
	{
		private readonly HashSet<Lock> _locks;

		public Lock(HashSet<Lock> locks)
		{
			_locks = locks;
			_locks.Add(this);
		}

		void IDisposable.Dispose()
		{
			_locks.Remove(this);
		}
	}
}
