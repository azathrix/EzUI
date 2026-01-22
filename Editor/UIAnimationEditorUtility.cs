using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.EzUI.Animations;
using UnityEditor;

namespace Azathrix.EzUI.Editor
{
    internal static class UIAnimationEditorUtility
    {
        public static List<Type> GetAnimationTypes()
        {
            var types = TypeCache.GetTypesDerivedFrom<UIAnimationComponent>();
            return types
                .Where(t => t != null && !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name)
                .ToList();
        }

        public static int FindTypeIndex(List<Type> types, Type currentType)
        {
            if (currentType == null)
                return 0;

            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] == currentType)
                    return i + 1;
            }

            return 0;
        }

        public static string[] BuildTypeDisplayNames(List<Type> types)
        {
            var names = new string[types.Count + 1];
            names[0] = "None";
            for (int i = 0; i < types.Count; i++)
                names[i + 1] = types[i].Name;
            return names;
        }
    }
}
