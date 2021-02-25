# Vertx.Utilities

General Utilities for Unity

## Table Of Contents

- [Runtime](#Runtime)
    - [InstancePool](#InstancePool)
    - [EnumToValue](#EnumToValue)
    - [PooledListView](#PooledListView)
    - [ProportionalValues](#ProportionalValues)
    - [Misc](#Misc)
- [Editor](#Editor)
    - [AssetInstance](#AssetInstance)
    - [EditorUtils](#EditorUtils)
    - [EditorGUIUtils](#EditorGUIUtils)
    - [AdvancedDropdownUtils](#AdvancedDropdownUtils)

# Runtime

## InstancePool

**Simple static class for managing object pools.**

#### Pooling and Unpooling

```cs
// Retrieve or instance an object from the pool
MyComponent instance = InstancePool.Get(prefab);

// Return an instance to the pool.
// The instance is moved to the Instance Pool scene and deactivated.
InstancePool.Pool(prefab, instance);
```

#### Capacity and TrimExcess

`TrimExcess` will reduce the size of the pool (by deleting pooled instances) to the limits set by `SetCapacity`.  
This is helpful when reloading a game/scene to ensure the pool never gets out of hand in the long term.

```cs
// Sets the pool to only keep 30 instances of a specific prefab when TrimExcess is called.
InstancePool.SetCapacity(prefab, 30);
// Sets the pool to only keep 30 instances of all pooled PoolObject prefabs when TrimExcess is called.
InstancePool.SetCapacities<PoolObject>(30);

// Trims all instances in every pool down to their capacities (20 is the default argument)
InstancePool.TrimExcess();
// Trims instances in the PoolObject pool down to their capacities (20 is the default argument)
InstancePool.TrimExcess<PoolObject>();
// Trims instances from a specific prefab down to its capacity (20 is the default argument)
InstancePool.TrimExcess(prefab);
```

#### Warmup

```cs
// Instances and immediately pools 30 instances of prefab spread over 30 frames.
StartCoroutine(InstancePool.WarmupCoroutine(prefab, 30));

// Instances and immediately pools 30 instances of prefab.
InstancePool.WarmupCoroutine(prefab, 30);
```

## EnumToValue

**Data type for associating serialized data with an enum.**  
This data is kept updated via the Inspector so to maintain parity it is important to inspect these fields when updating the associated enums.

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

⚠️ `EnumToValue` only supports enums with consecutive values, if you are using enums with gaps then `EnumToValueDictionary` handles that with minor additional overhead. ⚠️  
You can optionally add the `[HideFirstEnumValue]` attribute to hide the None/0 enum if it is irrelevant.

```cs
[SerializeField]
private EnumToValue<Shape, ShapeElement> data;


// Pre-Unity 2020 the above has to be a "solid" generic type, so you must serialize a derived class that does not use generics.
[System.Serializable]
private class Data : EnumToValue<Shape, ShapeElement> { }

[SerializeField]
private Data data;
```

#### Usage Example

```cs
[SerializeField] private Shape shape;

private void Start()
{
    GameObject g = gameObject;
    if (!g.TryGetComponent(out MeshRenderer meshRenderer))
        meshRenderer = g.AddComponent<MeshRenderer>();
    if (!g.TryGetComponent(out MeshFilter meshFilter))
        meshFilter = g.AddComponent<MeshFilter>();
    
    //Configure target components with our data
    var value = data[shape];
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

// Pre-Unity 2020 the above has to be a "solid" generic type, so you must serialize a derived class that does not use generics.
public class DataDescriptionExample : EnumDataDescription<DataDescriptionExample.ShapeData>
{ 
    [System.Serializable]
    public class ShapeData : EnumToValue<Shape, ShapeElement> { }
}
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

## Misc

#### TrimName

Trims appended text from Object names. Will trim `(Clone)` by default.  
This is not recursive, and only will remove a single instance of the word.

```cs
object.TrimName();
```

#### Rotation
Helper methods for basic axis-angle rotation operations.
```cs
//Rotates a Quaternion in-place via an angle-axis rotation.
rotation.RotateRef(angle, axisDir, Space.Self);
//Returns a Quaternion rotated by a angle-axis rotation.
transform.rotation = rotation.Rotate(angle, axisDir, Space.Self);
```

# Editor

## AssetInstance

A Generic class for creating singleton ScriptableObject assets. Helpful for settings and build scripts.  
An AssetInstance will instance itself at its `ResourcesLocation` when `.Instance` is called if it has not previously been instanced. You can override its name with the `NicifiedTypeName` property.

## EditorUtils

Many helper functions for random editor functionality I use, often or otherwise.

- Assets
    - `LoadAssetOfType`
    - `LoadAssetsOfType`  
      Loads assets matching a type with an optional name query.
    - `TryGetGUIDs`
- Folders
    - `ShowFolderContents`
    - `ShowFolder`
    - `GetCurrentlyFocusedProjectFolder`
- Project Browser
    - `SetProjectBrowserSearch`
    - `GetProjectBrowserWindow`
- Editor Extensions
    - `GetEditorExtensionsOfType`  
      Returns lists of containing newly instanced classes that inherit from the provided type.
- Scene
    - `BuildSceneScope`  
      A scope that can be enumerated that will iterate over the scenes in the Build Settings and reset when disposed. The scope also provides a method to draw a progress bar.
    - `GetAllComponentsInScene`
    - `GetAllGameObjectsInScene`  
      Recursively iterates all Components/GameObjects
    - `GetGameObjectsIncludingRoot`  
      Recursively iterates a Transform hierarchy
- Logging
    - `GetPathForObject`  
      Returns an appropriate full path to the object. This includes the scene if relevant.
- Serialized Properties
    - `GetIndexFromArrayProperty`  
      Gets the array index associated with a property via string manipulation.
    - `LogAllProperties`  
      Logs all properties on a SerializedObject
    - `GetObjectFromProperty`
      Retrieves the System.Object value associated with the property via reflection.
    - `serializedProperty.GetPropertyValue()`  
      Retrieves most basic System.Object values associated with the property via the property itself.
    - `serializedProperty.ToFullString()`  
      Attempts to log the value in the serialized property.
    - `SimpleCopyTo`  
      Simple automatic copy for most basic serialized property types.

## EditorGUIUtils

Many helper functions for random editor GUI functionality I use, often or otherwise.

#### Controls and Decorations

- `Exponential Slider`  
  An slider that represents its values exponentially. Useful for things like camera FoV.
- `DrawSplitter`  
  Draws a horizontal rule.
- `ButtonOverPreviousControl`  
  Uses GUILayout.GetLastRect to draw a button over the last drawn control.
- `OutlineScope`  
  Wraps a group of controls to surround them in an outline/background.  
  Similar in style to ReorderableList, so can be combined if drawn before a list with its header set to `headerHeight = 0`.
- `DrawOutline`  
  Manually draw an outline. Can be useful to surround a SerializedProperty in a PropertyDrawer.
- `ContainerScope`  
  Wraps a group of controls to surround them in an background. Also draws a header for the group.

#### Helpers

- `HeightWithSpacing`  
  singleLineHeight + standardVerticalSpacing
- `rect.NextGUIRect()`  
  Advances the rect to the next position with a standardVerticalSpacing gap.
- `rect.Indent()`
- `rect.GetIndentedRect()`  
  EditorGUI.IndentedRect alternatives.  
- `EditorGUIUtils.ZeroIndentScope`  
  Temporarily resets EditorGUI.indentLevel

#### ReorderableList

- `ReorderableListAddButton`  
  Draws an alternate add button if run after a reorderable list's DoLayoutList function. Set `displayAdd = false` when initialising the list.
- `ReorderableListHeaderClearButton`  
  Draws a Clear button in the top right of a ReorderableList's header when run in the header callback.

#### Styles

- `CenteredMiniLabel`
- `CenteredBoldMiniLabel`
- `CenteredBoldLabel`

## AdvancedDropdownUtils

**Helper class for authoring AdvancedDropdown menus.**  
`IPropertyDropdownItem` can be implemented to provide names and paths (`"Folder/Sub Folder"`).  
`CreateAdvancedDropdownFromAttribute` can generate an AdvancedDropdown from all types that implement an inherited `AdvancedDropdownAttribute`.  
`CreateAdvancedDropdownFromType` can generate one from a type inheritance structure.