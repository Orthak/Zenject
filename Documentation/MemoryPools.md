
## <a id="memory-pools"></a>Memory Pools

### <a id="introduction"></a>Introduction

Before understanding memory pools it would be helpful to understand factories, so please read the <a href="Factories.md">docs on factories</a> first.

It doesn't take long when developing games in Unity before you realize that proper memory management is very important if you want your game to run smoothly (especially on mobile).  Depending on the constraints of the platform and the type of game you are working on, it might be very important to avoid unnecessary heap allocations as much as possible.  One very effective way to do this is to use memory pools.

As an example let's look at at a case where we are dynamically creating a class:

```csharp
public class Foo
{
    public class Factory : Factory<Foo>
    {
    }
}

public class Bar
{
    readonly Foo.Factory _fooFactory;
    readonly List<Foo> _foos = new List<Foo>();

    public Bar(Foo.Factory fooFactory)
    {
        _fooFactory = fooFactory;
    }

    public void AddFoo()
    {
        _foos.Add(_fooFactory.Create());
    }

    public void RemoveFoo()
    {
        _foos.RemoveAt(0);
    }
}

public class TestInstaller : MonoInstaller<TestInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<Bar>().AsSingle();
        Container.BindFactory<Foo, Foo.Factory>();
    }
}
```

Here, every time we call Bar.AddFoo it will always allocate new heap memory. And every time we call Bar.RemoveFoo, the Bar class will stop referencing one of the instances of Foo, and therefore the memory for that instance will be marked for garbage collection.  If this happens enough times, then eventually the garbage collector will kick in and you will get a (sometimes very noticeable) spike in your game.

We can fix this spike by using memory pools instead:

```csharp
public class Foo
{
    public class Pool : MemoryPool<Foo>
    {
    }
}

public class Bar
{
    readonly Foo.Pool _fooPool;
    readonly List<Foo> _foos = new List<Foo>();

    public Bar(Foo.Pool fooPool)
    {
        _fooPool = fooPool;
    }

    public void AddFoo()
    {
        _foos.Add(_fooPool.Spawn());
    }

    public void RemoveFoo()
    {
        var foo = _foos[0];
        _fooPool.Despawn(foo);
        _foos.Remove(foo);
    }
}

public class TestInstaller : MonoInstaller<TestInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<Bar>().AsSingle();
        Container.BindMemoryPool<Foo, Foo.Pool>();
    }
}
```

As you can see, this works very similarly to factories, except that the terminology is a bit different (Pool instead of Factory, Spawn instead of Create) and unlike factories, you have to return the instance to the pool rather than just stop referencing it.

With this new implementation above, there will be some initial heap allocation with every call to AddFoo(), but if you call RemoveFoo() then AddFoo() in sequence this will re-use the previous instance and therefore save you a heap allocation.

This is better, but we might still want to avoid the spikes from the initial heap allocations as well.  One way to do this is to do all the heap allocations all at once as your game is starting up:

```csharp
public class TestInstaller : MonoInstaller<TestInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<Bar>().AsSingle();
        Container.BindMemoryPool<Foo, Foo.Pool>().WithInitialSize(10);
    }
}
```

When we use WithInitialSize like this in the Bind statement for our pool, 10 instances of Foo will be created immediately on startup to seed the pool.

## <a id="syntax"></a>Syntax

The syntax for memory pools are almost identical to factories.  The recommended convention is to use a public nested class named Pool (though this is just a convention).

```csharp
public class Foo
{
    public class Pool : MemoryPool<Foo>
    {
    }
}
```

Parameters are added by adding generic arguments:

```csharp
public class Foo
{
    public class Pool : MemoryPool<string, int, Foo>
    {
    }
}
```

The full format of the binding is the following:

<pre>
Container.BindMemoryPool&lt;<b>ObjectType, MemoryPoolType, MemoryPoolContractType</b>&gt;()
    .With<b>(InitialSize|FixedSize)</b>()
    .ExpandBy<b>(OneAtATime|Doubling)</b>()
    .To&lt;<b>ResultType</b>&gt;()
    .WithId(<b>Identifier</b>)
    .From<b>ConstructionMethod</b>()
    .WithArguments(<b>Arguments</b>)
    .When(<b>Condition</b>)
    .CopyIntoAllSubContainers()
    .NonLazy();
</pre>

Where:

* **ObjectType** = The type of the class that is being instantiated by the memory pool

* **MemoryPoolType** = The type of the MemoryPool derived class, which is often a nested class of `ObjectType`.

* **MemoryPoolContractType** = Note:  This can be left unspecified most of the time (ie. only the first two generic parameters are required).  This is the contract type to match for injected members.  In other words, when zenject injects on an object, it will be looking for constructor parameters/inject fields that have this type before injecting the memory pool instance.  It must be equal to or a parent class/interface of the given `MemoryPoolType`.  This is only useful for cases where you want to inject your memory pool by an interface like `IMemoryPool<Foo>` or a custom interface.

* **With** = Determines the number of instances that the pool is seeded with.  When not specified, the pool starts with zero instances.  The options are:

    * WithInitializeSize(x) - Create x instances immediately when the pool is created.  The pool is also allowed to grow as necessary if it exceeds that amount
    * WithFixedSize(x) - Create x instances immediately when the pool is created.  If the pool size is exceeded then an exception is thrown.

* **ExpandBy** = Determines the behaviour to invoke when the pool reaches its maximum size.  Note that when specifying WithFixedSize, this option is not available.  The options are:

    * ExpandByOneAtATime - Only allocate new instances one at a time as necessary
    * ExpandByDoubling - When the pool is full and a new instance is requested, the pool will double in size before returning the requested instance.  This approach can be useful if you prefer having large infrequent allocations to many small frequent allocations

The rest of the bind methods behave the same as the normal bind methods documented <a href="../README.md#binding">here</a>

## <a id="resetting"></a>Resetting

One very important thing to be aware of when using memory pools instead of factories is that you must make sure to completely "refresh" the given instance.  This is necessary otherwise you might have state a previous "life" of the instance bleeding in to the behaviour of the new instance.

You can refresh the object by implementing any of the following methods in your memory pool derived class:

```csharp
public class Foo
{
    public class Pool : MemoryPool<Foo>
    {
        protected override void OnCreated(Foo item)
        {
            // Called immediately after the item is first added to the pool
        }

        protected override void OnSpawned(Foo item)
        {
            // Called immediately after the item is removed from the pool
        }

        protected override void OnDespawned(Foo item)
        {
            // Called immediately after the item is returned to the pool
        }

        protected override void Reinitialize(Foo foo)
        {
            // Similar to OnSpawned
            // Called immediately after the item is removed from the pool
            // This method will also contain any parameters that are passed along
            // to the memory pool from the spawning code
        }
    }
}
```

In most cases, you will probably only have to implement the Reinitialize method.   For example, let's introduce some state to our first example by adding a Position value to Foo:

```csharp
public class Foo
{
    Vector3 _position = Vector3.zero;

    public void Move(Vector3 delta)
    {
        _position += delta;
    }

    public class Pool : MemoryPool<Foo>
    {
        protected override void Reinitialize(Foo foo)
        {
            foo._position = Vector3.zero;
        }
    }
}
```

Note that our pool class is free to access private variables inside Foo because of the fact that it is a nested class.

Or, if we wanted to avoid the duplication in Foo and Foo.Pool, we could do it this way:

```csharp
public class Foo
{
    Vector3 _position;

    public Foo()
    {
        Reset();
    }

    public void Move(Vector3 delta)
    {
        _position += delta;
    }

    void Reset()
    {
        _position = Vector3.zero;
    }

    public class Pool : MemoryPool<Foo>
    {
        protected override void Reinitialize(Foo foo)
        {
            foo.Reset();
        }
    }
}
```

## <a id="parameters"></a>Parameters

Just like Factories, you can also pass runtime parameters when spawning new instances of your pooled classes.  The difference is, instead of the parameters being injected into the class, they are passed to the Reinitialize method:

```csharp
public class Foo
{
    Vector3 _position;
    Vector3 _velocity;

    public Foo()
    {
        Reset();
    }

    public void Tick()
    {
        _position += _velocity * Time.deltaTime;
    }

    void Reset()
    {
        _position = Vector3.zero;
        _velocity = Vector3.zero;
    }

    public class Pool : MemoryPool<Vector3, Foo>
    {
        protected override void Reinitialize(Vector3 velocity, Foo foo)
        {
            foo.Reset();
            foo._velocity = velocity;
        }
    }
}

public class Bar
{
    readonly Foo.Pool _fooPool;
    readonly List<Foo> _foos = new List<Foo>();

    public Bar(Foo.Pool fooPool)
    {
        _fooPool = fooPool;
    }

    public void AddFoo()
    {
        float maxSpeed = 10.0f;
        float minSpeed = 1.0f;

        _foos.Add(_fooPool.Spawn(
            Random.onUnitSphere * Random.Range(minSpeed, maxSpeed)));
    }

    public void RemoveFoo()
    {
        var foo = _foos[0];
        _fooPool.Despawn(foo);
        _foos.Remove(foo);
    }
}
```

## <a id="monomemorypool"></a>Memory Pools for MonoBehaviours

Memory pools for GameObjects works similarly.  For example:

```csharp
public class Foo : MonoBehaviour
{
    Vector3 _velocity;

    [Inject]
    public void Construct()
    {
        Reset();
    }

    public void Update()
    {
        transform.position += _velocity * Time.deltaTime;
    }

    void Reset()
    {
        transform.position = Vector3.zero;
        _velocity = Vector3.zero;
    }

    public class Pool : MonoMemoryPool<Vector3, Foo>
    {
        protected override void Reinitialize(Vector3 velocity, Foo foo)
        {
            foo.Reset();
            foo._velocity = velocity;
        }
    }
}

public class Bar
{
    readonly Foo.Pool _fooPool;
    readonly List<Foo> _foos = new List<Foo>();

    public Bar(Foo.Pool fooPool)
    {
        _fooPool = fooPool;
    }

    public void AddFoo()
    {
        float maxSpeed = 10.0f;
        float minSpeed = 1.0f;

        _foos.Add(_fooPool.Spawn(
            Random.onUnitSphere * Random.Range(minSpeed, maxSpeed)));
    }

    public void RemoveFoo()
    {
        var foo = _foos[0];
        _fooPool.Despawn(foo);
        _foos.Remove(foo);
    }
}

public class TestInstaller : MonoInstaller<TestInstaller>
{
    public GameObject FooPrefab;

    public override void InstallBindings()
    {
        Container.Bind<Bar>().AsSingle();

        Container.BindMemoryPool<Foo, Foo.Pool>()
            .WithInitialSize(2)
            .FromComponentInNewPrefab(FooPrefab)
            .UnderTransformGroup("Foos");
    }
}
```

The main difference here is that Foo.Pool now derives from MonoMemoryPool instead of MemoryPool.  MonoMemoryPool is a helper class that will automatically enable and disable the game object for us when it is added/removed from the pool.  The implementation for MonoMemoryPool is simply this:

```csharp
public abstract class MonoMemoryPool<TParam1, TValue> : MemoryPool<TParam1, TValue>
    where TValue : Component
{
    protected override void OnCreated(TValue item)
    {
        item.gameObject.SetActive(false);
    }

    protected override void OnSpawned(TValue item)
    {
        item.gameObject.SetActive(true);
    }

    protected override void OnDespawned(TValue item)
    {
        item.gameObject.SetActive(false);
    }
}
```

Therefore, if you override one of these methods you will have to make sure to call the base version as well.

Also, worth noting is the fact that for this logic to work, our MonoBehaviour must be at the root of the prefab, since otherwise only the transform associated with Foo and any children will be disabled.

## <a id="abstract-pools"></a>Abstract Memory Pools

Just like <a href="Factories.md#abstract-factories">abstract factories</a>, sometimes you might want to create a memory pool that returns an interface, with the concrete type decided inside an installer.  This works very similarly to abstract factories.  For example:

```csharp
public interface IFoo
{
}

public class Foo1 : IFoo
{
}

public class Foo2 : IFoo
{
}

public class FooPool : MemoryPool<IFoo>
{
}

public class Bar
{
    readonly FooPool _fooPool;
    readonly List<IFoo> _foos = new List<IFoo>();

    public Bar(FooPool fooPool)
    {
        _fooPool = fooPool;
    }

    public void AddFoo()
    {
        _foos.Add(_fooPool.Spawn());
    }

    public void RemoveFoo()
    {
        var foo = _foos[0];
        _fooPool.Despawn(foo);
        _foos.Remove(foo);
    }
}

public class TestInstaller : MonoInstaller<TestInstaller>
{
    public bool Use1;

    public override void InstallBindings()
    {
        Container.Bind<Bar>().AsSingle();

        if (Use1)
        {
            Container.BindMemoryPool<IFoo, FooPool>().WithInitialSize(10).To<Foo1>();
        }
        else
        {
            Container.BindMemoryPool<IFoo, FooPool>().WithInitialSize(10).To<Foo2>();
        }
    }
}
```

We might also want to add a Reset() method to the IFoo interface as well here, and call that on Reinitialize()

### <a id="instantiating-directory"></a>Instantiating Memory Pools Directly

For complex scenarios involing custom factories, it might be desirable to directly instantiate memory pools.  In this case, you just have to make sure to provide an IFactory<> derived class to be used for creating new instances and all the settings information that would normally be provided via the bind statements.  For example:

```csharp
public class BarFactory : IFactory<Bar>
{
    public Bar Create()
    {
        ...
        [Custom creation logic]
        ...
    }
}

var settings = new MemoryPoolSettings()
{
    InitialSize = 1,
    ExpandMethod = PoolExpandMethods.Double,
};

var pool = _container.Instantiate<MemoryPool<Bar>>(
    new object[] { settings, new MyBarFactory<Bar>() });
```

