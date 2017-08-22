
## <a id="sub-containers-and-facades"></a>Sub-Containers And Facades

In some cases it can be very useful to use multiple containers in the same application.  For example, if you are creating a word processor it may be useful to have a sub-container for each tab that represents a separate document.  This way, you could bind a bunch of classes `AsSingle()` within the sub-container and they could all easily reference each other as if they were all singletons.  Then you could instantiate multiple sub-containers to be used for each document, with each sub-container having unique instances of all the classes that handle each specific document.

Another example might be if you are designing an open-world space ship game, you might want each space ship to have it's own container that contains all the class instances responsible for running that specific spaceship.

This is actually how ProjectContext bindings work.  There is one container for the entire project, and when a unity scene starts up, it container within each SceneContext is created "underneath" the ProjectContext container.  All the bindings that you add in your scene MonoInstaller are bound to your SceneContext container.  This allows the dependencies in your scene to automatically get injected with ProjectContext bindings, because sub-containers automatically inherit all the bindings in their parent (and grandparent, etc.).

A common design pattern that we like to use in relation to sub-containers is the <a href="https://en.wikipedia.org/wiki/Facade_pattern">Facade pattern</a>.  This pattern is used to abstract away a related group of dependencies so that it can be used at a higher level when used by other modules in the code base.  This is relevant here because often when you are defining sub-containers in your application it is very useful to also define a Facade class that is used to interact with this sub-container as a whole.  So, to apply it to the spaceship example above, you might have a SpaceshipFacade class that represents very high-level operations on a spaceship such as "Start Engine", "Take Damage", "Fly to destination", etc.  And then internally, the SpaceshipFacade class can delegate the specific handling of all the parts of these requests to the relevant single-responsibility dependencies that exist within the sub-container.

Let's do some examples in the following sections.

## <a id="hello-world-for-facades"></a>Hello World Example For Sub-Containers/Facade

```csharp
public class Greeter
{
    readonly string _message;

    public Greeter(string message)
    {
        _message = message;
    }

    public void DisplayGreeting()
    {
        Debug.Log(_message);
    }
}

public class GameController : IInitializable
{
    readonly Greeter _greeter;

    public GameController(Greeter greeter)
    {
        _greeter = greeter;
    }

    public void Initialize()
    {
        _greeter.DisplayGreeting();
    }
}

public class TestInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<GameController>().AsSingle();
        Container.Bind<Greeter>().FromSubContainerResolve().ByMethod(InstallGreeter).AsSingle();
    }

    void InstallGreeter(DiContainer subContainer)
    {
        subContainer.Bind<Greeter>().AsSingle();
        subContainer.BindInstance("Hello world!");
    }
}
```

The important thing to understand here is that any bindings that we add inside the `InstallGreeter` method will only be visible to objects within this sub-container.  The only exception is the Facade class (in this case, Greeter) since it is bound to the parent container using the FromSubContainerResolve binding.  In other words, in this example, the string "Hello World" is only visible by the Greeter class.

Note that you should always add a bind statement for whatever class is given to FromSubContainerResolve within the sub-container install method - otherwise you'll get exceptions and validation will fail.

Note also that unlike the above example, it is often better to use `ByInstaller` instead of `ByMethod`.  This is because when you use `ByMethod` it is easy to accidentally reference the Container instead of the subContainer.  Also, by using `ByInstaller` you can pass arguments into the Installer itself.

## <a id="using-tickables-within-sub-containers"></a>Using IInitializable / ITickable / IDisposable within Sub-Containers

One issue with the Hello World example above is that if I wanted to add some ITickable's or IInitializable's or IDisposable's to my sub-container it would not work.  For example, I might try doing this:

```csharp
public class GoodbyeHandler : IDisposable
{
    public void Dispose()
    {
        Log.Trace("Goodbye World!");
    }
}

public class HelloHandler : IInitializable
{
    public void Initialize()
    {
        Log.Trace("Hello world!");
    }
}

public class Greeter
{
    public Greeter()
    {
        Debug.Log("Created Greeter!");
    }
}

public class TestInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<Greeter>().FromSubContainerResolve().ByMethod(InstallGreeter).AsSingle().NonLazy();
    }

    void InstallGreeter(DiContainer subContainer)
    {
        subContainer.Bind<Greeter>().AsSingle();

        subContainer.BindInterfacesTo<GoodbyeHandler>().AsSingle();
        subContainer.BindInterfacesTo<HelloHandler>().AsSingle();
    }
}
```

However, while we will find that our `Greeter` class is created (due to the fact we're using `NonLazy`) and the text "Created Greeter!" is printed to the console, the Hello and Goodbye messages are not.  To get this working we need to change it to the following:

```csharp
public class GoodbyeHandler : IDisposable
{
    public void Dispose()
    {
        Debug.Log("Goodbye World!");
    }
}

public class HelloHandler : IInitializable
{
    public void Initialize()
    {
        Debug.Log("Hello world!");
    }
}

public class Greeter : Kernel
{
    public Greeter()
    {
        Debug.Log("Created Greeter");
    }
}

public class TestInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<Greeter>()
            .FromSubContainerResolve().ByMethod(InstallGreeter).AsSingle().NonLazy();
    }

    void InstallGreeter(DiContainer subContainer)
    {
        subContainer.Bind<Greeter>().AsSingle();

        subContainer.BindInterfacesTo<GoodbyeHandler>().AsSingle();
        subContainer.BindInterfacesTo<HelloHandler>().AsSingle();
    }
}

```

Now if we run it, we should see the Hello message, then if we stop playing we should see the Goodbye message.

The reason this works is because we are now binding IInitializable, IDisposable, and ITickable on the root container to the Greeter class given by `Container.BindInterfacesAndSelfTo<Greeter>()`.  Greeter now inherits from Kernel, which inherits from all these interfaces and also handles forwarding these calls to the IInitializable's / ITickable's / IDisposable's in our sub container.  Note that we use AsSingle() here, which is important otherwise it will create a new sub-container for every interface which is not what we want.

## <a id="using-game-object-contexts"></a>Creating Sub-Containers on GameObject's by using Game Object Context

Another issue with the <a href="#hello-world-for-facades">sub-container hello world example</a> above is that it does not work very well for MonoBehaviour classes.  There is nothing preventing us from adding MonoBehaviour bindings such as FromComponentInNewPrefab, FromNewComponentOnNewGameObject, etc. to our sub-container, however these will cause these new game objects to be added to the root of the scene heirarchy, so we'll have to manually track the lifetime of these objects ourselves by calling GameObject.Destroy on them when the Facade is destroyed.  Also, there is no way to have GameObject's that exist in our scene at the start but also exist within our sub-container.  These problems can be solved by using Game Object Context.

For this example, let's try to actually implement something similar to the open world space ship game described in <a href="#sub-containers-and-facades">the sub-container introduction</a>:

* Create a new scene
* Add the following files to your project:

```csharp
using Zenject;
using UnityEngine;

public class Ship : MonoBehaviour
{
    ShipHealthHandler _healthHandler;

    [Inject]
    public void Construct(ShipHealthHandler healthHandler)
    {
        _healthHandler = healthHandler;
    }

    public void TakeDamage(float damage)
    {
        _healthHandler.TakeDamage(damage);
    }
}
```

```csharp
using UnityEngine;
using Zenject;

public class GameRunner : ITickable
{
    readonly Ship _ship;

    public GameRunner(Ship ship)
    {
        _ship = ship;
    }

    public void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _ship.TakeDamage(10);
        }
    }
}
```

```csharp
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<GameRunner>().AsSingle();
    }
}
```

```csharp
using Zenject;
using UnityEngine;

public class ShipHealthHandler : MonoBehaviour
{
    float _health = 100;

    public void OnGUI()
    {
        GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 200, 100), "Health: " + _health);
    }

    public void TakeDamage(float damage)
    {
        _health -= damage;
    }
}
```

```csharp
using UnityEngine;
using System.Collections;

public class ShipInputHandler : MonoBehaviour
{
    [SerializeField]
    float _speed = 2;

    public void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            this.transform.position += Vector3.forward * _speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.position -= Vector3.forward * _speed * Time.deltaTime;
        }
    }
}
```

* Right Click inside the Hierarchy tab and select `Zenject -> Scene Context`
* Drag the GameInstaller class to the SceneContext game object
* Add a new row to the Installers property of the SceneContext
* Drag the GameInstaller component to the new row under Installers
* Right Click again inside the Hierarchy tab and select `Zenject -> Game Object Context`
* Rename the new game object GameObjectContext to Ship
* Drag the Ship MonoBehaviour to the Ship GameObject in our Scene. The Ship class will be used as a <a href="https://en.wikipedia.org/wiki/Facade_pattern">Facade</a> for our ship and will be used by other systems to interact with the ship at a high level
* Also add the `ShipInputHandler` component to the Ship game object
* Right click on the Ship GameObject and select 3D Object -> Cube.  This will serve as the placeholder model for our ship.
* Add new game object under ship called HealthHandler, and add the `ShipHealthHandler` component to it
* Your scene should look like this:

<img src="../UnityProject/Assets/Plugins/Zenject/Documentation/ShipFacadeExample1.png?raw=true" alt="Ship Facade Example"/>

* The idea here is that everything at or underneath the Ship game object should be considered inside it's own sub-container.  When we're done, we should be able to add multiple ships to our scene, each with their own components ShipHealthHandler, ShipInputHandler, etc. that can treat each other as singletons.
* Try to validate your scene by pressing CTRL+SHIFT+V.  You should get an error that looks like this: `Unable to resolve type 'ShipHealthHandler' while building object with type 'Ship'.`
* This is because the ShipHealthHandler component has not been added to our sub-container.  To address this:
    * Click on the HealthHandler game object and then click Add Component and type Zenject Binding (if you don't know what that is read the scene bindings section on the main page)
    * Drag the Ship Health Handler Component to the Components field of Zenject Binding
* Validate again by pressing CTRL+SHIFT+V.  You should now get this error instead: `Unable to resolve type 'Ship' while building object with type 'GameRunner'.` 
* Our Ship component also needs to be added to the container.  To address this, once again:
    * Click on the Ship game object and then click Add Component and type Zenject Binding
    * Drag the Ship Component to the Components field of Zenject Binding
* If we attempt to validate again you should notice the same error occurs.  This is because by default, ZenjectBinding only adds its components to the nearest container - in this case Ship.  This is not what we want though.  We want Ship to be added to the scene container because we want to use it as the Facade for our sub-container.  To address this we can explicitly tell ZenjectBinding which context to apply the binding to by dragging the SceneContext game object and dropping it on to the Context property of the Zenject binding
* Validation should now pass succesfully.
* If you run the scene now, you should see a health display in the middle of the screen, and you should be able to press Space bar to apply damage, and the up/down arrows to move the ship

Also note that we can add installers to our ship sub-container in the same way that we add installers to our Scene Context - just by dropping them into the Installers property of GameObjectContext.  In this example we used MonoBehaviour's for everything but we can also add however many plain C# classes we want here and have those classes available everywhere in the sub-container just like we do here for MonoBehaviour's by using ZenjectBinding.

## <a id="dynamic-game-object-contexts"></a>Creating Game Object Context's Dynamically

Continuing with the ship example <a href="#using-game-object-contexts">above</a>, let's pretend that we now want to create ships dynamically, after the game has started.

* First, create a prefab for the entire `Ship` GameObject that we created above and then delete it from the Scene.
* Then just add the following changes

```csharp
public class GameRunner : ITickable
{
    readonly Ship.Factory _shipFactory;

    Vector3 lastShipPosition;

    public GameRunner(Ship.Factory shipFactory)
    {
        _shipFactory = shipFactory;
    }

    public void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var ship = _shipFactory.Create();
            ship.transform.position = lastShipPosition;

            lastShipPosition += Vector3.forward * 2;
        }
    }
}
```

```csharp
public class GameInstaller : MonoInstaller
{
    [SerializeField]
    GameObject ShipPrefab;

    public override void InstallBindings()
    {
        Container.BindInterfacesTo<GameRunner>().AsSingle();

        Container.BindFactory<Ship, Ship.Factory>().FromSubContainerResolve().ByNewPrefab(ShipPrefab);
    }
}
```

* After doing this, make sure to drag and drop the newly created Ship prefab into the ShipPrefab property of GameInstaller in the inspector
* Now if we run our scene, we can hit Space to add multiple Ship's to our scene.

## <a id="dynamic-game-object-contexts"></a>Creating Game Object Context's Dynamically With Parameters

Let's make this even more interesting by passing a parameter into our ship facade.  Let's make the speed of the ship configurable from within the GameController class.

* Change our classes to the following:

```csharp
public class GameRunner : ITickable
{
    readonly Ship.Factory _shipFactory;

    Vector3 lastShipPosition;

    public GameRunner(Ship.Factory shipFactory)
    {
        _shipFactory = shipFactory;
    }

    public void Tick()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            var ship = _shipFactory.Create(Random.RandomRange(2, 20));
            ship.transform.position = lastShipPosition;

            lastShipPosition += Vector3.forward * 2;
        }
    }
}
```

```csharp
public class GameInstaller : MonoInstaller
{
    [SerializeField]
    GameObject ShipPrefab;

    public override void InstallBindings()
    {
        Container.BindInterfacesTo<GameRunner>().AsSingle();

        Container.BindFactory<float, Ship, Ship.Factory>().FromSubContainerResolve().ByNewPrefab<ShipInstaller>(ShipPrefab);
    }
}
```

```csharp
using Zenject;
using UnityEngine;

public class Ship : MonoBehaviour
{
    ShipHealthHandler _healthHandler;

    [Inject]
    public void Construct(ShipHealthHandler healthHandler)
    {
        _healthHandler = healthHandler;
    }

    public void TakeDamage(float damage)
    {
        _healthHandler.TakeDamage(damage);
    }

    public class Factory : Factory<float, Ship>
    {
    }
}
```

```csharp
using UnityEngine;
using System.Collections;
using Zenject;

public class ShipInputHandler : MonoBehaviour
{
    [Inject]
    float _speed;

    public void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            this.transform.position += Vector3.forward * _speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.position -= Vector3.forward * _speed * Time.deltaTime;
        }
    }
}
```

Also, add this new file:

```csharp
using System;
using Zenject;

public class ShipInstaller : MonoInstaller
{
    [Inject]
    float _speed;

    public override void InstallBindings()
    {
        Container.BindInstance(_speed).WhenInjectedInto<ShipInputHandler>();
    }
}
```

After that compiles, add ShipInstaller to the Ship prefab and also drag it to the Installers field of the GameObjectContext.

Note the changes that we made here:
- ShipInputHandler now has the speed injected instead of using Unity's SerializeField.
- The nested Factory class in Ship has a float parameter added to it
- In GameInstaller, the binding for the factory is different
- In GameRunner, we now need to pass a float parameter to the factory's create method

One important difference with creating a Sub-Container using a factory, is that the parameters you supply to the factory are not necessarily forwarded to the facade class.  In this example, the parameter is a float value for speed, which we want to forward to the ShipInputHandler class instead.  That is why these parameters are always forwarded to an installer for the sub-container, so that you can decide for yourself at install time what to do with the parameter.  Another reason for this is that in some cases the parameter might be used to choose different bindings.

You might also want to be able to drop Ship prefabs into the scene at design time.  This way, you can have some Ships that start in the scene, but you can also create them dynamically.  One way to do that would be to change ShipInstaller to the following:

```csharp
using System;
using Zenject;
using UnityEngine;

public class ShipInstaller : MonoInstaller
{
    [SerializeField]
    [InjectOptional]
    float _speed;

    public override void InstallBindings()
    {
        Container.BindInstance(_speed).WhenInjectedInto<ShipInputHandler>();
    }
}
```

This way, you can drop the Ship prefab into the scene and control the speed in the inspector, but you can also create them dynamically and pass the speed into the factory as a parameter.

For a more real-world example see the SpaceFighter sample project which makes heavy use of Game Object Contexts.

