# ManulECS

An Entity-Component-System and a simple shared resource manager for C#, inspired by DefaultEcs, minECS, specs and Entt.

I wrote a library called ManulEC in 2019 for my roguelike game to provide simple runtime composition similar to Unity, but it had obvious flaws and performance issues. Thus, a need for ManulECS had arisen and I decided to write a new library from scratch, using structs and sparse sets.

## Features

### Focus on simplicity

Simplicity as in simple to understand, this has been a huge learning opportunity for myself and I've purposefully tried to keep things as clean as possible. There are bound to be more feature-rich and more performant ECS implementations out there, but my implementation does its job in less than 1000 lines of code, comments and tests notwithstanding.

Out-of-the-box, there are no system classes to override, no code generation, no events, no parallelism. Just the core functionality for composition, iteration and serialization. If you want more exotic features, you can write your own wrappers around ManulECS.

### Sparse Sets

Under the hood, ManulECS uses sparse sets of structs to achieve data locality.

### Components, Tags and Resources

There's support for **Components** (regular structs), **Tags** (boolean flags) and **Resources** (classes outside the sphere of entities). 

### Serialization

There's support for custom serializers, although the process is a bit involved. A serializer for JSON format has been included in the project.

## Overview

### World

The ManulECS entity registry is called a `World`. It is self-contained, and we can have multiple worlds with different sets of components.

```csharp
var world = new World(); // Create a new entity registry.
```

### Entity

Entities are just simple 4-byte value types, representing an internal id and a version number. 

```csharp
var entity = world.Create(); // Create a new entity
world.Remove(entity);        // Remove the entity, clearing all components in the process.
```

### Components and Tags

There are two kinds of things assignable to entities, Components and Tags. They are specified by using marker interfaces `IComponent` and `ITag`. These interfaces are used only for method constraints to give some useful static typing, so there's no unnecessary boxing of structs happening. Component pools are setup automatically on first use, so there's no need to declare them beforehand.

Components are simple data structs.

```csharp
public struct Pos : IComponent {
  public int x;
  public int y;
}
```

Tags don't contain any data. They're like a typed boolean flag related to an entity.

```csharp
public struct IsPlayer : ITag { }
```

Easiest way to assign new components to entities is to use field initializers. `Assign` will only assign component if not already found on the entity, `Patch` will replace the existing component.

```csharp
world.Assign(entity, new Pos { x = 0, y = 0 }); // Will not overwrite
world.Patch(entity, new Pos { x = 1, y = 1 });  // Will overwrite
```

Tags have no replace function, as there's nothing to replace. Otherwise usage is similar, albeit with the `Tag` method.

```csharp
world.Tag<IsPlayer>(entity);
```

We can remove components one by one, or clear all components of type T from all entities. Components and Tags are removed/cleared the same way.

```csharp
world.Remove<Component>(entity);  // Remove a component from a single entity
world.Remove<Tag>(entity);        // Remove a tag from a single entity
world.Clear<Component>();         // Clear all components of type
world.Clear<Tag>();               // Clear all tags of type
```

### Entity handles

We can also create an entity handle to easily add multiple components on an entity. Entity handles can implicitly convert to entities.

```csharp
var entityHandle = world.Handle()
  .Assign(new Component1 { })
  .Tag<SomeTag>()
  .Patch(new Component2 { });

// This works as well, because EntityHandle implicitly converts to an Entity.
world.Remove(entityHandle);
```

We can also wrap existing entities with a handle.

```csharp
world.Handle(entity)
  .Assign(new Component1 { })
  .Patch(new Component2 { })
  .Remove<SomeTag>();
```

### Resources

Resources are classes that exist outside the sphere of entities. Resources are serialized just like entities, making them a good choice for complex data that needs to be persisted in a save game.

Resources are natural singletons. Examples of objects that make good candidates for resources could be the current level in a game, or a clock, that controls whether it's day or night in the game.

```csharp
var level = CreateLevel();
world.SetResource(level);
```

```csharp
var level = world.GetResource<Level>();
```

### Systems

ManulECS is not opinionated on how to build systems. Instead, ManulECS provides a View of Entities that we can iterate through with a foreach loop. Views are automatically updated on iteration if the related component pool has been modified.

We can use Pools<T...> method to improve performance by providing reusable component pool to read from.

```csharp
public static void MoveSystem(World world) {
  var (positions, velocities) = world.Pools<Position, Velocity>();
  foreach (var e in world.View<Position, Velocity>()) {
    ref var pos = ref positions[e];
    var vel = velocities[e];

    pos.coords += vel.transform;
  }
}
```

Tags are handled by views as well! We can use Tags to easily filter out results.

```csharp
foreach (var e in world.View<Position, Velocity, SomeInterestingTag>()) { }
```

### Serialization

We can control what components should be serialized by attributes. This is opt-out, by default everything is included. This is handy for omitting non-gameplay entities from save games, like particle effects.

```csharp
[ECSSerialize(Omit.Component)]
public struct IntentionMove : IComponent { }  // This component is never serialized

[ECSSerialize(Omit.Entity)]
public struct VisualEffect : IComponent { }   // The entire entity owning this component is never serialized
```

ManulECS also features a concept of serialization profiles, which we declare on component and resource basis. Always affects the entire entity.

```csharp
// The entity will only be serialized when no profile has been provided
public struct Monster : IComponent { }

// The entity will only be serialized when "global" profile has been provided
[ECSSerialize("global")]   
public struct Player : IComponent { }
```

We can use serialization profiles on resources as well. Note that Omit does nothing when used on resources.

```csharp
[ECSSerialize("level")]
public class Level { }
```

`WorldSerializer` abstract class exposes two public methods `Write` and `Read`, which can be used to serialize and deserialize world state accordingly. 

The project includes a derived `JsonWorldSerializer` class, which can be used to serialize/deserialize the World state to/from JSON format. JSON serialization uses `System.Text.Json` under the hood, so there are no external dependencies. 

It's a good idea to reuse serializer instances, as it caches some of the information needed to properly convert components. This way caches won't need to be built again from scratch on every write/read.

When not providing a serialization profile string, all resources and components that don't belong to any profiles will get serialized.

```csharp
// Default serialization
var serializer = new JsonWorldSerializer();
serializer.Write(stream, world);
```

Alternatively, we can provide a serialization profile, which will serialize only matching entities and resources.

```csharp
// Serialize only stuff set with the "global" profile
serializer.Write(stream, world, "global");
```

`serializer.Read` works incrementally, so we can combine multiple sets of saved data in a single world. An example use-case would be backtracking, where we want to combine global data with some level-specific data on level transitions 

```csharp
  var serializer = new JsonWorldSerializer(); 
  serializer.Read(stream, world, "global");
  ...
  serializer.Read(someOtherStream, world);
```

For the included `JsonWorldSerializer`, I've created a couple extra methods for easier handling of JSON strings.

```csharp
var json = serializer.Serialize(world, "global");
File.WriteAllText("global.json", json);
```

```csharp
var json = File.ReadAllText("global.json");
serializer.Deserialize(world, json);
```

There's a small limitation in deserialization - all components and resources need to belong to the same assembly. By default, the deserialization process looks for possible types to deserialize to in the entry assembly, but we can also customize the used assembly name.

```csharp
var serializer = new JsonWorldSerializer() { AssemblyName = "SomeOtherAssembly" };
```

The resulting JSON would look something like this:

```json
{
  "Entities": {
    "0": {
      "SomeNamespace.SomeComponent": {
        "someField": "someValue"
      }
    }
  },
  "Resources": {
    "SomeNamespace.SomeResource": {
      "someData": "someValue"
    }
  }
}
```

Components can belong to different namespaces though. By default components and resources are serialized by their full names, but we can omit namespaces from serialization by providing one explicitly. This alone can drastically decrease uncompressed filesizes.

```csharp
var serializer = new JsonWorldSerializer() { Namespace = "SomeNamespace" };
```

When providing namespaces, the resulting JSON would look something like this instead:

```json
{
  "Entities": {
    "0": {
      "SomeComponent": {
        "someField": "someValue"
      }
    }
  },
  "Resources": {
    "SomeResource": {
      "someData": "someValue"
    }
  }
}
```

On deserialize, the types would then be automatically constructed as `SomeNamespace.SomeComponent` and `SomeNamespace.SomeResource`.

## Benchmarks

I've included my testing benchmarks in ManulECS.Benchmark project, using the BenchmarkDotNet library.

Benchmarks were run on:
> .NET 6.0.4 on Windows 10
> Intel Core i5-8600K CPU 3.60GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores
> 16 GB RAM

### Creation and removal

We start reaching the multi-ms range at creating/removing 100000 entities per frame, which on its own seems like a bit extreme use case. Tags do still have some overhead, albeit less than components.

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

### Iterating views

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
 
### Serialization

The benchmark serializes/deserializes a world containing 100000 entities, each with two Components and one Tag. Serialization benchmarks use MemoryStreams, so results are most likely different when writing to an actual filesystem. Serialization also relies heavily on caching, so first runs might take longer than the subsequent ones, especially if deserializing lots of components that haven't been registered yet. 

Nevertheless, it's advisable to not serialize/deserialize World data in the middle of the tightest gameplay loops. It's still fast *enough* for things like autosaves, dumping state on disk between level transitions etc. For comparisons, according to the profiler, my own (relatively complex) roguelike game takes on average about 500ms to deserialize and 300ms to serialize.

|      Method |      N |     Mean |   Error |  StdDev |     Gen 0 | Allocated |
|------------ |------- |---------:|--------:|--------:|----------:|----------:|
|   Serialize | 100000 | 107.1 ms | 0.56 ms | 0.50 ms | 3000.0000 |     25 MB |
| Deserialize | 100000 | 145.6 ms | 1.45 ms | 1.35 ms | 4000.0000 |     35 MB |

