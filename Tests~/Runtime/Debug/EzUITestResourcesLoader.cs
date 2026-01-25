#if UNITY_EDITOR
using System.Collections.Generic;
using Azathrix.Framework.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.EzUI.DebugTools
{
    /// <summary>
    /// In-memory resources loader for EzUI tests
    /// </summary>
    public sealed class EzUITestResourcesLoader : IResourcesLoader
    {
        private readonly Dictionary<string, Object> _assets = new Dictionary<string, Object>();

        public void Add(string path, Object asset)
        {
            if (string.IsNullOrWhiteSpace(path) || asset == null)
                return;
            _assets[path] = asset;
        }

        public T Load<T>(string key) where T : Object
        {
            if (_assets.TryGetValue(key, out var obj))
                return obj as T;
            return null;
        }

        public UniTask<T> LoadAsync<T>(string key) where T : Object
        {
            return UniTask.FromResult(Load<T>(key));
        }

        public UniTask LoadSceneAsync(string key, bool additive = false)
        {
            return UniTask.CompletedTask;
        }
    }
}
#endif
