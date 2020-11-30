using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Vertx.Utilities
{
	[AddComponentMenu("Layout/Pooled List View")]
	public class PooledListView : ScrollRect
	{
		private enum Snapping
		{
			None,
			Snapped
		}

		[SerializeField] private Snapping snapping;
		
		[SerializeField] private RectTransform prefab;
		[Min(0)]
		[SerializeField] private float elementHeight;

		[System.Serializable]
		public class BindEvent : UnityEvent<int, RectTransform> { }

		[SerializeField] private BindEvent bindItem;
		public BindEvent BindItem => bindItem;

		private LayoutElement startPaddingElement, endPaddingElement;
		private IList list;

		[SerializeField] private Selectable selectOnUp, selectOnDown, selectOnLeft, selectOnRight;

		public Selectable SelectOnUp
		{
			get => selectOnUp;
			set => selectOnUp = value;
		}
		
		public Selectable SelectOnDown
		{
			get => selectOnDown;
			set => selectOnDown = value;
		}
		
		public Selectable SelectOnLeft
		{
			get => selectOnLeft;
			set => selectOnLeft = value;
		}
		
		public Selectable SelectOnRight
		{
			get => selectOnRight;
			set => selectOnRight = value;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (!Application.IsPlaying(this))
				return;

			if (verticalScrollbar != null)
			{
				//I wish I didn't have to remove the listener, but otherwise the Snapped behaviour will not function properly.
				verticalScrollbar.onValueChanged.RemoveAllListeners();
				verticalScrollbar.onValueChanged.AddListener(Position);
			}

			if (content != null)
			{
				var go = content.gameObject;
				if (!go.TryGetComponent(out ContentSizeFitter fitter))
					fitter = go.AddComponent<ContentSizeFitter>();
				fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			}

			horizontal = false;
			vertical = true;
			horizontalScrollbar = null;
		}

		public void Bind(IList elements)
		{
			if (!Application.IsPlaying(this))
			{
				Debug.LogWarning($"{nameof(Bind)} should not be called in Edit Mode");
				return;
			}

			list = elements;
			Refresh();
		}
		
		void Pool()
		{
			startPaddingElement.transform.SetSiblingIndex(0);
			endPaddingElement.transform.SetSiblingIndex(1);
			foreach (var instance in boundInstances)
				InstancePool.Pool(prefab, instance.Value);

			boundInstances.Clear();
		}

		private readonly Dictionary<int, RectTransform> boundInstances = new Dictionary<int, RectTransform>();

		public void Refresh()
		{
			if (!Application.IsPlaying(this))
			{
				Debug.LogWarning($"{nameof(Refresh)} should not be called in Edit Mode");
				return;
			}
			
			if (startPaddingElement == null)
			{
				startPaddingElement = new GameObject("Start Padding", typeof(RectTransform), typeof(LayoutElement)).GetComponent<LayoutElement>();
				startPaddingElement.transform.SetParent(content);
				endPaddingElement = new GameObject("End Padding", typeof(RectTransform), typeof(LayoutElement)).GetComponent<LayoutElement>();
				endPaddingElement.transform.SetParent(content);
			}

			Pool();

			if (list == null || list.Count == 0)
				return;

			var scrollbar = verticalScrollbar;
			float value = scrollbar == null ? 0 : scrollbar.value;

			Position(value);
		}

		private readonly List<int> toRemove = new List<int>();

		void Position(float value)
		{
			if (list == null || list.Count == 0)
				return;

			int elementCount = list.Count;

			value = 1 - value; // thanks
			
			float rectHeight = viewport.rect.height;
			float onScreenElements = rectHeight / elementHeight;
			float totalElementHeight = elementHeight * elementCount;
			const float startIndex = 0;
			float endIndex = elementCount - onScreenElements;

			float zeroIndex = Mathf.Lerp(startIndex, endIndex, value);

			if (snapping == Snapping.Snapped)
			{
				zeroIndex = Mathf.RoundToInt(zeroIndex);
				value = Mathf.InverseLerp(startIndex, endIndex, zeroIndex);
				verticalScrollbar.SetValueWithoutNotify(1 - value);
			}
			SetNormalizedPosition(1 - value, 1);

			float zeroIndexInUse = Mathf.Max(0, zeroIndex - 1);
			float endIndexInUse = Mathf.Min(zeroIndex + onScreenElements + 1, elementCount);

			toRemove.Clear();
			float extra = zeroIndexInUse % 1;
			float endIndexInt = endIndexInUse - extra;
			foreach (KeyValuePair<int, RectTransform> pair in boundInstances)
			{
				if (pair.Key >= zeroIndexInUse && pair.Key < endIndexInt)
					continue;
				
				toRemove.Add(pair.Key);
				// You love to see it
				if (!CanvasUpdateRegistry.IsRebuildingLayout())
					InstancePool.Pool(prefab, pair.Value);
				else
					StartCoroutine(PoolWithDelay(pair.Value));
			}

			foreach (int i in toRemove)
				boundInstances.Remove(i);

			Selectable prev = null;
			int c = 0;
			for (float v = zeroIndexInUse; v < endIndexInUse; v++, c++)
			{
				int i = Mathf.FloorToInt(v);
				if (!boundInstances.TryGetValue(i, out var instance))
				{
					boundInstances.Add(i, instance = InstancePool.Get(prefab, content, Vector3.zero, Quaternion.identity, Space.Self));
					BindItem.Invoke(i, instance);
				}

				instance.SetSiblingIndex(c + 1);

				//Automatic navigation setup
				if (!instance.TryGetComponent<Selectable>(out var next))
					continue;
				
				next.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = i == 0 ? selectOnUp : prev,
					selectOnLeft = selectOnLeft,
					selectOnRight = selectOnRight,
					//This is okay to assign here as it should be overridden when appropriate
					selectOnDown = i == elementCount - 1 ? selectOnDown : null
				};
				if (prev != null)
				{
					Navigation prevNav = prev.navigation;
					prevNav.selectOnDown = next;
					prev.navigation = prevNav;
				}

				prev = next;
			}

			startPaddingElement.transform.SetSiblingIndex(0);
			endPaddingElement.transform.SetSiblingIndex(content.childCount - 1);

			float startPadding = Mathf.Floor(zeroIndexInUse) * elementHeight;
			startPaddingElement.preferredHeight = startPadding;
			((RectTransform) startPaddingElement.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, startPadding);
			float endPadding = totalElementHeight - (startPadding + c * elementHeight);
			endPaddingElement.preferredHeight = endPadding;
			((RectTransform) endPaddingElement.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, endPadding);
		}

		IEnumerator PoolWithDelay(RectTransform value)
		{
			yield return new WaitForEndOfFrame();
			InstancePool.Pool(prefab, value);
		}
	}
}