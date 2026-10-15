// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using UnityEngine;
	using UnityEngine.UIElements;
	using System;
	using UnityEditor;

	/// <summary>
	/// Used for dropping assets into scenes
	/// </summary>
	internal sealed class DoubleClickManipulator : PointerManipulator
	{
		public DoubleClickManipulator()
		{
			activators.Add(new ManipulatorActivationFilter
			{
				button = MouseButton.LeftMouse,
				clickCount = 2
			});
		}

		public event Action<ClickEvent> onClick;

		
		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<ClickEvent>(OnClick);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<ClickEvent>(OnClick);
		}

		private void OnClick(ClickEvent e)
		{
			if (e.clickCount == 2)
			{
				onClick?.Invoke(e);
			}
		}

	}
}