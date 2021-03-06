#if !NOT_UNITY3D

using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using ModestTree;

namespace Zenject
{
    public enum LoadSceneRelationship
    {
        // This will use the ProjectContext container as parent for the new scene
        // This is similar to just running the new scene normally
        None,
        // This will use current scene as parent for the new scene
        // This will allow the new scene to refer to dependencies in the current scene
        Child,
        // This will use the parent of the current scene as the parent for the next scene
        // In most cases this will be the same as None
        Sibling,
    }

    public class ZenjectSceneLoader
    {
        readonly ProjectKernel _projectKernel;
        readonly DiContainer _sceneContainer;

        public ZenjectSceneLoader(
            SceneContext sceneRoot,
            ProjectKernel projectKernel)
        {
            _projectKernel = projectKernel;
            _sceneContainer = sceneRoot.Container;
        }

        public void LoadScene(string sceneName)
        {
            LoadScene(sceneName, LoadSceneMode.Single);
        }

        public void LoadScene(string sceneName, LoadSceneMode loadMode)
        {
            LoadScene(sceneName, loadMode, null);
        }

        public void LoadScene(
            string sceneName, LoadSceneMode loadMode, Action<DiContainer> extraBindings)
        {
            LoadScene(sceneName, loadMode, extraBindings, LoadSceneRelationship.None);
        }

        public void LoadScene(
            string sceneName,
            LoadSceneMode loadMode,
            Action<DiContainer> extraBindings,
            LoadSceneRelationship containerMode)
        {
            PrepareForLoadScene(loadMode, extraBindings, containerMode);

            Assert.That(Application.CanStreamedLevelBeLoaded(sceneName),
                "Unable to load scene '{0}'", sceneName);

            SceneManager.LoadScene(sceneName, loadMode);

            // It would be nice here to actually verify that the new scene has a SceneContext
            // if we have extra binding hooks, or LoadSceneRelationship != None, but
            // we can't do that in this case since the scene isn't loaded until the next frame
        }

        public AsyncOperation LoadSceneAsync(string sceneName)
        {
            return LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode loadMode)
        {
            return LoadSceneAsync(sceneName, loadMode, null);
        }

        public AsyncOperation LoadSceneAsync(
            string sceneName, LoadSceneMode loadMode, Action<DiContainer> extraBindings)
        {
            return LoadSceneAsync(sceneName, loadMode, extraBindings, LoadSceneRelationship.None);
        }

        public AsyncOperation LoadSceneAsync(
            string sceneName,
            LoadSceneMode loadMode,
            Action<DiContainer> extraBindings,
            LoadSceneRelationship containerMode)
        {
            PrepareForLoadScene(loadMode, extraBindings, containerMode);

            Assert.That(Application.CanStreamedLevelBeLoaded(sceneName),
                "Unable to load scene '{0}'", sceneName);

            return SceneManager.LoadSceneAsync(sceneName, loadMode);
        }

        void PrepareForLoadScene(
            LoadSceneMode loadMode,
            Action<DiContainer> extraBindings,
            LoadSceneRelationship containerMode)
        {
            if (loadMode == LoadSceneMode.Single)
            {
                Assert.IsEqual(containerMode, LoadSceneRelationship.None);

                // Here we explicitly unload all existing scenes rather than relying on Unity to
                // do this for us.  The reason we do this is to ensure a deterministic destruction
                // order for everything in the scene and in the container.
                // See comment at ProjectKernel.OnApplicationQuit for more details
                _projectKernel.ForceUnloadAllScenes();
            }

            if (containerMode == LoadSceneRelationship.None)
            {
                SceneContext.ParentContainers = null;
            }
            else if (containerMode == LoadSceneRelationship.Child)
            {
                SceneContext.ParentContainers = new DiContainer[] { _sceneContainer };
            }
            else
            {
                Assert.IsEqual(containerMode, LoadSceneRelationship.Sibling);
                SceneContext.ParentContainers = _sceneContainer.ParentContainers;
            }

            SceneContext.ExtraBindingsInstallMethod = extraBindings;
        }
    }
}

#endif
