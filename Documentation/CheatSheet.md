
## <a id="cheatsheet"></a>Installers Cheat-Sheet

Below are a bunch of randomly assorted examples of bindings that you might include in one of your installers.

For more examples, you may also be interested in reading some of the Unit tests (see `Zenject/OptionalExtras/UnitTests` and `Zenject/OptionalExtras/IntegrationTests` directories)

```csharp

// Create a new instance of Foo for every class that asks for it
Container.Bind<Foo>().AsTransient();

// Create a new instance of Foo for every class that asks for an IFoo
Container.Bind<IFoo>().To<Foo>().AsTransient();

// Non generic version
Container.Bind(typeof(IFoo)).To(typeof(Foo)).AsTransient();

///////////// AsSingle

// Create one definitive instance of Foo and re-use that for every class that asks for it
Container.Bind<Foo>().AsSingle();

// Create one definitive instance of Foo and re-use that for every class that asks for IFoo
Container.Bind<IFoo>().To<Foo>().AsSingle();

// In this example, the same instance of Foo will be used for all three cases
Container.Bind<Foo>().AsSingle();
Container.Bind<IFoo>().To<Foo>().AsSingle();
Container.Bind<IFoo2>().To<Foo>().AsSingle();

// Non generic versions
Container.Bind(typeof(Foo)).AsSingle();
Container.Bind(typeof(IFoo)).To(typeof(Foo)).AsSingle();

// Or, as one bind statement
Container.Bind(typeof(Foo), typeof(IFoo)).To(typeof(Foo)).AsSingle();

///////////// BindInterfaces

// Bind all interfaces that Foo implements to a new singleton of type Foo
Container.BindInterfacesTo<Foo>().AsSingle();

// So for example if Foo implements ITickable and IInitializable then the above
// line is equivalent to this:
Container.Bind<ITickable>().To<Foo>().AsSingle();
Container.Bind<IInitializable>().To<Foo>().AsSingle();

///////////// FromInstance

// Use the given instance everywhere that Foo is used
Container.Bind<Foo>().FromInstance(new Foo());

// This is simply a shortcut for the above binding
// This can be a bit nicer since the type argument can be deduced from the parameter
Container.BindInstance(new Foo());

// Note that FromInstance is different from AsSingle because it does allow multiple bindings
// and you can't re-use the same instance in multiple bindings like you can with AsSingle
// For example, the following is allowed and will match any constructor parameters of type List<Foo>
// (and throw an exception for parameters that ask for a single Foo)
Container.Bind<Foo>().FromInstance(new Foo());
Container.Bind<Foo>().FromInstance(new Foo());

///////////// Binding primitive types

// Use the number 10 every time an int is requested
// You'd never really want to do this, you should almost always use a When condition for primitive values (see conditions section below)
Container.Bind<int>().FromInstance(10);
Container.Bind<bool>().FromInstance(false);

// These are the same as above
// This can be a bit nicer though since the type argument can be deduced from the parameter
// Again though, be careful to use conditions to limit the scope of usage for values
// or consider using a Settings object as described above
Container.BindInstance(10);
Container.BindInstance(false);

///////////// FromMethod

// Create instance of Foo when requested, using the given method
// Note that for more complex construction scenarios, you might consider using a factory
// instead
Container.Bind<Foo>().FromMethod(GetFoo);

Foo GetFoo(InjectContext ctx)
{
    return new Foo();
}

// Randomly return one of several different implementations of IFoo
// We use Instantiate here instead of just new so that Foo1 gets its members injected
Container.Bind<IFoo>().FromMethod(GetFoo);

IFoo GetFoo(InjectContext ctx)
{
    switch (UnityEngine.Random.Range(0, 3))
    {
        case 0:
            return ctx.Container.Instantiate<Foo1>();

        case 1:
            return ctx.Container.Instantiate<Foo2>();
    }

    return ctx.Container.Instantiate<Foo3>();
}

// Using lambda syntax
Container.Bind<Foo>().FromMethod((ctx) => new Foo());

// This is equivalent to AsTransient
Container.Bind<Foo>().FromMethod((ctx) => ctx.Container.Instantiate<Foo>());

///////////// FromResolveGetter

// Bind to a property on another dependency
// This can be helpful to reduce coupling between classes
Container.Bind<Foo>().AsSingle();

Container.Bind<Bar>().FromResolveGetter<Foo>(foo => foo.GetBar());

// Another example using values
Container.Bind<string>().FromResolveGetter<Foo>(foo => foo.GetTitle());

///////////// FromNewComponentOnNewGameObject (singleton)

// Create a new game object at the root of the scene, add the Foo MonoBehaviour to it, and name it "Foo"
Container.Bind<Foo>().FromNewComponentOnNewGameObject().AsSingle();

// You can also specify the game object name to use using WithGameObjectName
Container.Bind<Foo>().FromNewComponentOnNewGameObject().WithGameObjectName("Foo1").AsSingle();

// Bind to an interface instead
Container.Bind<IFoo>().To<Foo>().FromNewComponentOnNewGameObject().AsSingle();

///////////// FromComponentInNewPrefab (singleton)

// Create a new game object at the root of the scene using the given prefab
// It is assumed that the Foo is a MonoBehaviour here and that Foo has been
// previously added to the prefab
// After zenject creates a new GameObject from the given prefab, it will
// search the prefab for a component of type 'Foo' and return that
GameObject fooPrefab;
Container.Bind<Foo>().FromComponentInNewPrefab(fooPrefab).AsSingle();

// Bind to interface instead
Container.Bind<IFoo>().To<Foo>().FromComponentInNewPrefab(fooPrefab).AsSingle();

// In this example we use AsSingle but with different components
// Note here that only one instance of the given prefab will be
// created.  The AsSingle applies to the prefab itself and not to
// the type that is being returned from the prefab.
// For this to work, there must be both a Foo MonoBehaviour and
// a Bar MonoBehaviour somewhere on the prefab
GameObject prefab;
Container.Bind<Foo>().FromComponentInNewPrefab(prefab).AsSingle();
Container.Bind<Bar>().FromComponentInNewPrefab(prefab).AsSingle();

///////////// FromComponentInNewPrefab (Transient)

// Instantiate a new copy of 'fooPrefab' every time an instance of Foo is
// requested by a constructor parameter, injected field, etc.
GameObject fooPrefab = null;
Container.Bind<Foo>().FromComponentInNewPrefab(fooPrefab);

// Again, this is equivalent since AsTransient is the default
Container.Bind<Foo>().FromComponentInNewPrefab(fooPrefab).AsTransient();

// Bind to interface instead
Container.Bind<IFoo>().To<Foo>().FromComponentInNewPrefab(fooPrefab);

///////////// Identifiers

// Bind a globally accessible string with the name 'PlayerName'
// Note however that a better option might be to create a Settings object and bind
// that instead
Container.Bind<string>().WithId("PlayerName").FromInstance("name of the player");

// This is the equivalent of the line above, and is a bit more readable
Container.BindInstance("name of the player").WithId("PlayerName");

// We can also use IDs to bind multiple instances of the same type:
Container.Bind<string>().WithId("FooA").FromInstance("foo");
Container.Bind<string>().WithId("FooB").FromInstance("asdf");

// Then when we inject these dependencies we have to use the same ID:
public class Norf
{
    [Inject(Id = "FooA")]
    string _foo;
}

public class Qux
{
    [Inject(Id = "FooB")]
    string _foo;
}

// In this example, we bind three instances of Foo, including one without an ID
Container.Bind<Foo>().AsCached();
Container.Bind<Foo>().WithId("FooA").AsCached();
Container.Bind<Foo>().WithId("FooA").AsCached();

// When an ID is unspecified in an [Inject] field, it will use the first
// instance
// Bindings without IDs can therefore be used as a default and we can
// specify IDs for specific versions of the same type
public class Norf
{
    [Inject]
    Foo _foo;
}

// Qux._foo will be the same instance as Norf._foo
// This is because we are using AsCached rather than AsTransient
// Note here that we don't want to use AsSingle since in that case
// Qux._foo2 will also use the same instance
public class Qux
{
    [Inject]
    Foo _foo;

    [Inject(Id = "FooA")]
    Foo _foo2;
}

///////////// Conditions

// This will only allow Bar to depend on Foo
// If we add Foo to the constructor of any other class it won't find it
Container.Bind<Foo>().AsSingle().WhenInjectedInto<Bar>();

// Use different implementations of IFoo dependending on which
// class is being injected
Container.Bind<IFoo>().To<Foo1>().AsSingle().WhenInjectedInto<Bar>();
Container.Bind<IFoo>().To<Foo2>().AsSingle().WhenInjectedInto<Qux>();

// Use "Foo1" as the default implementation except when injecting into
// class Qux, in which case use Foo2
Container.Bind<IFoo>().To<Foo1>().AsSingle();
Container.Bind<IFoo>().To<Foo2>().AsSingle().WhenInjectedInto<Qux>();

// Allow depending on Foo in only a few select classes
Container.Bind<Foo>().AsSingle().WhenInjectedInto(typeof(Bar), typeof(Qux), typeof(Baz));

// Supply "my game" for any strings that are injected into the Gui class with the identifier "Title"
Container.BindInstance("my game").WithId("Title").WhenInjectedInto<Gui>();

// Supply 5 for all ints that are injected into the Gui class
Container.BindInstance(5).WhenInjectedInto<Gui>();

// Supply 5 for all ints that are injected into a parameter or field
// inside type Gui that is named 'width'
// This is usually not a good idea since the name of a field can change
// easily and break the binding but shown here as an example  :)
Container.BindInstance(5.0f).When(ctx =>
    ctx.ObjectType == typeof(Gui) && ctx.MemberName == "width");

// Create a new 'Foo' for every class that is created as part of the
// construction of the 'Bar' class
// So if Bar has a constructor parameter of type Qux, and Qux has
// a constructor parameter of type IFoo, a new Foo will be created
// for that case
Container.Bind<IFoo>().To<Foo>().AsTransient().When(
    ctx => ctx.AllObjectTypes.Contains(typeof(Bar)));

///////////// Complex conditions example

var foo1 = new Foo();
var foo2 = new Foo();

Container.Bind<Bar>().WithId("Bar1").AsTransient();
Container.Bind<Bar>().WithId("Bar2").AsTransient();

// Here we use the 'ParentContexts' property of inject context to sync multiple corresponding identifiers
Container.BindInstance(foo1).When(c => c.ParentContexts.Where(x => x.MemberType == typeof(Bar) && x.Identifier == "Bar1").Any());
Container.BindInstance(foo2).When(c => c.ParentContexts.Where(x => x.MemberType == typeof(Bar) && x.Identifier == "Bar2").Any());

// This results in:
// Container.Resolve<Bar>("Bar1").Foo == foo1
// Container.Resolve<Bar>("Bar2").Foo == foo2

///////////// FromResolve

// This will result in IBar, IFoo, and Foo, all being bound to the same instance of
// Foo which is assume to exist somewhere on the given prefab
GameObject fooPrefab;
Container.Bind<Foo>().FromComponentInNewPrefab(fooPrefab).AsSingle();
Container.Bind<IBar>().To<Foo>().FromResolve();
Container.Bind<IFoo>().To<IBar>().FromResolve();

// This will result in the same behaviour as the above
GameObject fooPrefab = null;
Container.Bind<Foo>().FromComponentInNewPrefab(fooPrefab).AsSingle();
Container.Bind<IBar>().To<Foo>().FromComponentInNewPrefab(fooPrefab).AsSingle();
Container.Bind<IFoo>().To<Foo>().FromComponentInNewPrefab(fooPrefab).AsSingle();

///////////// Rebind

// Rebind can be used to override previous bindings
// This will result in IFoo being bound to only Bar
// The binding to Foo will have been removed
// Normally the order that the bindings occur in doesn't
// matter at all, but Rebind does create an order-dependency
// so use with caution
Container.Bind<IFoo>().To<Foo>().AsSingle();
Container.Rebind<IFoo>().To<Bar>().AsSingle();

///////////// Installing Other Installers

// Immediately call InstallBindings() on FooInstaller
FooInstaller.Install(Container);

// Before calling FooInstaller, configure a property of it
Container.BindInstance("foo").WhenInjectedInto<FooInstaller>();
FooInstaller.Install(Container);

// We can also pass arguments directly
// This line is equivalent to the above two lines
FooInstaller.Install(Container, new object[] { "foo" });

// After calling FooInstaller, override one of its bindings
// We assume here that FooInstaller binds IFoo to something
FooInstaller.Install(Container);
Container.Rebind<IFoo>().To<Bar>().AsSingle();

///////////// Manual Use of Container

// This will fill in any parameters marked as [Inject] and also call any [Inject] methods
var foo = new Foo();
Container.Inject(foo);

// Return an instance for IFoo, using the bindings that have been added previously
// Internally it is what is triggered when you fill in a constructor parameter of type IFoo
// Note: It will throw an exception if it cannot find a match
Container.Resolve<IFoo>();

// Same as the above except returns null when it can't find the given type
Container.TryResolve<IFoo>();

// Return a list of 2 instances of type Foo
Container.BindInstance(new Foo());
Container.BindInstance(new Foo());
var foos = Container.ResolveAll<IFoo>();

// Create a new instance of Foo and inject on any of its members
// And fill in any constructor parameters Foo might have
Container.Instantiate<Foo>();

GameObject prefab = null;
// Instantiate a new prefab and have any injectables filled in on the prefab
GameObject go = Container.InstantiatePrefab(prefab);

// Instantiate a new prefab and return a specific monobehaviour
Foo foo2 = Container.InstantiatePrefabForComponent<Foo>(prefab);

// Add a new component to an existing game object
Foo foo3 = Container.InstantiateComponent<Foo>(gameObject);

```

