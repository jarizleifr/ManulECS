# ManulECS

An Entity-Component-System and a simple shared resource manager for C#, inspired by DefaultEcs, minECS, specs and Entt.

I wrote a library called ManulEC in 2019 for my roguelike game to provide simple runtime composition similar to Unity, but it had obvious flaws and performance issues, due to not being very cache-friendly. Thus, a need for ManulECS had arisen and I decided to write a new library from scratch, using structs and sparse sets.

# Features

- Focus on simplicity
  - ManulECS provides only the core functionality of composition and iteration.
  - If you need system classes, events, parallelism, you'll need to write them yourself.
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

There are two kinds of things assignable to entities, Components and Tags. They are specified by using marker interfaces `IComponent` and `ITag`. These are only used for method constraints to give some useful static typing, so we're not unnecessarily boxing our structs.

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

All Components/Tags need to be declared by the World object before they can be used. Trying to access an undeclared component will throw an exception.

```
world.Declare<Pos>();
world.Declare<IsPlayer>();
```

Declare method returns the World object, so declarations can be chained. See the section on code generation for something extra, if this feels too profuse.

```
world.Declare<Pos>().Declare<IsPlayer>();
```

Easiest way to assign new components to entities is to use field initializers. Assign will only assign component if not already found on the entity, Patch will replace the existing component.

```
world.Assign(entity, new Pos { x = 0, y = 0 }); // Will not overwrite
world.Patch(entity, new Pos { x = 1, y = 1 });  // Will overwrite
```

Tags have no replace function, as there's nothing to replace. Otherwise usage is similar, just that the data is omitted.

```
world.Assign<IsPlayer>(entity);
```

You can remove components one by one, or clear all components of type T from all entities. Components and Tags are removed/cleared the same way.

```
world.Remove<Component>(entity);  // Remove a component from a single entity
world.Remove<Tag>(entity);        // Remove a tag from a single entity
world.Clear<Component>();         // Clear all components of type
world.Clear<Tag>();               // Clear all tags of type
```

## Entity handles

You can also create an entity handle to easily add multiple components on an entity.

```
var entity = world.Handle()
  .Assign(new Component1 { })
  .Tag<SomeTag>()
  .Patch(new Component2 { })
  .GetEntity();
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

```
var json = world.Serialize();           // Default serialization
File.WriteAllText("save.json", json);
```

```
var json = world.Serialize("global");   // Serialize only resources and entities set to "global" profile
File.WriteAllText("global.json", json);
```

# Benchmarks

I've included my testing benchmarks in ManulECS.Benchmark project, using the BenchmarkDotNet library.

Benchmarks were run on Windows 10, .NET 6.0.4 with:
Intel Core i5-8600K CPU 3.60GHz (Coffee Lake), 1 CPU, 6 logical and 6 physical cores

## Creation and removal

It's hard to get good benchmarks as creation and removal is generally quite fast, and you'll need to pump N value into unreal amounts to get good data, but working with 10 million entities, we can make some observations:

1. Both creation and removal is faster on sparsely mapped components.
2. Sparsely mapped components take roughly 1.5 times more memory than densely mapped.
3. Creating tags is slightly faster than components, but removing them is about the same. Tags take less memory than either type of component.

|                              Method |        N |       Mean |    Error |   StdDev | Allocated |
|------------------------------------ |--------- |-----------:|---------:|---------:|----------:|
|                      CreateEntities | 10000000 |   330.7 ms |  6.51 ms | 10.51 ms |      1 GB |
|  CreateEntitiesWith1SparseComponent | 10000000 |   594.9 ms |  4.61 ms |  4.31 ms |      2 GB |
| CreateEntitiesWith2SparseComponents | 10000000 |   956.6 ms | 18.75 ms | 31.33 ms |      3 GB |
|   CreateEntitiesWith1DenseComponent | 10000000 |   702.6 ms | 11.96 ms | 11.18 ms |      2 GB |
|  CreateEntitiesWith2DenseComponents | 10000000 | 1,115.6 ms | 11.72 ms | 10.97 ms |      2 GB |
|        CreateEntitiesWith1SparseTag | 10000000 |   475.1 ms |  8.87 ms |  8.71 ms |      1 GB |
|       CreateEntitiesWith2SparseTags | 10000000 |   594.2 ms |  6.41 ms |  6.00 ms |      2 GB |
|         CreateEntitiesWith1DenseTag | 10000000 |   539.7 ms | 10.51 ms | 11.24 ms |      1 GB |
|        CreateEntitiesWith2DenseTags | 10000000 |   738.3 ms |  7.57 ms |  7.08 ms |      1 GB |

|                              Method |        N |      Mean |    Error |   StdDev |
|------------------------------------ |--------- |----------:|---------:|---------:|
|  Remove1SparseComponentFromEntities | 10000000 |  89.40 ms | 0.675 ms | 0.631 ms |
| Remove2SparseComponentsFromEntities | 10000000 | 180.25 ms | 1.182 ms | 1.048 ms |
|   Remove1DenseComponentFromEntities | 10000000 | 246.64 ms | 2.289 ms | 2.142 ms |
|  Remove2DenseComponentsFromEntities | 10000000 | 508.07 ms | 5.175 ms | 4.587 ms |
|        Remove1SparseTagFromEntities | 10000000 |  87.23 ms | 1.553 ms | 1.453 ms |
|        Remove2SparseTagFromEntities | 10000000 | 177.42 ms | 1.722 ms | 1.526 ms |
|         Remove1DenseTagFromEntities | 10000000 | 229.81 ms | 3.419 ms | 3.198 ms |
|         Remove2DenseTagFromEntities | 10000000 | 470.47 ms | 6.776 ms | 6.338 ms |

## Iterating views

All tag iterations finished looping at around 74 μs. As tags have no values and cannot be operated on, there's practically no difference between iterating 1 or 2 tags.

For components, the update functions are simple move operations with an x,y transform applied on x,y coordinates.

Using a sparse pool:

|            Method |      N |     Mean |   Error |  StdDev | Allocated |
|------------------ |------- |---------:|--------:|--------:|----------:|
|  Update1Component | 100000 | 255.9 μs | 2.90 μs | 2.71 μs |       1 B |
| Update2Components | 100000 | 457.3 μs | 2.64 μs | 2.06 μs |       3 B |

Using a dense pool:

|            Method |      N |       Mean |    Error |   StdDev | Allocated |
|------------------ |------- |-----------:|---------:|---------:|----------:|
|  Update1Component | 100000 |   821.2 μs | 13.35 μs | 12.49 μs |       3 B |
| Update2Components | 100000 | 1,618.1 μs | 25.01 μs | 23.39 μs |       1 B |

Again, sparse pools are a lot faster, but one can optimize memory usage with dense pools.

# Extra - Code generation

I've included a Roslyn generator, which can auto-generate an extension method for you to declare all structs that have the `IComponent` or `ITag` marker interface. It's a bit of a brute force solution, but it makes things a bit more maintainable, when you're still developing your components and things change often.

This obviously won't work, if you're keen on having multiple worlds of different sets of components.

Add the ManulECS.Generators project in your own .csproj file as an analyzer:

```
<ProjectReference Include="..\ManulECS\ManulECS.Generators\ManulECS.Generators.csproj">
  <OutputItemType>Analyzer</OutputItemType>
  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
</ProjectReference>
```

Build your project and you should be good to go!

```
var world = new World();
world.DeclareAll();
```
