# ManulECS

An Entity-Component-System and a simple shared resource manager for C#, inspired by DefaultEcs, minECS, specs and Entt.

I created ManulEC in 2019 for my roguelike game to provide simple runtime composition similar to Unity, but it had obvious flaws and performance issues. Thus, a need for ManulECS had arisen and I decided to write a new library from scratch.

## Features

- Focus on simplicity
- Data-driven, cache-coherent ECS, achieved with sparse sets of structs
- Support for tag components without data
- Support for non-entity resources
- Declarative, control internals by providing attributes to components/resources
- Serialization powered by Json.NET

## Overview

### World

```
var world = new World();        // Create a new entity registry.
```

### Entity

Entity objects

```
var entity = world.Create();    // Create a new entity
world.Remove(entity);           // Remove the entity, clearing all components in the process.
```

### Components and Tags

There are two kinds of things assignable to entities, Components and Tags. They are specified by using marker interfaces `IComponent` and `ITag`. These are only used for method constraints to give some useful static typing, so we're not unnecessarily boxing our structs.

Components are simple data structs.

```
struct Pos : IComponent
{
    public int x;
    public int y;
}
```

Tags don't contain any data. They're like a typed flag that you can set on an entity.

```
struct IsPlayer : ITag {}
```

All Components/Tags need to be declared by the World object before they can be used. Trying to access an undeclared component will throw an exception.

```
world.Declare<Pos>();
world.Declare<IsPlayer>();
```

Easiest way to assign new components to entities is to use field initializers. Assign will only assign component if not already found on the entity, AssignOrReplace will replace the existing component.

```
world.Assign(entity, new Pos { x = 0, y = 0 });             // Will not overwrite
world.AssignOrReplace(entity, new Pos { x = 1, y = 1 });    // Will overwrite
```

Tags have no replace function, as there's nothing to replace. Otherwise usage is similar, just that the data is omitted.

```
world.Assign<IsPlayer>(entity);
```

You can remove components one by one, or clear all components of type T from all entities. Components and Tags are removed/cleared the same way.

```
world.Remove<Component>(entity);    // Remove a component from a single entity
world.Remove<Tag>(entity);          // Remove a tag from a single entity
world.Clear<Component>();           // Clear all components of type
world.Clear<Tag>();                 // Clear all tags of type
```

### Resource

Resources are classes that exist outside the sphere of entities. Resources are serialized just like entities, making them a good choice for complex data that needs to be persisted in a save game.

Examples of objects that make good candiates for resources could be the current level in a game, or a clock, that controls whether it's day or night in the game.

```
var level = CreateLevel();
world.SetResource(level);
```

```
var level = world.GetResource<Level>();
```

### Systems

ManulECS provides a View of Entities that you can iterate through with a foreach loop. Views are automatically updated on iteration if the related component pool has been modified.

You can use Pools<T...> method to improve performance by providing reusable component pool to read from.

```
public static void MoveSystem(World world)
{
    var (positions, velocities) = world.Pools<Position, Velocity>();
    foreach (var e in world.View<Position, Velocity>())
    {
        ref var pos = ref positions[e];
        ref var vel = ref velocities[e];

        pos.coords += vel.transform;
    }
}
```

Tags are handled by views as well! You can use Tags to easily filter out results.

```
foreach (var e in world.View<Position, Velocity, SomeInterestingTag>())
{
    ...
}
```

### Serialization

You can control what components should be serialized by attributes. This is opt-out, by default everything is included.

```
[NeverSerializeComponent]
public struct IntentionMove : IComponent { }     // This component is never serialized

[NeverSerializeEntity]
public struct VisualEffect : IComponent { }      // The entire entity owning this component is never serialized
```

ManulECS also features a concept of serialization profiles, which you declare on component and resource basis. Always affects the entire entity.

```
[SerializationProfile("global")]    // The entity will only be serialized on world.Serialize("global")
public struct Player : IComponent { }
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

### Code generation

I've included a Roslyn generator, which can auto-generate an extension method to declare all structs that have the `IComponent` or `ITag` marker interface. It's a bit of a brute force solution, but it makes things a bit more maintainable.

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
