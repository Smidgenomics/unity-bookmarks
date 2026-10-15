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
	internal sealed class AssetDragOutManipulator : PointerManipulator
	{
		public AssetDragOutManipulator(VisualElement root)
		{
			_pointerId = -1;
			activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_active = false;
			_root = root;
		}

		private readonly VisualElement _root;
		public event Action onMouseDown;
		private Vector3 _start;
		private bool _active;
		private int _pointerId;
		private Vector2 _startSize;
		private bool _dragStarted;
		
		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<PointerDownEvent>(OnPointerDown);
			target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
			target.RegisterCallback<PointerUpEvent>(OnPointerUp);
			_root.RegisterCallback<PointerOutEvent>(OnPointerOut);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
			target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
			target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
			_root.UnregisterCallback<PointerOutEvent>(OnPointerOut);
		}

		private void OnPointerUp(PointerUpEvent e)
		{
			if (_active)
			{
				target.ReleasePointer(_pointerId);
				e.StopPropagation();
			}
			_active = false;
		}

		private void OnPointerOut(PointerOutEvent e)
		{
			if (_active)
			{
				target.ReleasePointer(_pointerId);
				e.StopPropagation();
			}
			_active = false;
		}

		private void OnPointerDown(PointerDownEvent e)
		{
			if (_active)
			{
				return;
			}

			if (target.userData != null && CanStartManipulation(e))
			{
				_dragStarted = false;
				_start = e.localPosition;
				_pointerId = e.pointerId;
				_active = true;
				target.CapturePointer(_pointerId);
				onMouseDown?.Invoke();
			}
		}

		private void OnPointerMove(PointerMoveEvent e)
		{
			if (!_active || !target.HasPointerCapture(_pointerId) || _dragStarted)
			{
				return;
			}

			var dist = Vector3.Distance(_start, e.localPosition);
	
			if (dist > 20)
			{
				var asset = target.userData as BKAsset;
				_active = false;
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.StartDrag(string.Empty);
				DragAndDrop.objectReferences = new [] { asset?.Asset };
				DragAndDrop.visualMode = DragAndDropVisualMode.None;
				_dragStarted = true;
				
			}
			e.StopPropagation();
		}
	}
}