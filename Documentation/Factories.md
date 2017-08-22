
## <a id="creating-objects-dynamically"></a>Creating Objects Dynamically Using Factories

One of the things that often confuses people new to dependency injection is the question of how to create new objects dynamically, after the app/game has fully started up and all the `IInitializable` objects have had their Initialize() method called.  For example, if you are writing a game in which you are spawning new enemies throughout the game, then you will want to construct a new object for the 'enemy' class, and you will want to ensure that this object gets injected with dependencies just like all the objects that are part of the initial object graph.  How to do this?  The answer: Factories.

Remember that an important part of dependency injection is to reserve use of the container to strictly the "Composition Root Layer".  The container class (DiContainer) is included as a dependency in itself automatically so there is nothing stopping you from ignoring this rule and injecting the container into any classes that you want.  For example, the following code will work:

```csharp
public class Enemy
{
    DiContainer Container;

    public Enemy(DiContainer container)
    {
        Container = container;
    }

    public void Update()
    {
        ...
        var player = Container.Resolve<Player>();
        WalkTowards(player.Position);
        ...
        etc.
    }
}
```

However, the above code is an example of an anti-pattern.  This will work, and you can use the container to get access to all other classes in your app, however if you do this you will not really be taking advantage of the power of dependency injection.  This is known, by the way, as [Service Locator Pattern](http://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/).

Of course, the dependency injection way of doing this would be the following:

```csharp
public class Enemy
{
    Player _player;

    public Enemy(Player player)
    {
        _player = player;
    }

    public void Update()
    {
        ...
        WalkTowards(_player.Position);
        ...
    }
}
```

The only exception to this rule is within factories and installers.  Again, factories and installers make up what we refer to as the "composition root layer".

For example, if you have a class responsible for spawning new enemies, before DI you might do something like this:

```csharp
public class EnemySpawner
{
    List<Enemy> _enemies = new List<Enemy>();

    public void Update()
    {
        if (ShouldSpawnNewEnemy())
        {
            var enemy = new Enemy();
            _enemies.Add(enemy);
        }
    }
}
```

This will not work however, since in our case the Enemy class requires a reference to the Player class in its constructor.  We could add a dependency to the Player class to the EnemySpawner class, but then we have the problem described <a href="../README.md#theory">above</a>.  The EnemySpawner class doesn't care about filling in the dependencies for the Enemy class.  All the EnemySpawner class cares about is getting a new Enemy instance.

The recommended way to do this in Zenject is the following:

```csharp
public class Player
{
}

public class Enemy
{
    readonly Player _player;

    public Enemy(Player player)
    {
        _player = player;
    }

    public class Factory : Factory<Enemy>
    {
    }
}

public class EnemySpawner : ITickable
{
    readonly Enemy.Factory _enemyFactory;

    public EnemySpawner(Enemy.Factory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }

    public void Tick()
    {
        if (ShouldSpawnNewEnemy())
        {
            var enemy = _enemyFactory.Create();
            // ...
        }
    }
}

public class TestInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<EnemySpawner>().AsSingle();
        Container.Bind<Player>().AsSingle();
        Container.BindFactory<Enemy, Enemy.Factory>();
    }
}
```

By using `Enemy.Factory` above instead of `new Enemy`, all the dependencies for the Enemy class (such as the Player) will be automatically filled in.  We can also add runtime parameters to our factory.  For example, let's say we want to randomize the speed of each Enemy to add some interesting variation to our game.  Our enemy class becomes:

```csharp
public class Enemy
{
    readonly Player _player;
    readonly float _speed;

    public Enemy(float speed, Player player)
    {
        _player = player;
        _speed = speed;
    }

    public class Factory : Factory<float, Enemy>
    {
    }
}

public class EnemySpawner : ITickable
{
    readonly Enemy.Factory _enemyFactory;

    public EnemySpawner(Enemy.Factory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }

    public void Tick()
    {
        if (ShouldSpawnNewEnemy())
        {
            var newSpeed = Random.Range(MIN_ENEMY_SPEED, MAX_ENEMY_SPEED);
            var enemy = _enemyFactory.Create(newSpeed);
            // ...
        }
    }
}

public class TestInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<EnemySpawner>().AsSingle();
        Container.Bind<Player>().AsSingle();
        Container.BindFactory<float, Enemy, Enemy.Factory>();
    }
}
```

The dynamic parameters that are provided to the Enemy constructor are declared by using generic arguments to the Factory<> base class of Enemy.Factory.  This will add a method to Enemy.Factory that takes the parameters with the given types, which is called by EnemySpawner.

Note: You can have multiple parameters declared with the same type.  In this case, the order that the values are given to the factory will match the parameter order - assuming that you are using constructor or method injection.  However, if you are using field or property injection, then the order that values are injected is not guaranteed to follow the declaration order, since these fields are retrieved using Type.GetFields which does not guarantee order as described <a href="https://msdn.microsoft.com/en-us/library/ch9714z3.aspx">here</a>

In this case, they will be injected in the same order that they are declared in your class.  In the case of constructor/inject-method parameters, this will be the order the parameters are given in, or in the case of fields the order will be from top to bottom that they are declared in.  Constructor parameters are processed first, then fields, then properties, then inject methods.

There is no requirement that the `Enemy.Factory` class be a nested class within Enemy, however we have found this to be a very useful convention.  `Enemy.Factory` is always intentionally left empty and simply derives from the built-in Zenject `Factory<>` class, which handles the work of using the DiContainer to construct a new instance of Enemy.

The reason that we don't use the `Factory<Enemy>` class directly is because this can become error prone when adding/removing parameters and also can get verbose since the parameter types must be declared whenever you refer to this class.  This is error prone because if for example you add a parameter to enemy and change the factory from `Factory<Enemy>` to `Factory<float, Enemy>`, any references to `Factory<Enemy>` will not be resolved.  And this will not be caught at compile time and instead will only be seen during validation or runtime.  So we recommend always using an empty class that derives from `Factory<>` instead.

Also note that by using the built-in Zenject `Factory<>` class, the Enemy class will be automatically validated as well.  So if the constructor of the Enemy class includes a type that is not bound to the container, this error can be caught before running your app, by simply running validation.  Validation can be especially useful for dynamically created objects, because otherwise you may not catch the error until the factory is invoked at some point during runtime.  See the validation section for more details on Validation.

This is all well and good for simple cases like this, but what if the Enemy class derives from MonoBehaviour and is instantiated via a prefab?  Or what if I want to create Enemy using a custom method or a custom factory class?

These cases are handled very similar to how they are handled when not using a factory.  When you bind the factory to the container using `BindFactory`, you have access to the same bind methods that are given by the Bind method (see binding section for full details).  For example, if we wanted to instantiate the `Enemy` class from a prefab, our code would become:

```csharp
public class Enemy : MonoBehaviour
{
    Player _player;

    // Note that we can't use a constructor anymore since we are a MonoBehaviour now
    [Inject]
    public void Construct(Player player)
    {
        _player = player;
    }

    public class Factory : Factory<Enemy>
    {
    }
}

public class TestInstaller : MonoInstaller
{
    public GameObject EnemyPrefab;

    public override void InstallBindings()
    {
        Container.BindInterfacesTo<EnemySpawner>().AsSingle();
        Container.Bind<Player>().AsSingle();
        Container.BindFactory<Enemy, Enemy.Factory>().FromComponentInNewPrefab(EnemyPrefab);
    }
}

```

And similarly for FromInstance, FromMethod, FromSubContainerResolve, or any of the other construction methods.

Using FromSubContainerResolve can be particularly useful if your dynamically created object has a lot of its own dependencies.  You can have it behave like a Facade.  See the Subcontainers section for details on nested containers / facades.

Also note that you for dynamically instantiated MonoBehaviours (for example when using FromComponentInNewPrefab with BindFactory) injection should always occur before Awake and Start, so you can use Awake and Start for initialization logic and use the inject method strictly for saving dependencies (ie. similar to constructors for non-monobehaviours)

## <a id="abstract-factories"></a>Abstract Factories

The above description of factories is great for most cases, however, there are times you do not want to depend directly on a concrete class and instead want your factory to return an interface instead.  This kind of factory is called an Abstract Factory.

Let's create an example scenario, where we have multiple different implementations of a given interface:

```csharp

public interface IPathFindingStrategy
{
    ...
}

public class AStarPathFindingStrategy : IPathFindingStrategy
{
    ...
}

public class RandomPathFindingStrategy : IPathFindingStrategy
{
    ...
}
```

For the sake of this example, let's also assume that we have to create the instance of IPathFindingStrategy at runtime.  Otherwise it would be as simple as executing `Container.Bind<IPathFindingStrategy>().To<TheImplementationWeWant>().AsSingle();` in one of our installers.

This is done in a very similar way that non-Abstract factories work.  One difference is that we can't include the factory as a nested class inside the interface (not allowed in C#) but otherwise it's no different:

```csharp
public class PathFindingStrategyFactory : Factory<IPathFindingStrategy>
{
}

public class GameController : IInitializable
{
    PathFindingStrategyFactory _strategyFactory;
    IPathFindingStrategy _strategy;

    public GameController(PathFindingStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory;
    }

    public void Initialize()
    {
        _strategy = _strategyFactory.Create();
        // ...
    }
}

public class GameInstaller : MonoInstaller
{
    public bool UseAStar;

    public override void InstallBindings()
    {
        Container.BindInterfacesTo<GameController>().AsSingle();

        if (UseAStar)
        {
            Container.BindFactory<IPathFindingStrategy, PathFindingStrategyFactory>().To<AStarPathFindingStrategy>();
        }
        else
        {
            Container.BindFactory<IPathFindingStrategy, PathFindingStrategyFactory>().To<RandomPathFindingStrategy>();
        }
    }
}
```

## <a id="custom-factories"></a>Custom Factories

*Ok, but what if I don't know what type I want to create until after the application has started?  Or what if I have special requirements for constructing instances of the Enemy class that are not covered by any of the construction methods?* 

In these cases you can create a custom factory, and directly call `new Enemy` or directly use the <a href="../README.md#dicontainer-methods">methods on DiContainer</a> to create your object.  For example, continuing the previous factory example, let's say that you wanted to be able to change a runtime value (difficulty) that determines what kinds of enemies get created.

```csharp
public enum Difficulties
{
    Easy,
    Hard,
}

public interface IEnemy
{
}

public class EnemyFactory : Factory<IEnemy>
{
}

public class Demon : IEnemy
{
}

public class Dog : IEnemy
{
}

public class DifficultyManager
{
    public Difficulties Difficulty
    {
        get;
        set;
    }
}

public class CustomEnemyFactory : IFactory<IEnemy>
{
    DiContainer _container;
    DifficultyManager _difficultyManager;

    public CustomEnemyFactory(DiContainer container, DifficultyManager difficultyManager)
    {
        _container = container;
        _difficultyManager = difficultyManager;
    }

    public IEnemy Create()
    {
        if (_difficultyManager.Difficulty == Difficulties.Hard)
        {
            return _container.Instantiate<Demon>();
        }

        return _container.Instantiate<Dog>();
    }
}

public class GameController : IInitializable
{
    readonly EnemyFactory _enemyFactory;

    public GameController(EnemyFactory enemyFactory)
    {
        _enemyFactory = enemyFactory;
    }

    public void Initialize()
    {
        var enemy = _enemyFactory.Create();
        // ...
    }
}

public class TestInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<GameController>().AsSingle();
        Container.Bind<DifficultyManager>().AsSingle();
        Container.BindFactory<IEnemy, EnemyFactory>().FromFactory<CustomEnemyFactory>();
    }
}
```

You could also directly call `new Dog()` and `new Demon()` here instead of using the DiContainer (though in that case `Dog` and `Demon` would not have their members injected).

One important issue to be aware of with using custom factories, is that the dynamically created classes will not be validated properly.  So in this example, if the `Demon` or `Dog` classes have a constructor parameter that is not bound to the DiContainer, that will not become obvious until runtime.  Whereas, if using a normal factory, that would be caught during validation.

If you want to properly validate your custom factories, you can do that by just making a small modification to it:

```csharp
public class CustomEnemyFactory : IFactory<IEnemy>, IValidatable
{
    DiContainer _container;
    DifficultyManager _difficultyManager;

    public CustomEnemyFactory(DiContainer container, DifficultyManager difficultyManager)
    {
        _container = container;
        _difficultyManager = difficultyManager;
    }

    public IEnemy Create()
    {
        if (_difficultyManager.Difficulty == Difficulties.Hard)
        {
            return _container.Instantiate<Demon>();
        }

        return _container.Instantiate<Dog>();
    }

    public void Validate()
    {
        _container.Instantiate<Dog>();
        _container.Instantiate<Demon>();
    }
}
```

This is done by implementing the interface `IValidatable` and then adding a `Validate()` method.  Then, to manually validate objects, you simply instantiate them.  Note that this will not actually instantiate these objects (these calls actually return null here).  The point is to do a "dry run" without actually instantiating anything, to prove out the full object graph.  For more details on validation see the validation section.

## <a id="custom-interface"></a>Custom Factory Interface

In some cases, you might want to avoid becoming directly coupled to the factory class, and would prefer to use a base class or an interface instead.  You can do that by using the `BindFactoryContract` method instead of `BindFactory` like this:

```
public interface IMyFooFactory : IFactory<Foo>
{
}

public class Foo
{
    public class Factory : Factory<Foo>, IMyFooFactory
    {
    }
}

public class Runner : IInitializable
{
    readonly IMyFooFactory _fooFactory;

    public Runner(IMyFooFactory fooFactory)
    {
        _fooFactory = fooFactory;
    }

    public void Initialize()
    {
        var foo = _fooFactory.Create();
        // ...
    }
}

public class FooInstaller : MonoInstaller<FooInstaller>
{
    public override void InstallBindings()
    {
        Container.BindFactoryContract<Foo, IMyFooFactory, Foo.Factory>();
    }
}
```
