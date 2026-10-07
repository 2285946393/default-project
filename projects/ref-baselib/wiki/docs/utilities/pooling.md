---
title: Node Pooling
parent: Utilities
---

Slay the Spire 2 uses its `NodePool` class to pool the instantiation of commonly reused models, such as `NCard`. While additional types of node can be registered with it, they are required to be instantiated directly from a scene. If you want to pool a node created through code, you can utilize the methods in the `GeneratedNodePool` class instead. This will still add to the original `NodePool` class.

When using a pool ensure that instances of your class are freed when no longer in use. Generally if your node implements `IPoolable`, you always use `NodePool.Get<NodeType>()` to obtain new instances, and your node is always removed from the scene tree through MegaCrit's extension methods defined in `GodotTreeExtensions` like `FreeSafely` that should be enough. These extension methods return any `IPoolable` removed from the scene tree to the pool.

Stub; requires details.
