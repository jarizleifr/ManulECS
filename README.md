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

|                        Method |        N |       Mean |    Error |   StdDev |     Median |
|------------------------------ |--------- |-----------:|---------:|---------:|-----------:|
|                CreateEntities | 10000000 |   363.2 ms |  7.22 ms | 13.56 ms |   364.7 ms |
|  CreateEntitiesWith1Component | 10000000 |   681.3 ms |  5.20 ms |  4.86 ms |   680.0 ms |
| CreateEntitiesWith2Components | 10000000 | 1,089.5 ms | 21.49 ms | 47.18 ms | 1,114.5 ms |
|        CreateEntitiesWith1Tag | 10000000 |   574.9 ms |  8.21 ms |  7.68 ms |   575.7 ms |
|       CreateEntitiesWith2Tags | 10000000 |   740.5 ms | 13.76 ms | 13.51 ms |   738.1 ms |

|                        Method |        N |     Mean |   Error |  StdDev |
|------------------------------ |--------- |---------:|--------:|--------:|
|  Remove1ComponentFromEntities | 10000000 | 152.4 ms | 1.16 ms | 1.09 ms |
| Remove2ComponentsFromEntities | 10000000 | 312.9 ms | 4.97 ms | 4.65 ms |
|        Remove1TagFromEntities | 10000000 | 146.5 ms | 1.08 ms | 1.01 ms |
|        Remove2TagFromEntities | 10000000 | 293.2 ms | 2.06 ms | 1.93 ms |

## Iterating views

For components, the update functions are simple move operations with an x,y transform applied on x,y coordinates.

The worst case scenario is having to build the entity view on every iteration.

|                              Method |      N |     Mean |   Error |  StdDev |
|------------------------------------ |------- |---------:|--------:|--------:|
|                    Update1Component | 100000 | 120.2 us | 1.29 us | 1.20 us |
|                   Update2Components | 100000 | 251.3 us | 2.36 us | 2.21 us |
| Update2Components_WorstCaseScenario | 100000 | 686.6 us | 3.01 us | 2.67 us |

All tag iterations were quite fast and there's virtually no difference between looping through 1 or 2 tags.

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
