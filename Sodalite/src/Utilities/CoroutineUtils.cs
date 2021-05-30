using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sodalite
{
	public static class CoroutineUtils
	{


		/// <summary>
		/// Runs the given coroutine, with an option to perform an action in the event of an error
		/// </summary>
		/// <remarks>
		/// Primarily useful for when an error happens while yielding something
		/// </remarks>
		/// <param name="routine">The coroutine that will be run</param>
		/// <param name="onError">An action that is performed when an uncaught error happens in the coroutine</param>
		public static void RunCoroutine(IEnumerator routine, Action<Exception>? onError = null)
		{
			AnvilManager.Instance.StartCoroutine(RunAndCatch(routine, onError));
		}




		/// <summary>
		/// Runs the given coroutine, with an option to perform an action in the event of an error. This runs the coroutine using anvil manager, allowing you to yield anvil callbacks. This does not support other yields, such as WaitForSeconds
		/// </summary>
		/// <remarks>
		/// Primarily useful for when an error happens while yielding something
		/// </remarks>
		/// <param name="routine">The coroutine that will be run</param>
		/// <param name="onError">An action that is performed when an uncaught error happens in the coroutine</param>
		public static void RunAnvilCoroutine(IEnumerator routine, Action<Exception>? onError = null)
		{
			AnvilManager.Run(RunAndCatch(routine, onError));
		}




		/// <summary>
		/// A coroutine that wraps around another given coroutine, and can perform an action in the evne of an error
		/// </summary>
		/// <remarks>
		/// Primarily useful for when an error happens while yielding something
		/// </remarks>
		/// <param name="routine"></param>
		/// <param name="onError"></param>
		/// <returns></returns>
		public static IEnumerator RunAndCatch(IEnumerator routine, Action<Exception>? onError = null)
		{
			bool more = true;
			while (more)
			{
				//Try to run the routine
				try
				{
					more = routine.MoveNext();
				}

				//Catch any error that isn't handled in the routine
				catch (Exception e)
				{
					if (onError != null)
					{
						onError(e);
					}

					yield break;
				}

				if (more)
				{
					yield return routine.Current;
				}
			}
		}

	}
}
