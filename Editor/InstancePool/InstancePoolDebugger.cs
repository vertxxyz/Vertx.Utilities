using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.Utilities.Editor
{
	public class InstancePoolDebugger : EditorWindow
	{
		[MenuItem("Window/Analysis/Instance Pool Debugger")]
		private static void Open()
		{
			var instancePoolDebugger = GetWindow<InstancePoolDebugger>();
			instancePoolDebugger.titleContent = new GUIContent("Instance Pool Debugger");
			instancePoolDebugger.Show();
		}

		private Type instancePoolType;
		private IntegerField valuesCount;
		private ListView keysListView, valuesListView;

		[SerializeField] private Component selectedKey;
		private readonly List<Component> componentKeys = new List<Component>();
		private readonly List<string> keyedPools = new List<string>();
		private readonly List<Component> componentValues = new List<Component>();

		[Flags]
		private enum RefreshMode
		{
			Data = 1,
			Keys = 1 << 1,
			Values = 1 << 2,
			DataAndValues = Data | Values,
			DataAndKeys = Data | Keys,
			All = Data | Keys | Values
		}

		private void Refresh(RefreshMode valuesOnly = RefreshMode.All)
		{
			componentKeys.Clear();
			keyedPools.Clear();

			object selectedPool = null;

			if ((valuesOnly & RefreshMode.Data) != 0)
			{
				// Gather all pool keys
				foreach (IComponentPoolGroup componentPoolGroup in InstancePool.s_instancePools)
				{
					foreach (DictionaryEntry o in componentPoolGroup.PoolGroup)
					{
						var key = (Component)o.Key;
						componentKeys.Add(key);
						keyedPools.Add(TypeNameWithoutGenerics(o.Value.GetType()));

						if (selectedKey != key) continue;
						// value = IComponentPool<TInstanceType>
						selectedPool = o.Value;
						
						static string TypeNameWithoutGenerics(Type t)
						{
							string name = t.Name;
							int index = name.IndexOf('`');
							return index == -1 ? name : name.Substring(0, index);
						}
					}
				}
			}

			// Refresh key list
			if ((valuesOnly & RefreshMode.Keys) != 0)
			{
				RebuildList(keysListView);
				if (selectedKey != null)
					keysListView.selectedIndex = componentKeys.IndexOf(selectedKey);
			}

			if ((valuesOnly & RefreshMode.Values) != 0)
			{
				if (selectedPool == null)
					ClearList(valuesListView);
				else
				{
					componentValues.Clear();
					foreach (Component o in (IEnumerable)selectedPool)
						componentValues.Add(o);
					RebuildList(valuesListView);
				}

				valuesCount.value = componentValues.Count;
			}
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += ChangedPlayMode;
			Refresh(RefreshMode.Data);

			var root = rootVisualElement;
			// Padding
			var padding = new VisualElement
			{
				style = { marginBottom = 10, marginLeft = 5, marginRight = 5, marginTop = 10 }, name = "Padding"
			};
			root.Add(padding);
			padding.StretchToParentSize();
			root = padding;

			root.Add(new Label("Instance Pool:") { style = { unityFontStyleAndWeight = FontStyle.Bold } });

			root.Add(new Button(() => Refresh())
			{
				text = "Refresh"
			});

			// Component Key list
			root.Add(new Label("Key"));
			keysListView = new ListView(componentKeys, (int)EditorGUIUtility.singleLineHeight, () =>
			{
				VisualElement entry = CreateListEntry(true);
				entry.Q<ObjectField>().pickingMode = PickingMode.Ignore;
				return entry;
			}, (element, i) =>
			{
				var objectField = element.Q<ObjectField>();
				objectField.SetValueWithoutNotify(componentKeys[i]);
				objectField.label = keyedPools[i];
			})
			{
				style =
				{
					height = 50,
					backgroundColor = new Color(0, 0, 0, 0.15f),
					marginTop = 5
				}
			};
			keysListView.selectedIndicesChanged += indices =>
			{
				int[] ind = indices.ToArray();
				if (ind.Length > 1)
					keysListView.selectedIndex = ind.First();
				else
				{
					selectedKey = componentKeys[ind[0]];
					Refresh(RefreshMode.DataAndValues);
				}
			};
			root.Add(keysListView);

#if UNITY_2020_1_OR_NEWER
			HelpBox container = new HelpBox("Data is not refreshed in realtime.", HelpBoxMessageType.Warning);
			root.Add(container);
#else
			Label container = new Label("Data is not refreshed in realtime.");
			root.Add(container);
#endif

			// List View
			root.Add(valuesCount = new IntegerField("Values")
			{
				value = componentValues.Count
			});
			valuesCount.SetEnabled(false);
			valuesListView = new ListView(componentValues, (int)EditorGUIUtility.singleLineHeight, CreateListEntryNoLabel, (element, i) =>
			{
				var objectField = element.Q<ObjectField>();
				objectField.SetValueWithoutNotify(componentValues[i]);
			})
			{
				style =
				{
					flexGrow = 1,
					backgroundColor = new Color(0, 0, 0, 0.15f),
					marginTop = 5
				}
			};
			root.Add(valuesListView);

			rootVisualElement.SetEnabled(Application.isPlaying);

			VisualElement CreateListEntryNoLabel() => CreateListEntry(false);
			
			VisualElement CreateListEntry(bool label)
			{
				var element = new VisualElement
				{
					pickingMode = PickingMode.Ignore
				};

				ObjectField objectField;
				if (label)
				{
					objectField = new ObjectField("Label");
					objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
				}
				else
					objectField = new ObjectField { objectType = typeof(Component) };
				element.Add(objectField);
				objectField.Q(className: ObjectField.selectorUssClassName).style.display = DisplayStyle.None;
				objectField.RegisterValueChangedCallback(evt => objectField.SetValueWithoutNotify(evt.previousValue));
				return element;
			}
		}

		private void OnDisable() => EditorApplication.playModeStateChanged -= ChangedPlayMode;

		private void ChangedPlayMode(PlayModeStateChange obj)
		{
			rootVisualElement.SetEnabled(obj == PlayModeStateChange.EnteredPlayMode);
			switch (obj)
			{
				case PlayModeStateChange.EnteredPlayMode:
					rootVisualElement.SetEnabled(true);
					Refresh();
					break;
				case PlayModeStateChange.ExitingPlayMode:
					rootVisualElement.SetEnabled(false);
					ClearList(valuesListView);
					ClearList(keysListView);
					break;
				case PlayModeStateChange.EnteredEditMode:
				case PlayModeStateChange.ExitingEditMode:
				default:
					return;
			}
		}

		private static void RebuildList(ListView listView)
#if UNITY_2021_2_OR_NEWER
			=> listView.Rebuild();
#else
			=> listView.Refresh();
#endif

		private static void ClearList(ListView listView)
		{
			listView.itemsSource?.Clear();
			RebuildList(listView);
		}
	}
}