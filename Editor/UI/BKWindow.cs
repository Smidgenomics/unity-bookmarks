// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.UIElements;
	using UnityEngine.UIElements;
	using UObject = UnityEngine.Object;

	internal class BKWindow : EditorWindow
	{

		[MenuItem("Window/Bookmarks", false, -40)]
		public static void Open() => CreateWindow<BKWindow>(_DOCK_TYPES).Show();

		// context menu handler
		private delegate void AssetContextFn(BKUser u, BKAsset a);

		private static readonly GUIContent _LB_EDIT = new ("Edit...");
		private static readonly GUIContent _LB_ALL = new ("All");

		// element refs
		private ListView _itemList;
		private ToolbarSearchField _search;
		private readonly BKWindowViewData _viewData = new(); // data bound to UI
		private (bool, double) _bufferedFilter; // edited/time
		[SerializeField] private BKUser _bkUser = new(); // view manager

		private static readonly Type[] _DOCK_TYPES =
		{
			typeof(BKWindow), // prefer to dock next to other bookmarks
			// project browser
			Type.GetType("UnityEditor.ProjectBrowser, UnityEditor.CoreModule")
		};

		// declared here for convenience
		private static readonly (GUIContent, AssetContextFn)[] _ASSET_CTX_ACTIONS =
		{
			(new GUIContent("Copy GUID"), (_,a) =>
			{
				GUIUtility.systemCopyBuffer = a.GUID;
			}),
			(new GUIContent("Copy Path"), (_,a) =>
			{
				GUIUtility.systemCopyBuffer = a.AssetPath;
			}),
			(new GUIContent("Copy Type"), (_,a) =>
			{
				GUIUtility.systemCopyBuffer = a.MainType.Name;
			}),
			default,
			(new GUIContent("Exclude"), (u,a) =>
			{
				u.RemoveGUID(a.GUID);
			}),
		};

		private static T LoadGUID<T>(string guid) where T : UObject
		{
			return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
		}

		// Note: could be optimized to only run on demand
		private void Update()
		{
			// we do this to avoid repeated filtering while typing
			if (_bufferedFilter.Item1 && _bufferedFilter.Item2 < EditorApplication.timeSinceStartup)
			{
				_bkUser.FilterString = _search.value;
				_bufferedFilter = default;
			}
		}

		private void CreateGUI()
		{
			_viewData.user = _bkUser;
			_viewData.items.Clear();

			var tmp = LoadGUID<VisualTreeAsset>(BKConstants.WIN_TEMPLATE_GUID);
			var win = tmp.Instantiate();

			win.dataSource = _viewData;

			rootVisualElement.Add(win);
			
			_itemList = win.Q<ListView>();

			_search = win.Q<ToolbarSearchField>();
			_search.value = _bkUser.FilterString;
			_search.RegisterValueChangedCallback(ev =>
			{
				// search cleared, update immediately
				if (ev.newValue.Length == 0)
				{
					_bufferedFilter = (true, 0);
					return;
				}
				_bufferedFilter = (true, EditorApplication.timeSinceStartup + BKConstants.SEARCH_BUFFER_TIME);
			});

			_itemList.bindItem = OnBindListItem;
			_itemList.itemsSource = _viewData.items;

			win.Q<ToolbarButton>(name:"Refresh").RegisterCallback<ClickEvent>(OnClickRefresh);
			win.Q<VisualElement>(name:"Load").RegisterCallback<ClickEvent>(OnClickLoad);
			win.Q<VisualElement>(name:"Filter").RegisterCallback<ClickEvent>(OnClickFilter);
			win.Q<VisualElement>(name:"Sort").RegisterCallback<ClickEvent>(OnClickSort);

			OnViewChanged();
		}

		private void OnBindListItem(VisualElement el, int i)
		{
			if (i < 0 || i >= _viewData.items.Count)
			{
				return;
			}
			var data = _viewData.items[i];
			var a = data.asset;
			
			el.RegisterCallback<ContextClickEvent>(_ =>
			{
				OnItemContext(i);
			});

			var dragManip = new AssetDragOutManipulator(rootVisualElement, a?.Asset);
			
			el.AddManipulator(dragManip);

			dragManip.onMouseDown += () =>
			{
				OnItemClicked(i);
			};
		}

		private void OnViewChanged()
		{
			_viewData.items.Clear();
			foreach (var a in _bkUser.Assets)
			{
				_viewData.items.Add(new BKItemViewData
				{
					asset = a
				});
			}
			_itemList?.Rebuild();
			titleContent = new GUIContent(_bkUser.GetDisplayTitle(), _bkUser.GetDisplayIcon());
			Repaint();
		}

		private void OnItemContext(int i)
		{
			if (i < 0 || i >= _viewData.items.Count)
			{
				return;
			}
			var data = _viewData.items[i];
			var a = data.asset;
			var m = new GenericMenu();

			foreach (var (l, fn) in _ASSET_CTX_ACTIONS)
			{
				if (l == null)
				{
					m.AddSeparator(string.Empty);
					continue;
				}
				m.AddItem(l, false, () => fn.Invoke(_bkUser, a));
			}
			m.ShowAsContext();
		}

		private void OnItemClicked(int i)
		{
			if (i < 0 || i >= _viewData.items.Count)
			{
				return;
			}
			var data = _viewData.items[i];
			var a = data.asset;
			var sz = Helpers.GetFileSizeLabel(a.FileSize);
			var txt = $"{a.AssetPath} // {sz}";
			_viewData.footerText = txt;
			Selection.activeObject = a?.Asset;
		}

		private void OnClickRefresh(ClickEvent ev)
		{
			OnViewChanged();
		}

		private void OnClickLoad(ClickEvent ev)
		{
			var m = new GenericMenu();

			if (_bkUser.HasProfile)
			{
				m.AddItem(_LB_EDIT, false, _bkUser.Edit);
				m.AddSeparator(string.Empty);
			}

			var options = BKProfile.GetProfileOptions();

			foreach (var (l, path) in options)
			{
				var active = _bkUser.ProfilePath == path;
				var fPath = path;
				m.AddItem(new GUIContent(l), active, () =>
				{
					_viewData.items.Clear();
					_bkUser.Load(fPath);
					OnViewChanged();
				});
			}

			if (ev.target is VisualElement _)
			{
				m.ShowAsContext();
			}
		}

		private void OnClickFilter(ClickEvent ev)
		{
			var m = new GenericMenu();

			m.AddItem(_LB_ALL, string.IsNullOrEmpty(_bkUser.View), () =>
			{
				_bkUser.View = null;
			});

			m.AddSeparator(string.Empty);

			foreach (var v in _bkUser.Views)
			{
				var cv = v;
				var active = v == _bkUser.CurrentView;
				m.AddItem(new GUIContent(v.Label), active, () =>
				{
					_bkUser.View = cv.Key;
				});
			}
			m.ShowAsContext();
		}

		private void OnClickSort(ClickEvent ev)
		{
			// sort columns
		}
		
		private void OnEnable()
		{
			_bkUser.viewChanged -= OnViewChanged;
			_bkUser.viewChanged += OnViewChanged;
			titleContent = new GUIContent(_bkUser.GetDisplayTitle(), _bkUser.GetDisplayIcon());
			OnViewChanged();
		}

		private void OnDisable()
		{
			_bkUser.viewChanged -= OnViewChanged;
		}
		
	}
}