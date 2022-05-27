# ManulECS

An Entity-Component-System and a simple shared resource manager for C#, inspired by minECS, specs and Entt.

I wrote a library called ManulEC in 2019 for my roguelike game to provide simple runtime composition similar to Unity, but it had obvious flaws and performance issues. Thus, a need for ManulECS had arisen and I decided to write a new library from scratch, using structs and sparse sets.

## Features and technical details

### Focus on simplicity

Ultimately, ManulECS is just a library and not a framework. It is assumed that ECS is just a part of whatever larger architecture you're using. ManulECS doesn't care about how to build your systems or model your program states. It provides just the core functionality for composition of entities, iterating them and serializing the whole mess to JSON and back.

I've tried to keep ManulECS as light and simple as possible, while still managing competetive single-thread performance with other libraries of the same sort. There are bound to be more feature-rich and more performant ECS implementations out there, but mine does its job in less than 900 lines of code, comments, blanks and tests notwithstanding.

### Sparse sets of components

Under the hood, ManulECS uses sparse sets of structs to achieve data locality. The main building blocks we need to care about, are as follows:

- **Entity**, a simple 4-byte value that is used to index components
- **Component**, a regular data-holding struct
- **Tag**, a boolean flag, which don't contain data
- **Resource**, singleton class that exists outside the sphere of entities

A collection of entities having a certain configuration of Components and Tags is called a View, which is pretty much just a read-only Span of entities for us to index components with. 

### Serialization

There's a JSON serializer included in the project, there's also support for custom serializers, although the process is a bit involved.

### Limitations, known issues

Entity ids are limited by an unsigned 3-byte value, meaning the maximum number of entities is 16777215.

Maximum component/tag limit is controlled by the `MAX_SIZE` constant in `ManulECS\Key.cs`. With the default value of 4, we can have up to 128 (4*32) components or tags. ManulECS works fine with any values, but larger values will come with a performance cost.

## Overview

### World

The ManulECS entity registry is called a `World` and we can have multiple worlds, should we want to.

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

There are two kinds of things assignable to entities, Components and Tags. They are specified by using marker interfaces `Component` and `Tag`. These interfaces are used only for method constraints to give some useful static typing, so there's no unnecessary boxing of structs happening. Component pools are setup automatically on first use, so there's no need to declare them beforehand.

Components are simple data structs.

```csharp
public struct Pos : Component {
  public int x;
  public int y;
}
```

Tags don't contain any data. They're like a typed boolean flag related to an entity.

```csharp
public struct IsPlayer : Tag { }
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
world.Remove<SomeComponent>(entity); // Remove a component from a single entity
world.Remove<SomeTag>(entity);       // Remove a tag from a single entity
world.Clear<SomeComponent>();        // Clear all components of type
world.Clear<SomeTag>();              // Clear all tags of type
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

### Views

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
public struct IntentionMove : Component { }  // This component is never serialized

[ECSSerialize(Omit.Entity)]
public struct VisualEffect : Component { }   // The entire entity owning this component is never serialized
```

ManulECS also features a concept of serialization profiles, which we declare on component and resource basis. Always affects the entire entity.

```csharp
// The entity will only be serialized when no profile has been provided
public struct Monster : Component { }

// The entity will only be serialized when "global" profile has been provided
[ECSSerialize("global")]   
public struct Player : Component { }
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
serializer.Read(stream, world);
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

We start reaching the multi-ms range at around creating/removing 100000 entities per frame, which on its own seems like a bit extreme use case. Tags do still have some memory overhead, albeit less than components.

|                        Method |      N |     Mean |     Error |    StdDev | Allocated |
|------------------------------ |------- |---------:|----------:|----------:|----------:|
|                CreateEntities | 100000 | 1.673 ms | 0.0277 ms | 0.0259 ms |      4 MB |
|  CreateEntitiesWith1Component | 100000 | 3.656 ms | 0.0709 ms | 0.0663 ms |      6 MB |
| CreateEntitiesWith2Components | 100000 | 5.807 ms | 0.0270 ms | 0.0240 ms |      9 MB |
|        CreateEntitiesWith1Tag | 100000 | 3.361 ms | 0.0233 ms | 0.0195 ms |      6 MB |
|       CreateEntitiesWith2Tags | 100000 | 5.401 ms | 0.0367 ms | 0.0343 ms |      7 MB |

Creating tags is a tiny bit faster than creating components, but they're about the same when removing. Fastest way to get rid of components, is to remove the entire entity holding them.

|                        Method |      N |       Mean |    Error |   StdDev |
|------------------------------ |------- |-----------:|---------:|---------:|
|                RemoveEntities | 100000 |   463.6 μs |  2.30 μs |  1.92 μs |
|  Remove1ComponentFromEntities | 100000 | 1,389.5 μs |  4.31 μs |  3.60 μs |
| Remove2ComponentsFromEntities | 100000 | 2,721.2 μs | 21.35 μs | 19.97 μs |
|        Remove1TagFromEntities | 100000 | 1,309.7 μs |  4.29 μs |  4.02 μs |
|        Remove2TagFromEntities | 100000 | 2,671.5 μs |  5.31 μs |  4.71 μs |

### Iterating views

Benchmarks perform a simple add operation for each component.

The worst case scenario benchmark rebuilds the entity view on each iteration. There is currently no sorting mechanism, so iteration isn't as cache-friendly as I'd like, but it's _fast enough_ even with random access, with the added bonus that we can use simple foreach loops instead of complex lambdas (no need to worry about closures) or creating system classes.

Most of the time is spent on retrieving pools and doing actual operations on data. Just looping through components and doing nothing with them has similar performance as with looping tags.

|                              Method |      N |     Mean |   Error |  StdDev |
|------------------------------------ |------- |---------:|--------:|--------:|
|                    Update1Component | 100000 | 104.6 μs | 0.62 μs | 0.55 μs |
|                   Update2Components | 100000 | 181.9 μs | 0.56 μs | 0.52 μs |
| Update2Components_WorstCaseScenario | 100000 | 585.1 μs | 1.89 μs | 1.57 μs |
|                   Update3Components | 100000 | 329.7 μs | 1.22 μs | 1.08 μs |

For tags, there's virtually no difference between looping through 1 or 2 tags.

|    Method |      N |     Mean |    Error |   StdDev |   Median |
|---------- |------- |---------:|---------:|---------:|---------:|
|  Loop1Tag | 100000 | 33.08 μs | 0.859 μs | 2.520 μs | 32.41 μs |
| Loop2Tags | 100000 | 32.99 μs | 0.770 μs | 2.246 μs | 32.29 μs |
 
### Serialization

The benchmark serializes/deserializes a world containing 100000 entities, each with two Components and one Tag. Serialization benchmarks use MemoryStreams, so results are most likely different when writing to an actual filesystem. Serialization also relies heavily on caching, so first runs might will take longer than the subsequent ones, especially if deserializing lots of components that haven't been registered yet by the application. 

Nevertheless, it's advisable to not serialize/deserialize World data too much in the middle of the tightest gameplay loops.

|      Method |      N |      Mean |    Error |   StdDev |     Gen 0 | Allocated |
|------------ |------- |----------:|---------:|---------:|----------:|----------:|
|   Serialize | 100000 |  83.35 ms | 0.679 ms | 0.635 ms | 3000.0000 |     33 MB |
| Deserialize | 100000 | 187.71 ms | 0.865 ms | 0.809 ms | 5000.0000 |     41 MB |

