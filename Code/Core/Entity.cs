using System;
using System.Collections.Generic;

namespace SS15.Code.Core
{
    public class Entity
    {
        public int Id { get; set; }
        public Dictionary<Type, object> Components { get; } = new();

        public void SetComponent<T>(T component) where T : struct
        {
            Components[typeof(T)] = component;
        }

        public T GetComponent<T>() where T : struct
        {
            if (Components.TryGetValue(typeof(T), out var comp))
                return (T)comp;
            return default;
        }

        public bool HasComponent<T>() where T : struct => Components.ContainsKey(typeof(T));
    }
}