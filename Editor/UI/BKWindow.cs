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
		private delegate void AssetContextFn(in ItemActionContext ctx);
		private delegate bool AssetContextGuard(in ItemActionContext ctx);

		private static readonly GUIContent _LB_EDIT = new ("Edit...");
		private static readonly GUIContent _LB_OPEN = new ("Open...");
		private static readonly GUIContent _LB_ALL = new ("All");

		// element refs
		private ListView _itemList;
		private ToolbarSearchField _search;
		private VisualElement _filterBtn;
		private readonly BKWindowViewData _viewData = new(); // data bound to UI
		private (bool, double) _bufferedFilter; // edited/time
		[SerializeField] private BKUser _bkUser = new(); // view manager

		private static readonly Type[] _DOCK_TYPES =
		{
			typeof(BKWindow), // prefer to dock next to other bookmarks
			// project browser
			Type.GetType("UnityEditor.ProjectBrowser, UnityEditor.CoreModule")
		};

		private static bool IsAsset(in ItemActionContext ctx) => ctx.item.ItemType == EBKItemType.Asset;
		private static bool IsExternal(in ItemActionContext ctx) => ctx.item.ItemType == EBKItemType.External;
		private static bool CanOpenItem(in ItemActionContext ctx) => ctx.item.CanOpenFile();

		private struct ItemActionContext
		{
			public BKItem item;
			public BKUser user;
		}

		// declared here for convenience
		private static readonly (GUIContent, AssetContextGuard, AssetContextFn)[] _ASSET_CTX_ACTIONS =
		{
			(new GUIContent("Copy/GUID"), IsAsset, (in ItemActionContext ctx) =>
			{
				ctx.item.GUID.CopyToClipboard();
			}),
			(new GUIContent("Copy/Path"), null, (in ItemActionContext ctx) =>
			{
				ctx.item.FilePath.CopyToClipboard();
			}),
			(new GUIContent("Copy/Type"), IsAsset, (in ItemActionContext ctx) =>
			{
				ctx.item.MainType.Name.CopyToClipboard();
			}),
			default,
			(new GUIContent("Exclude"), null, (in ItemActionContext ctx) =>
			{
				var a = ctx.item;
				var u = ctx.user;
				if (a.ItemType == EBKItemType.Asset)
				{
					u.RemoveRef(a.GUID);
				}
				else
				{
					u.RemoveRef(a.FilePath);
				}
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

		private void RefreshTitleContent()
		{
			titleContent.text = _bkUser.GetDisplayTitle();
			titleContent.image = _bkUser.GetDisplayIcon();
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
			_itemList.bindItem = OnBindListItem;
			_itemList.itemsSource = _viewData.items;

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

			win.Q<ToolbarButton>(name:"Refresh").RegisterCallback<ClickEvent>(OnClickRefresh);
			win.Q<VisualElement>(name:"Load").RegisterCallback<ClickEvent>(OnClickLoad);
			win.Q<VisualElement>(name:"Sort").RegisterCallback<ClickEvent>(OnClickSort);

			_filterBtn = win.Q<VisualElement>(name:"Filter");
			_filterBtn.RegisterCallback<ClickEvent>(OnClickFilter);

			OnViewChanged();
		}

		private void OnBindListItem(VisualElement el, int i)
		{
			el.userData = _viewData.items[i].item;

			EditorApplication.delayCall += () =>
			{
				var typeIcon = el.Q<Image>(className: "sm-bookmarks__item__type-icon");
				typeIcon.MarkDirtyRepaint();
			};

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
				dblClickManip.onClick += _ =>
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

		private void OpenFilePicker()
		{
			var path = EditorUtility.OpenFilePanel("Load Bookmarks", "/","bk.json");

			if (path.Length < BKConstants.PROJECT_ROOT.Length)
			{
				return;
			}
			path = path[(BKConstants.PROJECT_ROOT.Length + 1)..];
			_viewData.items.Clear();
			_bkUser.Load(path);
			OnViewChanged();
		}

		private void OnViewChanged()
		{
			_viewData.items.Clear();
			_itemList?.Rebuild();
			foreach (var a in _bkUser.VisibleAssets)
			{
				_viewData.items.Add(new BKItemViewData
				{
					item = a,
					prefs = _viewData.prefs
				});
			}
			_itemList?.Rebuild();
			_itemList?.MarkDirtyRepaint();

			_filterBtn?.Q<Image>()?.MarkDirtyRepaint();
			
			RefreshTitleContent();

		}

		private void OnItemContext(BKItem a)
		{
			if (a == null)
			{
				return;
			}
			var m = new GenericMenu();

			var ctx = new ItemActionContext
			{
				item = a,
				user = _bkUser
			};
			foreach (var (l, guardFn, fn) in _ASSET_CTX_ACTIONS)
			{
				if (guardFn != null && !guardFn.Invoke(ctx))
				{
					continue;
				}
				if (l == null)
				{
					m.AddSeparator(string.Empty);
					continue;
				}
				m.AddItem(l, false, () => fn.Invoke(ctx));
			}
			m.ShowAsContext();
		}

		private void OnItemClicked(BKItem a)
		{
			if (a == null)
			{
				return;
			}
			a.Select();
			_viewData.prefs.lastClicked = a;
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

			if (_bkUser.HasProfile && _bkUser.CanEditCurrentProfile())
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
			
			m.AddSeparator(string.Empty);

			m.AddItem(_LB_OPEN, false, OpenFilePicker);

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
			EditorApplication.delayCall += OnViewChanged;
		}

		private void OnDisable()
		{
			_bkUser.viewChanged -= OnViewChanged;
		}
		
	}
}