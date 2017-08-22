
## <a id="release-notes"></a>Release Notes

###5.2.0 (April 30, 2017)

Minor release with just a few fixes.

- Fixed to compile again on WSA
- Fixed to be backwards compatible with Unity 5.5
- Removed MemoryPool DespawnAll() method because of its reliance on GetHashCode (issue #241)
- Added support for late install in decorator contexts
- Fixed to always trigger injection before the Awake event for MonoBehaviours attached to ProjectContext
- Added new bind method FromScriptableObjectResource which doesn't instantiate the scriptable object (issue #218)

###5.1.0 (April 3, 2017)

Notable
- Fixes related to upgrading to Unity 5.6
- Moved Zenject folder to plugins directory
- Changed to trigger injection before Awake / OnEnable / Start for dynamically instantaited prefabs. This is nice because you can treat [Inject] methods more like constructors, and then use Awake/Start for initialization logic, for both dynamically created objects and objects in the scene at the start
- Marked the [Inject] attribute with MeansImplicitUseAttribute to improve integration with JetBrains Resharper
- Fixed bug that was happening when using ZenjectSceneLoader with LoadSceneMode.Single (it was destroying ProjectContext)
- Added support for declaring [Inject] methods with return type IEnumerator similar to Start
- Changed UnderTransform bind method overload to accept InjectContext instead of just DiContainer to be consistent with the other action overloads
- Added new bind method FromComponentOn that takes an Action<> instead of a specific game object
- Changed to just always include SignalManager in the project context since this is where it should always be declared anyway
- Changed to require that all signal parameters be reference types when on IL2CPP platforms.  See docs for why this is necessary.
- Added new signal bind method that gets both parameters and a handler class (so you can perform an operation on the parameters before forwarding to the handler for example)

Minor
- Fixed bug where some fields marked as InjectOptional were still producing errors
- Changed to allow doing Bind<Transform>().FromNewComponentOnNewGameObject()
- [Memory pools] Minor change to allow specifying an explicit interface for the memory pool itself
- [Memory pools] Fixed bug where validation was failing
- [Memory pools] Added Listen and Unlisten methods to ISignal
- Changed Pause/Resume methods on TickableManager to be inherited from parents
- Added an option to exclude self (current object) in FromComponentInChildren and FromComponentInParents
- Fixed minor issue when using FromSubContainerResolve with factories

###5.0.2 (March 5, 2017)

- Fixed to allow parameterized tests using double parameters in ZenjectIntegrationTestFixtures
- Added another overload to BindMemoryPool to allow creating them directly without creating an empty subclass
- Changed memory pools to take an IFactory<> instead of a provider so that they can be instantiated directly by anyone that wants to do some custom stuff with it without needing to use BindMemoryPool
- Bug fix to validation for game object contexts
- Fixed script execution order to ensure that tickables, initializables, etc. are executed before MonoBehaviours in the scene (this is how it was in older versions)
- Changed to call IInitializable.Initialize immediately for GameObjextContext prefabs that are created dynamically. This is nice because otherwise, when you create a GameObjectContext via a factory, you can't use it immediately Unity waits until the end of the frame to call Start() to trigger Initialize
- Changed to use a runtime check inside profiler blocks to allow creating unit tests outside unity
- Fixed signals to validate properly
- Renamed FromScriptableObjectResource to FromNewScriptableObjectResource for consistency.
- Added a few missing factory bindings (FromComponentInHierarchy and FromNewScriptableObjectResource)
- Fixed signal installer bindings to work properly with AsTransient and multi-bindings

###5.0.1 (February 15, 2017)

- Hotfix.  Signal UniRx integration was completely broken

###5.0 (February 13, 2017)

Summary

Notable parts of this release includes the long awaited support for Memory Pools, a re-design of Commands/Signals, and support for late resolve via Lazy<> construct.  It also includes some API breaking changes to make it easier for new users.  Some of the bind methods were renamed to better represent what they mean, and in some cases the scope is now required to be made explicit, to avoid accidentally using transient scope.  Finally, there was also some significant performance improvements for when using Zenject in scenes with many transforms.

New Features
- Significant changes to commands and signals.  The functionality of commands was merged into Signals, and some more features were added to it to support subcontainers (see docs)
- Added Lazy<> construct so that you can have the resolve occur upon first usage
- Added menu option "Validate All Active Scenes"
- Added support for memory pools.  This includes a fluent interface similar to how factories work
- Added DiContainer.QueueForInject method to support adding pre-made instances to the initial inject list
- Added new construction methods
    - FromMethodMultiple
    - FromComponentInHierarchy
    - FromComponentSibling
    - FromComponentInParents
    - FromComponentInChildren 
    - FromScriptableObjectResource

Changes
- Updated sample projects to be easier to understand
- Improved error messages to include full type names
- Changed list bindings to default to optional so that you don't have to do this explicitly constantly
- Changed to require that the scope be explicitly set for some of the bind methods to avoid extremely common errors of accidentally leaving it as transient.  Bind methods that are more like "look ups" (eg. FromMethod, FromComponentInParents, etc.) have it as optional, however bind methods that create new instances require that it be set explicitly
- Renamed BindAllInterfaces to BindInterfacesTo and BindAllInterfacesAndSelf to BindInterfacesAndSelfTo to avoid the extremely common mistake of forgetting the To
- Removed support for passing arguments to InjectGameObject and InstantiatePrefab methods (issue #125)
- Removed UnityEventManager since it isn't core to keep things lightweight
- Renamed the Resolve overload that included an ID to ResolveId to avoid the ambiguity with the non generic version of Resolve
- Signals package received significant changes
    - The order of generic arguments to the Signal<> base class was changed to have parameters first to be consistent with everything else
    - The functionality of commands was merged into signals
- Renamed the following construction methods.  This was motivated by the fact that with the new construction methods it's unclear which ones are "look ups" versus creating new instances
    - FromComponent => FromNewComponentOn
    - FromSiblingComponent => FromNewComponentSibling 
    - FromGameObject => FromNewComponentOnNewGameObject 
    - FromPrefab => FromComponentInNewPrefab
    - FromPrefabResource => FromComponentInNewPrefabResource
    - FromSubContainerResolve.ByPrefab => FromSubContainerResolve.ByNewPrefab

Bug fixes
- (optimization) Fixed major performance issue for scenes that have a lot of transforms Re issue #188.
- (optimization) Fixed to avoid the extra performance costs of calling SetParent by directly passing the parent to the GameObject.Instantiate method issue #188
- Fixed extremely rare bug that would cause an infinite loop when using complex subcontainer setups
- Fixed to work with nunit test case attributes
- Fixed to instantiate prefabs without always changing them to be active
- Fixed WithArguments bind method to support passing null values
- Fixed context menu to work properly when creating installers etc. issue #200
- Fixed issue with ZenUtilInternal.InjectGameObjectForComponent method to support looking up non-monobehaviours.
- Fixed NonLazy() bind method to work properly wtihin sub containers

---------

###4.7 (November 6, 2016)
- Removed the concept of triggers in favour of just directly acting on the Signal to both subscribe and fire, since using Trigger was too much overhead for not enough gain
- Fixed issue for Windows Store platform where zenject was not properly stripping out the WSA generated constructors
- Changed to automatically choose the public constructor if faced with a choice between public and private
- Fix to IL2CPP builds to work again
- Added support for using the WithArguments bind method combined with FromFactory
- Improved validation of multi-scene setups using Contract Names to output better error messages

---------

###4.6 (October 23, 2016)
- Changed Validation to run at edit time rather than requiring that we enter play mode.  This is significantly faster.  Also added a hotkey to "validate then run" since it's fast enough to use as a pre-run check
- Added InstantiateComponentOnNewGameObject method
- Changed to install ScriptableObjectInstallers before MonoInstallers since it is common to include settings in ScriptableObjectInstallers (including settings for MonoInstallers)
- Added new option to ZenjectBinding BindType parameter to bind from the base class
- Changed to allow specifying singleton identifiers as object rather than just string
- Added design-time support to Scene Parenting by using Contract Names (see docs for details)
- Changed Scene Decorators to use Contract Names as well (see docs for details)
- Fixed to ensure that the order that initial instances on the container are injected in follows their dependency order #161
- Added LoadSceneAsync method to ZenjectSceneLoader class.  Also removed the option to pass in postBindings since nobody uses this and it's kind of bad practice anyway.  Also renamed LoadSceneContainerMode to LoadSceneRelationship
- Added AutoRun field on SceneContext for cases where you want to start it manually
- Removed the IBinder and IResolver interfaces since they weren't really used and were a maintenance headache
- Renamed WithGameObjectGroup to UnderTransformGroupX and also added UnderTransform method
- Added helper classes to make writing integration tests or unit tests with Unity's EditorTestRunner easier
- Added documentation on ZenjectEditorWindow, Unit Testing, and Integration Testing
- Misc. bug fixes

---------

###4.5 (September 1, 2016)
- Fixed DiContainer.ResolveTypeAll() method to properly search in parent containers
- Fixed exception that was occuring with Factories when using derived parameter types
- Fixed FromResolve to properly search in parent containers
- Fixed exception that was occuring with FromMethod when using derived parameter types

---------

###4.4 (July 23, 2016)
- Changed the way installers are called from other installers, to allow strongly typed parameter passing
- Added untyped version of FromMethod
- Added FromSiblingComponent bind method
- Added non-generic FromFactory bind method
- Minor bug fix to command binding to work with InheritInSubcontainers() method
- Bug fix - NonLazy method was not working properly when used with ByInstaller or ByMethod

---------

###4.3 (June 4, 2016)
- Changed to disallow using null with BindInstance by default, to catch these errors earlier
- Changed to use UnityEngine.Object when referring to prefabs to allow people to get some stronger type checking of prefabs at edit time
- (bug fix) for Hololens with Unity 5.4
- (bug fix) Scene decorator property was not being serialized correctly
- (bug fix) Custom factories were not validating in some cases

---------

###4.2 (May 30, 2016)
- Finally updated the documentation
- Renamed FromGetter to FromGetterResolve
- Added some optimizations to convention binding
- Renamed InstallPrefab to InstallPrefabResource
- (bug) Fixed PrefabFactory to work with abstract types
- (bug) Fixed some bugs related to convention binding
- (bug) Fixed bug with Unity 5.3.5 where the list of installers would not serialize properly
- (but) Fixed minor bug with validation

---------

###4.1 (May 15, 2016)
- Changed ResolveAll method to be optional by default, so it can return the empty list
- Removed Zenject.Commands namespace in favour of just Zenject
- Added convention based binding (eg. Container.Bind().To(x => x.AllTypes().DerivingFrom()))
- Fixed GameObjectCompositionRoot to expose an optional facade property in its inspector
- Renamed CompositionRoot to Context.
- Changed to just re-use the InjectAttribute instead of PostInjectAttribute
- Better support for making custom Unity EditorWindow implementations that use Zenject
- Added right click Menu items in project pane to create templates of common zenject C# files
- Renamed TestWithContainer to ZenjectUnitTestFixture
- Added simple test framework for both unit tests and integration tests
- Changed Identifier to be type object so it can be used with enums (or other types)
- Added InjectLocal attribute
- Changed to guarantee that any component that is injected into another component has itself been injected already
- Fixed an issue where calling Resolve<> or Instantiate<> inside an installer would cause objects to be injected twice
- Fixes to WSA platform
- Changed to automatically call ScriptableObject.CreateInstance when creating types that derive from ScriptableObject
- Fix to non-unity build

---------

###4.0 (April 30, 2016)
- Added another property to CompositionRoot to specify installers as prefabs re #96
- Changed global composition root to be a prefab instead of assembling together a bunch of ScriptableObject assets re #98
- Changed to lookup Zenject Auto Binding components by default, without the need for AutoBindInstaller. Also added new properties such as CompositionRoot, identifier, and made Component a list. Also works now when put underneath GameObjectCompositionRoot's.
- Added ability to pass in multiple types to the Bind() method. This opens up a lot of possibilities including convention-based binding. This also deprecated the use of BindAllInterfacesToSingle in favour of just BindAllInterfaces<>
- Added "WithArguments" bind method, to allow passing arguments directly to the type instead of always using WhenInjectedInto
- Added concept of EditorWindowCompositionRoot to make it easier to use Zenject with editor plugins
- Added "InheritInSubContainers" bind method, to allow having bindings automatically forwarded to sub containers
- Removed the different Exception classes in favour of just one (ZenjectException)
- Added 'AsCached' method as an alternative to 'AsSingle' and 'AsTransient'. AsCached will function like AsTransient except it will only create the object once and thereafter return that value
- Changed some methods that previously used 'params' to explicitly take a list, to avoid common errors
- Cleaned up InjectContext to be easier to work with
- Made significant change to how Factories work. Now there is just one definitive Factory class, and you can change how that factory constructs the object in your installers
- Changed the fluent interface to specify whether the binding is single or transient as a separate method, to avoid the explosion of ToSinglePrefab, ToTransientPrefab, etc. (now it's just ToPrefab)
- Renamed GlobalCompositionRoot to ProjectCompositionRoot and FacadeCompositionRoot to GameObjectCompositionRoot.
- Added more intuitive bindings for creating subcontainers. eg: Container.Bind().ToSubContainerResolve().ByPrefab()
- Added WithGameObjectName and WithGroupName bind methods to prefab or game object related bindings
- Made another big chagne to the fluent interface to avoid having duplicate methods for with Self and Concrete. Now you choose between Container.Bind().ToSelf() and Container.Bind().To(). ToSelf is assumed if unspecified
- Changed Triggers to directly expose the signal event so they can be used as if they are signals
- Added concept of ScriptableObjectInstaller - especially useful for use with settings
- Added ZenjectSceneLoader class to allow additively loading other scenes as children or siblings of existing scene
- Changed scene decorators to work more intuitively with the multi-scene editting features of Unity 5.3+. You can now drag in multiple scenes together, and as long as you use DecoratorCompositionRoot in scenes above a main scene, they will be loaded together.
- Removed IncludeInactive flag. Now always injects into inactive game objects. This was kinda necessary because validation needs to control the active flag
- Removed the concept of one single DependencyRoot in favour of having any number of them, using binding with identifier. Also added NonLazy() bind method to make this very easy
- Added new attribute ZenjectAllowDuringValidation for use with installer setting objects that you need during validation
- Changed validation to occur at runtime to be more robust and less hacky. Now works by adding dummy values to mark which dependencies have successfully been found
- Renamed BindPriority to BindExecutionOrder
- Removed support for binary version of Zenject. This was necessary since Zenject now needs to use some unity defines (eg. UNITY_EDITOR) which doesn't work in DLLs

---------

###3.11 (May 15, 2016)
- Bug fix - Calling Resolve<> or Instantiate<> inside an installer was causing the object to be injected twice
- Added StaticCompositionRoot as an even higher level container than ProjectCompositionRoot, for cases where you want to add dependencies directly to the Zenject assembly before Unity even starts up
- Bug fix - loading the same scene multiple times with LoadSceneAdditive was not working
- Fixed compiler errors with Unity 5.4

---------

###3.10 (March 26, 2016)
- Fixed to actually support Windows Store platform
- Added pause/resume methods to TickableManager
- Bug fix - OnlyInjectWhenActive flag did not work on root inactive game objects 

---------

###3.9 (Feb 7, 2016)
- Added a lot more error checking when using the ToSingle bindings. It will no longer allow mixing different ToSingle types
- Fixed ToSingleGameObject and ToSingleMonoBehaviour to allow multiple bindings to the same result
- Made it easier to construct SceneCompositionRoot objects dynamically
- Added untyped versions of BindIFactory.ToFactory method
- Removed the ability warn on missing ITickable/IInitializable bindings
- Added a bunch of integration tests
- Reorganized folder structure

---------

###3.8 (Feb 4, 2016)
- Changed back to only initializing the ProjectCompositionRoot when starting a scene with a SceneCompositionRoot rather than always starting it in every scene

---------

###3.7 (Jan 31, 2016)
- Changed to not bother parenting transforms to the CompositionRoot object by default (This is still optional with a checkbox however)
- Added string parameter to BindMonoBehaviourFactory method to allow specifying the name of an empty GameObject to use for organization
- Changed FacadeFactory to inherit from IFactory
- Changed ProjectCompositionRoot to initialize using Unity's new [RuntimeInitializeOnLoadMethod] attribute
- Added easier ability to validate specific scenes from the command line outside of Unity
- Added AutoBindInstaller class and ZenjectBinding attribute to make it easier to add MonoBehaviours that start in the scene to the container
- Added optional parameter to the [Inject] attribute to specify which container to retrieve from in the case of nested containers
- Fixed some unity-specific bind commands to play more nicely with interfaces

---------

###3.6 (Jan 24, 2016)
- Another change to signals to not require parameter types to the bind methods

---------

###3.5 (Jan 17, 2016)
- Made breaking change to require separate bind commands for signals and triggers, to allow adding different conditionals on each.

---------

###3.4 (Jan 7, 2016)
- Cleaned up directory structure
- Fixed bug with Global bindings not getting their Tick() called in the correct order
- Fixes to the releases automation scripts

---------

###3.2 (December 20, 2015)
- Added the concept of "Commands" and "Signals".  See documentation for details.
- Fixed validation for decorator scenes that open decorator scenes.
- Changed to be more strict when using a combination of differents kinds of ToSingle<>, since there should only be one way to create the singleton.
- Added ToSingleFactory bind method, for cases where you have complex construction requirements and don't want to use ToSingleMethod
- Removed the InjectFullScene flag on SceneCompositionRoot.  Now always injects on the full scene.
- Renamed AllowNullBindings to IsValidating so it can be used for other kinds of validation-only logic
- Renamed BinderUntyped to UntypedBinder and BinderGeneric to GenericBinder
- Added the ability to install MonoInstaller's directly from inside other installers by calling Container.Install<MyCustomMonoInstaller>().  In this case it tries to load a prefab from Resources/Installers/MyCustomMonoInstaller.prefab before giving up.  This can be helpful to keep scenes incredibly small instead of having many installer prefabs.
- Added the ability to install MonoInstaller's directly from inside other installers.  In this case it tries to load a prefab from the resources directory before giving up.
- Added some better error output in a few places
- Fixed some iOS AOT issues
- Added BindFacade<> method to DiContainer, to allow creating nested containers without needing to use a factory.
- Added an Open button in scene decorator comp root for easily jumping to the decorated scene
- Removed support for object graph visualization since I hadn't bothered maintaining it
- Got the optional Moq extension method ToMock() working again
- Fixed scene decorators to play more nicely with Unity's own way of handling LoadLevelAdditive.  Decorated scenes are now organized in the scene heirarchy under scene headings just like when calling LoadLevelAdditive normally

---------

###3.1
- Changes related to upgrading to Unity 5.3
- Fixed again to make zero heap allocations per frame

---------

###3.0
- Added much better support for nested containers.  It now works more closely to what you might expect:  Any parent dependencies are always inherited in sub-containers, even for optional injectables.  Also removed BindScope and FallbackContainer since these were really just workarounds for this feature missing.  Also added [InjectLocal] attribute for cases where you want to inject dependencies only from the local container.
- Changed the way execution order is specified in the installers.  Now the order for Initialize / Tick / Dispose are all given by one property similar to how unity does it, using ExecutionOrderInstaller
- Added ability to pass arguments to Container.Install<>
- Added support for using Facade pattern in combination with nested containers to allow easily created distinct 'islands' of dependencies.  See documentation for details
- Changed validation to be executed on DiContainer instead of through BindingValidator for ease of use
- Added automatic support for WebGL by marking constructors as [Inject]

---------

###2.8
* Fixed to properly use explicit default parameter values in Constructor/PostInject methods.  For eg: public Foo(int bar = 5) should consider bar to be optional and use 5 if not resolved.

---------

###2.7
* Bug fix to ensure global composition root always gets initialized before the scene composition root
* Changed scene decorators to use LoadLevelAdditive instead of LoadLevel to allow more complex setups involving potentially several decorators within decorators

---------

###2.6
* Added new bind methods: ToResource, ToTransientPrefabResource, ToSinglePrefabResource
* Added ability to have multiple sets of global installers
* Fixed support for using zenject with .NET 4.5
* Created abstract base class CompositionRoot for both SceneCompositionRoot and ProjectCompositionRoot
* Better support for using the same DiContainer from multiple threads
* Added back custom list inspector handler to make it easier to re-arrange etc.
* Removed the extension methods on DiContainer to avoid a gotcha that occurs when not including 'using Zenject
* Changed to allow having a null root transform given to DiContainer
* Changed to assume any parameters with hard coded default values (eg: int x = 5) are InjectOptional
* Fixed bug with asteroids project which was causing exceptions to be thrown on the second run due to the use of tags

---------

###2.5
* Added support for circular dependencies in the PostInject method or as fields (just not constructor parameters)
* Fixed issue with identifiers that was occurring when having both [Inject] and [InjectOptional] attributes on a field/constructor parameter.  Now requires that only one be set
* Removed BindValue in favour of just using Bind for both reference and value types for simplicity
* Removed GameObjectInstantiator class since it was pretty awkward and confusing.  Moved methods directly into IInstantiator/DiContainer.  See IInstantiator class.
* Extracted IResolver and IBinder interfaces from DiContainer

---------

###2.4
* Refactored the way IFactory is used to be a lot cleaner. It now uses a kind of fluent syntax through its own bind method BindIFactory<>

---------

###2.3
* Added "ParentContexts" property to InjectContext, to allow very complex conditional bindings that involve potentially several identifiers, etc.
* Removed InjectionHelper class and moved methods into DiContainer to simplify API and also to be more discoverable
* Added ability to build dlls for use in outside unity from the assembly build solution

---------

###2.2
* Changed the way installers invoke other installers.  Previously you would Bind them to IInstaller and now you call Container.Install<MyInstaller> instead.  This is better because it allows you to immediately call Rebind<> afterwards

---------

###2.1
* Simplified interface a bit more by moving more methods into DiContainer such as Inject and Instantiate.  Moved all helper methods into extension methods for readability. Deleted FieldsInjector and Instantiator classes as part of this
* Renamed DiContainer.To() method to ToInstance since I had witnessed some confusion with it for new users.  Did the same with ToSingleInstance
* Added support for using Zenject outside of Unity by building with the ZEN_NOT_UNITY3D define set
* Bug fix - Validation was not working in some cases for prefabs.
* Renamed some of the parameters in InjectContext for better understandability.
* Renamed DiContainer.ResolveMany to DiContainer.ResolveAll
* Added 'InjectFullScene' flag to CompositionRoot to allow injecting across the entire unity scene instead of just objects underneath the CompositionRoot

---------

###2.0
* Added ability to inject dependencies via parameters to the [PostInject] method just like it does with constructors.  Especially useful for MonoBehaviours.
* Fixed the order that [PostInject] methods are called in for prefabs
* Changed singletons created via ToSinglePrefab to identify based on identifier and prefab and not component type. This allows things like ToSingle<Foo>(prefab1) and ToSingle<Bar>(prefab1) to use the same prefab, so you can map singletons to multiple components on the same prefab. This also works with interfaces.
* Removed '.As()' method in favour of specifying the identifier in the first Bind() statement
* Changed identifiers to be strings instead of object to avoid accidental usage
* Renamed ToSingle(obj) to ToSingleInstance to avoid conflict with specifying an identifier
* Fixed validation to work properly for ToSinglePrefab
* Changed to allow using conditions to override a default binding. When multiple providers are found it will now try and use the one with conditions.  So for example you can define a default with `Container.Bind<IFoo>().ToSingle<Foo1>()` and then override for specific classes with `Container.Bind<IFoo>().ToSingle<Foo2>().WhenInjectedInto<Bar>()`, etc.

---------

###1.19

* Upgraded to Unity 5
* Added an optional identifier to InjectOptional attribute
* Changed the way priorities are interpreted for tickables, disposables, etc. Zero is now used as default for any unspecified priorities.  This is helpful because it allows you to choose priorities that occur either before or after the unspecified priorities.
* Added some helper methods to ZenEditorUtil for use by CI servers to validate all scenes

---------

###1.18

* Added minor optimizations to reduce per-frame allocation to zero
* Fixed unit tests to be compatible with unity test tools
* Minor bug fix with scene decorators, GameObjectInstantiator.

---------

###1.17

* Bug fix.  Was not forwarding parameters correctly when instantiating objects from prefabs

---------

###1.16

* Removed the word 'ModestTree' from namespaces since Zenject is open source and not proprietary to the company ModestTree.

---------

###1.15

* Fixed bug with ToSinglePrefab which was causing it to create multiple instances when used in different bindings.

---------

###1.14

* Added flag to CompositionRoot for whether to inject into inactive game objects or ignore them completely
* Added BindAllInterfacesToSingle method to DiContainer
* Changed to call PostInject[] on children first when instantiating from prefab
* Added ILateTickable interface, which works just like ITickable or IFixedTickable for unity's LateUpdate event
* Added support for 'decorators', which can be used to add dependencies to another scene

---------

###1.13

* Minor bug fix to global composition root.  Also fixed a few compiler warnings.

---------

###1.12

* Added Rebind<> method
* Changed Factories to use strongly typed parameters by default.  Also added ability to pass in null values as arguments as well as multiple instances of the same type
* Renamed _container to Container in the installers
* Added support for Global Composition Root to allow project-wide installers/bindings
* Added DiContainer.ToSingleMonoBehaviour method
* Changed to always include the StandardUnityInstaller in the CompositionRoot class.
* Changed TickableManager to not be a monobehaviour and receive its update from the UnityDependencyRoot instead
* Added IFixedTickable class to support unity FixedUpdate method

---------

###1.11

* Removed Fasterflect library to keep Zenject nice and lightweight (it was also causing issues on WP8)
* Fixed bug related to singletons + object graph validation. Changed the way IDisposables are handled to be closer to the way IInitializable and ITickable are handled. Added method to BinderUntyped.

---------

###1.10

* Added custom editor for the Installers property of CompositionRoot to make re-ordering easier

---------

###1.09

* Added support for nested containers
* Added ability to execute bind commands using Type objects rather than a generic type
* Changed the way IDisposable bindings work to be similar to how ITickable and IInitializable work
* Bug fixes

---------

###1.08

* Order of magnitude speed improvement by using more caching
* Minor change to API to use the As() method to specify identifiers
* Bug fixes

---------

###1.07

* Simplified API by removing the concept of modules in favour of just having installers instead (and add support for installers installing other installers)
* Bug fixes

---------

###1.06

* Introduced concept of scene installer, renamed installers 'modules'
* Bug fixes
