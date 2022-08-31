# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [4.0.0-pre.1]
### Added
- Added different pooling variants to InstancePool. These variants can be initialised using the InstancePool.Override(...) method.
  You can also write your own pooling variants for use with InstancePool's static structure.
- Added InstancePool.RemovePool(s) methods to cover clearing pools of all levels, and added a callback to handle pooled instances.
- Added InstancePool.DefaultPoolHasSafetyChecks to switch pooling to a performant unchecked variant by default.
- Added UIToolkit support to EnumToValue.
- Added IList<T> extensions: RemoveUnordered and RemoveUnorderedAt.

### Removed
- Removed old serialization for EnumToValue. Please use version 3.1.1 and port EnumToValue data before moving to 4.0.0.
- Removed CodeUtils.
- Removed AssetInstance.
- Removed StyleUtils. Please use default references to serialize StyleSheets into EditorWindows. Or use LoadAssetOfType.

### Changed
- Changed most internal access to an Assembly Definition Reference, bypassing reflection.
- Simplified EnumToValue UI.
- Renamed InstancePool.RemovePrefabPool to RemovePool.
- Renamed FindBackingProperty to FindBackingPropertyRelative (and added FindBackingProperty to SerializedObject)

### Fixed
- Fixed issues with Instance Pool Debugger.

### Known issues

## [3.1.1] - 2022-04-29
- Improved serialization porting for EnumToValueDescription objects made in version 2.

## [3.1.0] - 2022-04-20
- Improved AdvancedDropdownOfSubtypes to better list interfaces and generic types.
- Marked AssetInstance as Obsolete. Please use the built-in ScriptableSingleton type.
- Removed StyleUtils.AddVertxStyleSheets and associated USS.

## [3.0.2] - 2022-03-17
- Renamed RuntimeUtils to RuntimeExtensions.
- Added ConstraintExtensions.
- Added NullableBounds, a Bounds struct where using methods like Encapsulate will not expand from (0,0,0).
- Reduced version requirement to 2019.4, EnumToValue is excluded below 2020.1

## [3.0.1] - 2022-01-17
- Added SerializedProperty.SimpleCopyInto

## [3.0.0] - 2021-10-22
- Upgraded version requirement to 2020.1.
- Added support for 2021.2+
- Removed RotationUtils
- EnumToValue changes:
    - Removed EnumToValueDictionary, EnumToValue now handles all enum configurations
    - Ported data structures to a key value pair configuration. If you have issues with this, revert changes and switch to 2.4.5.
    - Fixed issues with multiple EnumToValues on the same object (or nested).
- Added Icon to IAdvancedDropdownItem
- Added AdvancedDropdownOfSubtypes to replace AdvancedDropdownUtils.CreateAdvancedDropdownFromType

## [2.4.5] - 2021-07-29
- Added SerializedProperty.ReverseArray

## [2.4.4] - 2021-05-01
- Added EditorUtils.GetFieldInfoFromProperty

## [2.4.3] - 2021-04-11
- Added EditorUtils.GetSerializedTypeFromFieldInfo, returns the element type Unity would serialize for a given FieldInfo.

## [2.4.2] - 2021-03-29
- Added a FindBackingProperty method for SerializedProperty
- Added CreateAdvancedDropdownFromAttribute without generics.

## [2.4.1] - 2021-03-28
- Added non-generic AdvancedDropdownUtil.CreateAdvancedDropdownFromType
- Made various AdvancedDropdownUtil functions public
- Fixed issues with EnumToValue additions
- Added SerializedProperty.LogAllProperties

## [2.4.0] - 2020-12-29
- Added AdvancedDropdownUtil additions that allow remapping types to multiple results
- Changed AdvancedDropdownUtil functions to have a validate func parameter that allows for excluding results entirely

## [2.3.7] - 2020-12-03
- Fixed key serialization with EnumToValueDictionary

## [2.3.6] - 2020-12-03
- Fixed BuildSceneScope to function correctly when scenes are not active in Build Settings

## [2.3.5] - 2020-11-30
- Added minimum size to the AdvancedDropdownUtils functions.

## [2.3.4] - 2020-11-30
- Fix for EnumToValueDictionary drawers.
- Fix for PooledListView not properly handling enable and disable with snapping active.

## [2.3.3] - 2020-11-18
- Fixed PooledListView code running in edit-mode (and prefab mode at runtime) that caused start and end padding elements to be duplicated.

## [2.3.2] - 2020-11-17
- Fixed layout issue for EnumToValue when using [HideFirstEnumValue].
- Added a clearer Release function to PooledListView. This is identical to Bind(null).

## [2.3.1] - 2020-11-06
- Fixed startup exceptions with PooledListView.
- Added optional explicit navigation to PooledListView.
- If using Vertx.Editors, that requires an update too.

## [2.3.0] - 2020-11-02
- Added PooledListView.
- Added Instance Pool Debugger.
- Changed Instance Pool to move unpooled objects to the active scene when a parent is not specified.

## [2.2.4] - 2020-10-25
- Added RotationUtils, providing Rotate and RotateRef extensions to Quaternion.
- Added rect.GetNextGUIRect, alternate version of NextGUIRect which returns a new rect.

## [2.2.3] - 2020-10-22
- EnumToValue now supports drawing complex nested serialization, and custom property drawers.

## [2.2.2] - 2020-10-21
- Exposed Instance Pool scene.

## [2.2.1] - 2020-10-20
- Minor drawer improvements for EnumToValue.

## [2.2.0] - 2020-10-14
- Changed InstancePool to instance prefabs with their original position, rotation, and scale if not provided.
- Fixed InstancePool disregarding the provided localScale parameter.
- Added support for Enter Play Mode Options disabling domain reload.

## [2.1.2] - 2020-10-11
- Added InstancePool.IsPooled(prefab, instance)
- Added EditorGUIUtils.ZeroIndentScope for temporarily resetting EditorGUI.indentLevel

## [2.1.1] - 2020-10-10
- Added EditorUtils.SetSceneViewHierarchySearch and GetSceneViewHierarchyWindow.
- Added GetCurrentlyPooledCount to InstancePool.

## [2.1.0] - 2020-10-03
- Moved Project Browser and Folders functions from EditorGUIUtils to EditorUtils.
- SerializedProperty.ToFullString shows full precision for vector types.

## [2.0.0] - 2020-10-03
- InstancePool type can be inferred. InstancePool<MyComponent>.Get(prefab) becomes InstancePool.Get(prefab).
- ProportionalValues now has an OnValueChanged callback and is accessed via indexers and Length.
- PropertyDropdownUtils renamed to AdvancedDropdownUtils and slightly reworked.
- Added AdvancedDropdownAttribute. This can be added to types to generate an AdvancedDropdown with them by calling AdvancedDropdownUtils.CreateAdvancedDropdownFromAttribute.
- Added EditorGUIUtils ReorderableListAddButton and ReorderableListHeaderClearButton.

## [1.1.0] - 2020-10-01
- Added SetCapacity and TrimExcess functions to InstancePool.

## [1.0.4] - 2020-09-24
- InstancePool now uses a HashSet and logs in the editor when items are pooled twice.

## [1.0.3] - 2020-09-21
- Added some logging and copying functionality to SerializedProperty.

## [1.0.2] - 2020-09-18
- Added Object.TrimName to trim (Clone) from instantiated objects.
- Fix for BuildSceneScope to function with disabled scenes.

## [1.0.1] - 2020-09-12
- Added missing EnumToValueDrawer, updated it to support direct generic serialization.

## [1.0.0]
- Initial release.