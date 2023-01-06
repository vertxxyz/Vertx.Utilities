# Vertx.Utilities

General utilities for Unity.  

> **Warning**  
> Unity **2020.1+** (lower versions may be supported, but will miss features).

## Table Of Contents

- [Runtime](#Runtime)
    - [InstancePool](#InstancePool)
    - [EnumToValue](#EnumToValue)
    - [PooledListView](#PooledListView)
    - [ProportionalValues](#ProportionalValues)
    - [Bounds2D](#Bounds2D)
    - [NullableBounds](#NullableBounds)
    - [Extensions](#Extensions)
- [Editor](#Editor)
    - [EditorUtils](#EditorUtils)
    - [EditorGUIUtils](#EditorGUIUtils)
    - [AdvancedDropdownUtils](#AdvancedDropdownUtils)

# Runtime

## InstancePool

**Simple static class for managing object pools.**

#### Pooling and Unpooling

```cs
// Retrieve or instance an object from the pool
MyComponent instance = InstancePool.Get(Prefab);

// Return an instance to the pool.
// The instance is moved to the Instance Pool scene and deactivated.
InstancePool.Pool(Prefab, instance);
```

#### Capacity and TrimExcess

`TrimExcess` will reduce the size of the pool (by deleting pooled instances) to the limits set by `SetCapacity`.  
This is helpful when reloading a game/scene to ensure the pool never gets out of hand in the long term.

```cs
// Sets the pool to only keep 30 instances of a specific prefab when TrimExcess is called.
InstancePool.SetCapacity(Prefab, 30);
// Sets the pool to only keep 30 instances of all pooled PoolObject prefabs when TrimExcess is called.
InstancePool.SetCapacities<PoolObject>(30);

// Trims all instances in every pool down to their capacities (20 is the default argument)
InstancePool.TrimExcess();
// Trims instances in the PoolObject pool down to their capacities (20 is the default argument)
InstancePool.TrimExcess<PoolObject>();
// Trims instances from a specific prefab down to its capacity (20 is the default argument)
InstancePool.TrimExcess(Prefab);
```

#### Warmup

```cs
// Instances and immediately pools 30 instances of prefab spread over 30 frames.
StartCoroutine(InstancePool.WarmupCoroutine(Prefab, 30));

// Instances and immediately pools 30 instances of prefab.
InstancePool.Warmup(Prefab, 30);
```

#### Cleanup
It's advised to avoid retaining references from pooled objects or prefabs to other objects in the scene. Clear these references before an object is pooled, then call `TrimExcess` instead to bring these allocated objects down to a predictable level.  
When this cannot be achieved, because we're dealing with static pools you may want to ensure that they're completely unloaded at a safe point.  
Having references from pooled objects retained in a pool is memory leak unless those references are reset in rotation, so the following methods allow you to clean up.

```cs
// Remove a pool from the system, and optionally handle what happens to the currently pooled instances.
InstancePool.RemovePool(Prefab, instance => Destroy(instance.gameObject));
// Remove all pools of a component type from the system, and optionally handle what happens to the currently pooled instances.
InstancePool.RemovePools<MyComponent>(instance => Destroy(instance.gameObject));
// Remove *all pools*, and optionally handle what happens to the currently pooled instances. This can be done to resolve all possible memory leaks.
// You may also choose to unload the instance pool scene, but there is no utility method for doing this.
InstancePool.RemovePools(instance => Destroy(instance.gameObject));
```

#### Variants
You can use different types of pools with the `Override` method. Custom pools can also be created by inheriting from `IComponentPool<T>`.  
Supported pool types are:  

| Type                      | Description                                                                                                                                                                                                                                                                                                                                                                               |
|---------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ExpandablePool`          | A pool that will expand to contain all instances pooled into it.<br/>Only instances that are freed to the pool can be returned by `Get`.                                                                                                                                                                                                                                                  |
| `ExpandablePoolUnchecked` | `ExpandablePool`, but there are no safety checks performed in high-frequency operations.<br/>If abused, you can enter instances into the pool multiple times or destroy objects that are in the pool, which will result in logic errors and unhandled exceptions.                                                                                                                         |
| `CircularPool`            | 	A fixed-capacity pool that uses the least recent entry when `Get` is called.<br/>Instances are always considered a part of the pool, and can be requested at any moment.<br/>The pool will grow to the internal size unless warmed up beforehand.<br/>This pool use useful in circumstances where there's a fixed count of an unimportant resource, like bullet hole decals for example. |


## EnumToValue

**Data type for associating serialized data with an enum.**  
> **Note**  
> This data is kept updated via the Inspector. To maintain parity it is important to inspect these fields when updating the associated enums.

#### Example Types

```cs
public enum Shape
{
    Box,
    Sphere,
    Capsule,
    Cylinder
}

[System.Serializable]
public class ShapeElement
{
    public string Name;
    public Material Material;
    public Mesh Mesh;
}
```

#### Field Declaration

You can optionally add the `[HideFirstEnumValue]` attribute to hide the None/0 value if it is irrelevant.

```cs
[SerializeField]
private EnumToValue<Shape, ShapeElement> _data;
```

#### Usage Example

```cs
[SerializeField] private Shape _shape;

private void Start()
{
    GameObject g = gameObject;
    if (!g.TryGetComponent(out MeshRenderer meshRenderer))
        meshRenderer = g.AddComponent<MeshRenderer>();
    if (!g.TryGetComponent(out MeshFilter meshFilter))
        meshFilter = g.AddComponent<MeshFilter>();
    
    //Configure target components with our data
    var value = _data[_shape];
    g.name = value.Name;
    meshRenderer.sharedMaterial = value.Material;
    meshFilter.sharedMesh = value.Mesh;
}
```

![EnumToValue](http://vertx.xyz/Images/Utilities/EnumToValue_01.png)

#### EnumDataDescription

A ScriptableObject containing an `EnumToValue` structure for data reuse.

```cs
public class DataDescriptionExample : EnumDataDescription<EnumToValue<Shape, ShapeElement>> { }
```

## PooledListView

A vertical ScrollView that contains fixed-height elements that are pooled.  
Navigation is automatically set within the list's contents.

```cs
// Binds a list to the ListView
public void Bind(IList elements)
// A UnityEvent to bind UI items with list item content
// int index, RectTransform root
public BindEvent BindItem
// Call to regenerate content when the bound list changes
public void Refresh()
```

## ProportionalValues

**Helper class for managing multiple values so they always total the same amount.**  
The example below will keep all child Sliders the component in sync proportionally.

```cs
private void Start()
{
    var sliders = GetComponentsInChildren<Slider>();
    
    var proportionalValues = new ProportionalValues(sliders.Length);
    for (int i = 0; i < proportionalValues.Length; i++)
    {
        int iLocal = i;
        // Initialise the sliders to their calculated values.
        sliders[i].SetValueWithoutNotify(proportionalValues[i]);
        // When a slider changes, set the associated proportional value (this will force a recalculation)
        sliders[i].onValueChanged.AddListener(v => proportionalValues[iLocal] = v);
    }
    // When the proportional value changes, inform the sliders.
    proportionalValues.OnValueChanged += (index, v) => sliders[index].SetValueWithoutNotify(v);
}
```

## Bounds2D
`Bounds2D`, (and `NullableBounds2D`) is a parallel to `Bounds` that implements similar methods.  
`Rect` is commonly used, but it's not built for purpose.

## NullableBounds
`Bounds`, but `Encapsulate` doesn't expand from the default `(0,0,0)` when first called.  
If a value has not yet been assigned methods like `Intersects`, `IntersectRay`, `Contains`, will return false.  
Other query methods have `TryGet` alternatives.  

## Extensions
| Name                                                        | Description                                                                                                                                           |
|-------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IList<T>.RemoveUnordered`<br/>`IList<T>.RemoveUnorderedAt` | Removes an item from a list without caring about maintaining order.<br/>(Moves the last element into the hole and removes it from the end)            |
| `UnityEngine.Object.TrimName`                               | Trims appended text from Object names. Will trim `(Clone)` by default.<br/>This is not recursive, and only will remove a single instance of the word. |

## Utils
| Name         | Description                                                 |
|--------------|-------------------------------------------------------------|
| `EditorOnly` | A call to a lambda that will be stripped in builds.         |
| `DebugOnly`  | A call to a lambda that will be stripped in release builds. |

# Editor
## EditorUtils

Many helper functions for random editor functionality I use, often or otherwise.

| Name                                                     | Description                                                                                                                                                                |
|----------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Assets**                                               |
| `LoadAssetOfType`<br/>`LoadAssetsOfType`                 | Loads assets matching a type with an optional name query.                                                                                                                  |
| **Project Browser**                                      |
| `GetProjectBrowserWindow`                                | Gets an instance of the project browser EditorWindow.                                                                                                                      |
| `ShowFolderContents`                                     | Reveals the contents of a folder in the project browser.                                                                                                                   |
| `GetCurrentlyFocusedProjectFolder`                       | Returns the currently focused folder in the project browser.                                                                                                               |
| `SetProjectBrowserSearch`                                | Sets the value of the project browser search.                                                                                                                              |
| **Editor Extensions**                                    |
| `GetEditorExtensionsOfType`                              | Returns lists of containing newly instanced classes that inherit from the provided type.                                                                                   |
| **Scene**                                                |
| `BuildSceneScope`                                        | A scope that can be enumerated that will iterate over the scenes in the Build Settings and reset when disposed. The scope also provides a method to draw a progress bar.   |
| `GetAllComponentsInScene`<br/>`GetAllGameObjectsInScene` | Recursively iterates all Components/GameObjects.                                                                                                                           |
| `GetGameObjectsIncludingRoot`                            | Recursively iterates a Transform hierarchy.                                                                                                                                |
| **Logging**                                              |
| `GetPathForObject`                                       | Returns an appropriate full path to the object. This includes the scene if relevant.                                                                                       |
| **Serialized Properties**                                |
| `FindBackingProperty`<br/>`FindBackingPropertyRelative`  | Finds a backing property by name. Ie. a field serialized with `[field: SerializeField]` or `[field: SerializeReference]`.                                                  |
| `GetIndexFromArrayProperty`                              | Gets the array index associated with a property via string manipulation.                                                                                                   |
| `LogAllProperties`                                       | Logs all properties on a SerializedObject.                                                                                                                                 |
| `GetPropertyValue`                                       | Retrieves most basic System.Object values associated with the property via the property itself.                                                                            |
| `ToFullString`                                           | Attempts to log the value in the serialized property.                                                                                                                      |
| `SimpleCopyTo`                                           | Simple automatic copy for most basic serialized property types.                                                                                                            |
| `SimpleCopyInto`                                         | Copy an object of the same type into most basic serialized property types. Handles collections, but not generic objects. Combine with GetFields to get the most out of it. |
| `Reverse`                                                | Reverses serialized property arrays.                                                                                                                                       |

## EditorGUIUtils

Many helper functions for random editor IMGUI functionality I use.

#### Controls and Decorations

| Name                        | Description                                                                                                                                                                                        |
|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Exponential Slider`        | A slider that represents its values exponentially. Useful for things like camera FoV.                                                                                                              |
| `DrawSplitter`              | Draws a horizontal rule.                                                                                                                                                                           |
| `ButtonOverPreviousControl` | Uses GUILayout.GetLastRect to draw a button over the last drawn control.                                                                                                                           |
| `OutlineScope`              | Wraps a group of controls to surround them in an outline/background.<br/>Similar in style to ReorderableList, so can be combined if drawn before a list with its header set to `headerHeight = 0`. |
| `DrawOutline`               | Manually draw an outline. Can be useful to surround a SerializedProperty in a PropertyDrawer.                                                                                                      |
| `ContainerScope`            | Wraps a group of controls to surround them in an background. Also draws a header for the group.                                                                                                    |

#### Helpers

| Name                                         | Description                                                                |
|----------------------------------------------|----------------------------------------------------------------------------|
| `HeightWithSpacing`                          | singleLineHeight + standardVerticalSpacing                                 |
| `rect.NextGUIRect()`                         | Advances the rect to the next position with a standardVerticalSpacing gap. |
| `rect.Indent()`<br/>`rect.GetIndentedRect()` | EditorGUI.IndentedRect alternatives.                                       |
| `EditorGUIUtils.ZeroIndentScope`             | Temporarily resets EditorGUI.indentLevel.                                  |

#### ReorderableList

| Name                               | Description                                                                                                                                 |
|------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| `ReorderableListAddButton`         | Draws an alternate add button if run after a reorderable list's DoLayoutList function. Set `displayAdd = false` when initialising the list. |
| `ReorderableListHeaderClearButton` | Draws a Clear button in the top right of a ReorderableList's header when run in the header callback.                                        |

#### Styles

| Name                                            | Description                                                         |
|-------------------------------------------------|---------------------------------------------------------------------|
| `CenteredMiniLabel`<br/>`CenteredBoldMiniLabel` | Variations of `EditorStyles.miniLabel`/`EditorStyles.miniBoldLabel` |
| `CenteredBoldLabel`                             | Variation of `EditorStyles.boldLabel`                               |

## AdvancedDropdownUtils

**Helper class for authoring AdvancedDropdown menus.**  
`IPropertyDropdownItem` can be implemented to provide names and paths (`"Folder/Sub Folder"`).  
`CreateAdvancedDropdownFromAttribute` can generate an AdvancedDropdown from all types that implement an inherited `AdvancedDropdownAttribute`.  
For dropdowns created from a type inheritance structure consider using `AdvancedDropdownOfSubtypes`.  

---
If you find this resource helpful:

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z42ZYHB)

## Installation

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.vertx.utilities

To add it the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.vertx
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.vertx.utilities`
- click <kbd>Add</kbd>

</details>

<details>
<summary>Add from GitHub | <em>not recommended, no updates through UPM</em></summary>

Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/vertxxyz/Vertx.Utilities.git`
- click <kbd>Add</kbd>  
  **or**
- Edit your `manifest.json` file to contain `"com.vertx.utilities": "https://github.com/vertxxyz/Vertx.Utilities.git"`,

To update the package with new changes, remove the lock from the `packages-lock.json` file.
</details>