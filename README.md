# Vertx.Utilities
General Utilities for Unity

## Runtime
### InstancePool
Simple static class for managing object pools.  
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
InstancePool.SetCapacities(30);

// Trims all instances in every pool down to their capacities (20 is the default argument)
InstancePool.TrimExcess();
// Trims instances in the PoolObject pool down to their capacities (20 is the default argument)
InstancePool.TrimExcess();
// 
```

#### Warmup
```cs
// Instances and immediately pools 30 instances of prefab spread over 30 frames.
StartCoroutine(InstancePool.WarmupCoroutine(prefab, 30));

// Instances and immediately pools 30 instances of prefab.
InstancePool.WarmupCoroutine(prefab, 30);
```

---
### EnumToValue
```cs
[SerializeField]
private EnumToValue<Shape, ShapeElement> data;
		
// Pre-Unity 2020 the above has to be a "solid" generic type, so you must serialize a derived class that does not use generics.
[System.Serializable]
private class Data : EnumToValue<Shape, ShapeElement> { }

[SerializeField]
private Data data;
```