using System;
using System.Collections.Generic;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Utilities
{
    /// <summary>
    /// A set of utilities for converting generic types between each other.
    /// </summary>
    public static class Conversion
    {
        /// <summary>
        /// Converts a value to the requested type -- coercing primitives to
        /// the desired type, wrapping lists and dictionaries appropriately,
        /// or else returning null.
        ///
        /// This should be used on any containers that might be coming from a
        /// user to normalize the collection types. Collection types coming from
        /// JSON deserialization can be safely assumed to be lists or dictionaries of
        /// objects.
        /// </summary>
        public static T As<T>(object value) where T : class
        {
            return ConvertTo<T>(value) as T;
        }

        /// <summary>
        /// Converts a value to the requested type -- coercing primitives to
        /// the desired type, wrapping lists and dictionaries appropriately,
        /// or else throwing an exception.
        ///
        /// This should be used on any containers that might be coming from a
        /// user to normalize the collection types. Collection types coming from
        /// JSON deserialization can be safely assumed to be lists or dictionaries of
        /// objects.
        /// </summary>
        public static T To<T>(object value)
        {
            return (T)ConvertTo<T>(value);
        }

        /// <summary>
        /// Converts a value to the requested type -- coercing primitives to
        /// the desired type, wrapping lists and dictionaries appropriately,
        /// or else passing the object along to the caller unchanged.
        ///
        /// This should be used on any containers that might be coming from a
        /// user to normalize the collection types. Collection types coming from
        /// JSON deserialization can be safely assumed to be lists or dictionaries of
        /// objects.
        /// </summary>
        internal static object ConvertTo<T>(object value)
        {
            if (value is T || value == null)
            {
                return value;
            }

            if (ReflectionHelpers.IsPrimitive(typeof(T)))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return value;
        }

        /// <summary>
        /// Holds a dictionary that maps a cache of interface types for related concrete types.
        /// The lookup is slow the first time for each type because it has to enumerate all interface
        /// on the object type, but made fast by the cache.
        ///
        /// The map is:
        ///    (object type, generic interface type) => constructed generic type
        /// </summary>
        private static readonly Dictionary<Tuple<Type, Type>, Type> interfaceLookupCache =
            new Dictionary<Tuple<Type, Type>, Type>();
        private static Type GetInterfaceType(Type objType, Type genericInterfaceType)
        {
            // Side note: It so sucks to have to do this. What a piece of crap bit of code
            // Unfortunately, .NET doesn't provide any of the right hooks to do this for you
            // *sigh*
            if (ReflectionHelpers.IsConstructedGenericType(genericInterfaceType))
            {
                genericInterfaceType = genericInterfaceType.GetGenericTypeDefinition();
            }
            var cacheKey = new Tuple<Type, Type>(objType, genericInterfaceType);
            if (interfaceLookupCache.ContainsKey(cacheKey))
            {
                return interfaceLookupCache[cacheKey];
            }
            foreach (var type in ReflectionHelpers.GetInterfaces(objType))
            {
                if (ReflectionHelpers.IsConstructedGenericType(type) &&
                    type.GetGenericTypeDefinition() == genericInterfaceType)
                {
                    return interfaceLookupCache[cacheKey] = type;
                }
            }
            return null;
        }
    }
}
