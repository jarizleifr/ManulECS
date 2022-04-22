# ManulECS

An Entity-Component-System and a simple shared resource manager for C#, inspired by DefaultEcs, minECS, specs and Entt.

I wrote a library called ManulEC in 2019 for my roguelike game to provide simple runtime composition similar to Unity, but it had obvious flaws and performance issues. Thus, a need for ManulECS had arisen and I decided to write a new library from scratch, using structs and sparse sets.

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

We start reaching the multi-ms range at creating/removing 100000 entities per frame, which on its own seems like a bit extreme use case.

|                        Method |      N |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------------ |------- |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
|                CreateEntities | 100000 | 2.025 ms | 0.0220 ms | 0.0206 ms |  50.0000 |  50.0000 |  50.0000 |      4 MB |
|  CreateEntitiesWith1Component | 100000 | 4.732 ms | 0.0458 ms | 0.0429 ms |  75.0000 |  75.0000 |  75.0000 |      7 MB |
| CreateEntitiesWith2Components | 100000 | 7.471 ms | 0.0895 ms | 0.0837 ms | 100.0000 | 100.0000 | 100.0000 |     10 MB |
|        CreateEntitiesWith1Tag | 100000 | 3.756 ms | 0.0669 ms | 0.0626 ms |  62.5000 |  62.5000 |  62.5000 |      6 MB |
|       CreateEntitiesWith2Tags | 100000 | 5.922 ms | 0.0365 ms | 0.0324 ms |  87.5000 |  87.5000 |  87.5000 |      7 MB |

Creating tags is a bit faster than creating components, but they're about the same when removing. Fastest way to get rid of components, is to remove the entire entity holding them.

|                        Method |      N |       Mean |    Error |   StdDev |
|------------------------------ |------- |-----------:|---------:|---------:|
|                RemoveEntities | 100000 |   649.8 us | 10.06 us |  9.41 us |
|  Remove1ComponentFromEntities | 100000 | 1,211.8 us | 11.55 us | 10.80 us |
| Remove2ComponentsFromEntities | 100000 | 2,445.5 us | 19.32 us | 17.13 us |
|        Remove1TagFromEntities | 100000 | 1,185.9 us | 17.34 us | 16.22 us |
|        Remove2TagFromEntities | 100000 | 2,390.0 us | 10.53 us |  8.79 us |

## Iterating views

For components, the update functions are simple move operations with an x/x,y transform applied on x/x,y coordinates.

The worst case scenario benchmark rebuilds the entity view on each iteration. There is currently no sorting mechanism, so iteration isn't as cache-friendly as I'd like, but it's _fast enough_, with the added bonus that we can use simple foreach loops instead of complex lambdas (no need to worry about closures) or creating system classes.

My previous benchmark for two component update was 465 us, so I'm pretty happy with the current performance.

|                              Method |      N |     Mean |   Error |  StdDev |
|------------------------------------ |------- |---------:|--------:|--------:|
|                    Update1Component | 100000 | 120.2 us | 1.29 us | 1.20 us |
|                   Update2Components | 100000 | 251.3 us | 2.36 us | 2.21 us |
| Update2Components_WorstCaseScenario | 100000 | 686.6 us | 3.01 us | 2.67 us |

For tags, there's virtually no difference between looping through 1 or 2 tags.

|    Method |      N |     Mean |    Error |   StdDev |
|---------- |------- |---------:|---------:|---------:|
|  Loop1Tag | 100000 | 25.32 us | 0.169 us | 0.158 us |
| Loop2Tags | 100000 | 25.24 us | 0.166 us | 0.147 us |

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
