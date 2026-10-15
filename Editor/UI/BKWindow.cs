// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.UIElements;
	using UnityEngine.UIElements;
	using UObject = UnityEngine.Object;

	internal sealed class BKWindow : EditorWindow
	{

		[MenuItem("Window/Bookmarks", false, -40)]
		public static void Open() => CreateWindow<BKWindow>(_DOCK_TYPES).Show();

		// context menu handler
		private delegate void AssetContextFn(BKUser u, BKItem a);
		private delegate bool AssetContextGuard(BKUser u, BKItem a);

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

		private static bool IsAsset(BKUser u, BKItem a) => a.ItemType == EBKItemType.Asset;
		private static bool IsExternal(BKUser u, BKItem a) => a.ItemType == EBKItemType.External;
		private static bool CanOpenItem(BKUser u, BKItem a) => a.CanOpenFile();

		// declared here for convenience
		private static readonly (GUIContent, AssetContextGuard, AssetContextFn)[] _ASSET_CTX_ACTIONS =
		{
			(new GUIContent("Copy GUID"), IsAsset, (_,a) =>
			{
				GUIUtility.systemCopyBuffer = a.GUID;
			}),
			(new GUIContent("Copy Path"), IsAsset, (_,a) =>
			{
				GUIUtility.systemCopyBuffer = a.FilePath;
			}),
			(new GUIContent("Copy Type"), IsAsset, (_,a) =>
			{
				GUIUtility.systemCopyBuffer = a.MainType.Name;
			}),
			(null, IsAsset, null),
			(new GUIContent("Exclude"), null, (u,a) =>
			{
				if (a.ItemType == EBKItemType.Asset)
				{
					u.RemoveRef(a.GUID);
				}
				else
				{
					u.RemoveRef(a.FilePath);
				}
			}),
			(null, CanOpenItem, null),
			(new GUIContent("Open..."), CanOpenItem, (u,a) =>
			{
				a.Open();
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

			// win.RegisterCallbackOnce<DragEnterEvent>(ev =>
			// {
			// 	
			// 	Debug.Log("drag enter");
			// });
			//
			// win.RegisterCallbackOnce<DragPerformEvent>(ev =>
			// {
			// 	Debug.Log("drag perform");
			// });

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
			el.userData = _viewData.items[i].asset;

			// register handlers only once
			if (!el.ClassListContains("events"))
			{
				el.AddToClassList("events");
				el.RegisterCallback<ContextClickEvent>(_ =>
				{
					OnItemContext(el.userData as BKItem);
				});

				var dragManip = new AssetDragOutManipulator(rootVisualElement);
				dragManip.onMouseDown += () =>
				{
					OnItemClicked(el.userData as BKItem);
				};

				var dblClickManip = new DoubleClickManipulator();
				dblClickManip.onClick += e =>
				{
					if (el.userData is BKItem item && item.CanOpenFile())
					{
						item.Open();
					}
				};

				el.AddManipulator(dblClickManip);
				el.AddManipulator(dragManip);
			}
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
		}

		private void OnItemContext(BKItem a)
		{
			if (a == null)
			{
				return;
			}
			var m = new GenericMenu();
			foreach (var (l, guardFn, fn) in _ASSET_CTX_ACTIONS)
			{
				if (guardFn != null && !guardFn.Invoke(_bkUser, a))
				{
					continue;
				}
				if (l == null)
				{
					m.AddSeparator(string.Empty);
					continue;
				}
				m.AddItem(l, false, () => fn.Invoke(_bkUser, a));
			}
			m.ShowAsContext();
		}

		private void OnItemClicked(BKItem a)
		{
			if (a == null)
			{
				return;
			}
			var sz = BKUtils.FormatFileSize(a.FileSize);
			var txt = $"{a.FilePath} // {sz}";
			_viewData.footerText = txt;
			a.Select();
		}

		private void OnClickRefresh(ClickEvent ev)
		{
			OnViewChanged();
		}

		private void OnClickLoad(ClickEvent ev)
		{
			var m = new GenericMenu
			{
				allowDuplicateNames = true
			};

			if (_bkUser.HasProfile)
			{
				m.AddItem(_LB_EDIT, false, _bkUser.EditCurrentProfile);
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

			m.AddItem(_LB_ALL, string.IsNullOrEmpty(_bkUser.FilterKey), () =>
			{
				_bkUser.FilterKey = null;
			});

			m.AddSeparator(string.Empty);

			foreach (var v in _bkUser.Filters)
			{
				var cv = v;
				var active = v == _bkUser.CurrentFilter;
				m.AddItem(new GUIContent(v.Title), active, () =>
				{
					_bkUser.FilterKey = cv.ViewKey;
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