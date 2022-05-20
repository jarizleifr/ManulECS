# ManulECS

An Entity-Component-System and a simple shared resource manager for C#, inspired by DefaultEcs, minECS, specs and Entt.

I wrote a library called ManulEC in 2019 for my roguelike game to provide simple runtime composition similar to Unity, but it had obvious flaws and performance issues. Thus, a need for ManulECS had arisen and I decided to write a new library from scratch, using structs and sparse sets.

# Features

- Focus on simplicity
  - ManulECS provides only the core functionality of composition and iteration.
  - If you need system classes, events, parallelism, you'll need to write wrappers for ManulECS yourself.
- Data-driven, cache-coherent ECS, achieved with sparse sets of structs
- Support for tag components without data
- Support for non-entity resources
- Declarative, control internals by providing attributes to components/resources
- Serialization powered by Json.NET

# Overview

## World

The ManulECS entity registry is called a 'World'. It is self-contained, and you can have multiple worlds with different sets of components.

```
var world = new World();  // Create a new entity registry.
```

## Entity

Entity objects

```
var entity = world.Create();  // Create a new entity
world.Remove(entity);         // Remove the entity, clearing all components in the process.
```

## Components and Tags

There are two kinds of things assignable to entities, Components and Tags. They are specified by using marker interfaces `IComponent` and `ITag`. These are only used for method constraints to give some useful static typing, so we're not unnecessarily boxing our structs. Component pools are setup automatically on first use, so there's no need to declare them beforehand.

Components are simple data structs.

```
public struct Pos : IComponent {
  public int x;
  public int y;
}
```

Tags don't contain any data. They're like a typed flag that you can set on an entity.

```
public struct IsPlayer : ITag { }
```

Easiest way to assign new components to entities is to use field initializers. Assign will only assign component if not already found on the entity, Patch will replace the existing component.

```
world.Assign(entity, new Pos { x = 0, y = 0 }); // Will not overwrite
world.Patch(entity, new Pos { x = 1, y = 1 });  // Will overwrite
```

Tags have no replace function, as there's nothing to replace. Otherwise usage is similar, just that the data is omitted.

```
world.Tag<IsPlayer>(entity);
```

You can remove components one by one, or clear all components of type T from all entities. Components and Tags are removed/cleared the same way.

```
world.Remove<Component>(entity);  // Remove a component from a single entity
world.Remove<Tag>(entity);        // Remove a tag from a single entity
world.Clear<Component>();         // Clear all components of type
world.Clear<Tag>();               // Clear all tags of type
```

## Entity handles

You can also create an entity handle to easily add multiple components on an entity. Entity handles can implicitly convert to entities.

```
var entityHandle = world.Handle()
  .Assign(new Component1 { })
  .Tag<SomeTag>()
  .Patch(new Component2 { });

// This works, because EntityHandle implicitly converts to Entity.
world.Remove(entityHandle); 
```

You can also wrap existing entities with a handle.

```
world.Handle(entity)
  .Assign(new Component 1 { })
  .Patch(new Component2 { })
  .Remove<SomeTag>();
```

## Resource

Resources are classes that exist outside the sphere of entities. Resources are serialized just like entities, making them a good choice for complex data that needs to be persisted in a save game.

Resources are natural singletons. Examples of objects that make good candidates for resources could be the current level in a game, or a clock, that controls whether it's day or night in the game.

```
var level = CreateLevel();
world.SetResource(level);
```

```
var level = world.GetResource<Level>();
```

## Systems

ManulECS is not opinionated on how to build systems. Instead, ManulECS provides a View of Entities that you can iterate through with a foreach loop. Views are automatically updated on iteration if the related component pool has been modified.

You can use Pools<T...> method to improve performance by providing reusable component pool to read from.

```
public static void MoveSystem(World world) {
  var (positions, velocities) = world.Pools<Position, Velocity>();
  foreach (var e in world.View<Position, Velocity>()) {
    ref var pos = ref positions[e];
    var vel = velocities[e];

    pos.coords += vel.transform;
  }
}
```

Tags are handled by views as well! You can use Tags to easily filter out results.

```
foreach (var e in world.View<Position, Velocity, SomeInterestingTag>()) {
  ...
}
```

## Serialization

You can control what components should be serialized by attributes. This is opt-out, by default everything is included. This is handy for omitting non-gameplay entities from save games, like particle effects.

```
[ECSSerialize(Omit.Component)]
public struct IntentionMove : IComponent { }  // This component is never serialized

[ECSSerialize(Omit.Entity)]
public struct VisualEffect : IComponent { }   // The entire entity owning this component is never serialized
```

ManulECS also features a concept of serialization profiles, which you declare on component and resource basis. Always affects the entire entity.

```
public struct Monster : IComponent { }  // The entity will only be serialized on 'world.Serialize();'

[ECSSerialize("global")]   
public struct Player : IComponent { }   // The entity will only be serialized on 'world.Serialize("global");'
```

You can use serialization profiles on resources as well. Note that Omit does nothing when used on resources.

```
[ECSSerialize("level")]
public class Level {
  ...
}
```

Serialization uses the excellent Json.NET library. 

`world.Serialize` takes all the entities and resources without profiles set and dumps it as a JSON string, which you can just save to a file.

```
var json = world.Serialize();           // Default serialization
File.WriteAllText("save.json", json);
```

Alternatively, you can provide a serialization profile, which will serialize only matching entities and resources.

```
var json = world.Serialize("global");   // Serialize only stuff set to "global" profile
File.WriteAllText("global.json", json);
```

`world.Deserialize` works incrementally, so you can combine multiple sets of saved data in a single world.

```
world.Deserialize(File.ReadAllText("global.json"));
world.Deserialize(File.ReadAllText("level.json"));
```

# Benchmarks

I've included my testing benchmarks in ManulECS.Benchmark project, using the BenchmarkDotNet library.

Benchmarks were run on Windows 10, .NET 6.0.4 with:
Intel Core i5-8600K CPU 3.60GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores

## Creation and removal

We start reaching the multi-ms range at creating/removing 100000 entities per frame, which on its own seems like a bit extreme use case. We could shave off about 0.500ms by having to declare components beforehand, in which case we could omit some checks, but I find the performance hit to be justified in this case, as it makes code simpler and easier to maintain. 

Tags do still have some overhead, albeit less than components.

|                        Method |      N |     Mean |     Error |    StdDev | Allocated |
|------------------------------ |------- |---------:|----------:|----------:|----------:|
|                CreateEntities | 100000 | 2.000 ms | 0.0359 ms | 0.0336 ms |      4 MB |
|  CreateEntitiesWith1Component | 100000 | 5.216 ms | 0.0560 ms | 0.0523 ms |      7 MB |
| CreateEntitiesWith2Components | 100000 | 8.506 ms | 0.0612 ms | 0.0573 ms |     10 MB |
|        CreateEntitiesWith1Tag | 100000 | 4.259 ms | 0.0258 ms | 0.0215 ms |      6 MB |
|       CreateEntitiesWith2Tags | 100000 | 6.874 ms | 0.0396 ms | 0.0370 ms |      7 MB |

Creating tags is a bit faster than creating components, but they're about the same when removing. Fastest way to get rid of components, is to remove the entire entity holding them.

|                        Method |      N |       Mean |    Error |   StdDev |
|------------------------------ |------- |-----------:|---------:|---------:|
|                RemoveEntities | 100000 |   615.6 μs |  7.40 μs |  6.92 μs |
|  Remove1ComponentFromEntities | 100000 | 1,883.1 μs | 18.33 μs | 17.15 μs |
| Remove2ComponentsFromEntities | 100000 | 3,989.2 μs | 47.58 μs | 42.18 μs |
|        Remove1TagFromEntities | 100000 | 1,973.3 μs | 19.91 μs | 18.62 μs |
|        Remove2TagFromEntities | 100000 | 4,028.0 μs | 42.75 μs | 39.98 μs |

## Iterating views

For components, the update functions are simple move operations with an x/x,y transform applied on x/x,y coordinates.

The worst case scenario benchmark rebuilds the entity view on each iteration. There is currently no sorting mechanism, so iteration isn't as cache-friendly as I'd like, but it's _fast enough_ even with random access, with the added bonus that we can use simple foreach loops instead of complex lambdas (no need to worry about closures) or creating system classes.

My previous benchmark for two component update was 465μs, so I'm pretty happy with the current performance.

|                              Method |      N |     Mean |   Error |  StdDev |
|------------------------------------ |------- |---------:|--------:|--------:|
|                    Update1Component | 100000 | 115.8 μs | 0.89 μs | 0.83 μs |
|                   Update2Components | 100000 | 242.1 μs | 0.87 μs | 0.77 μs |
| Update2Components_WorstCaseScenario | 100000 | 694.2 μs | 5.94 μs | 5.56 μs |

For tags, there's virtually no difference between looping through 1 or 2 tags.

|    Method |      N |     Mean |    Error |   StdDev |
|---------- |------- |---------:|---------:|---------:|
|  Loop1Tag | 100000 | 24.50 μs | 0.081 μs | 0.076 μs |
| Loop2Tags | 100000 | 24.50 μs | 0.063 μs | 0.056 μs |
 
