﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MoonSharp.Interpreter.DataStructs;
using MoonSharp.Interpreter.Interop.BasicDescriptors;

namespace MoonSharp.Interpreter.Interop.UserDataRegistries
{
    /// <summary>
    ///     Registry of all extension methods. Use UserData statics to access these.
    /// </summary>
    internal class ExtensionMethodsRegistry
    {
        private static readonly object s_Lock = new object();

        private static readonly MultiDictionary<string, IOverloadableMemberDescriptor> s_Registry =
            new MultiDictionary<string, IOverloadableMemberDescriptor>();

        private static readonly MultiDictionary<string, UnresolvedGenericMethod> s_UnresolvedGenericsRegistry =
            new MultiDictionary<string, UnresolvedGenericMethod>();

        private static int s_ExtensionMethodChangeVersion;

        /// <summary>
        ///     Registers an extension Type (that is a type containing extension methods)
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="mode">The InteropAccessMode.</param>
        public static void RegisterExtensionType(Type type, InteropAccessMode mode = InteropAccessMode.Default)
        {
            lock (s_Lock)
            {
                var changesDone = false;

                foreach (var mi in type.GetMethods().Where(_mi => _mi.IsStatic))
                {
                    if (mi.GetCustomAttributes(typeof (ExtensionAttribute), false).Length == 0)
                        continue;

                    if (mi.ContainsGenericParameters)
                    {
                        s_UnresolvedGenericsRegistry.Add(mi.Name, new UnresolvedGenericMethod(mi, mode));
                        changesDone = true;
                        continue;
                    }

                    if (!MethodMemberDescriptor.CheckMethodIsCompatible(mi, false))
                        continue;

                    var desc = new MethodMemberDescriptor(mi, mode);

                    s_Registry.Add(mi.Name, desc);
                    changesDone = true;
                }

                if (changesDone)
                    ++s_ExtensionMethodChangeVersion;
            }
        }

        /// <summary>
        ///     Gets all the extension methods which can match a given name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static IEnumerable<IOverloadableMemberDescriptor> GetExtensionMethodsByName(string name)
        {
            lock (s_Lock)
                return new List<IOverloadableMemberDescriptor>(s_Registry.Find(name));
        }

        /// <summary>
        ///     Gets a number which gets incremented everytime the extension methods registry changes.
        ///     Use this to invalidate caches based on extension methods
        /// </summary>
        /// <returns></returns>
        public static int GetExtensionMethodsChangeVersion()
        {
            return s_ExtensionMethodChangeVersion;
        }

        /// <summary>
        ///     Gets all the extension methods which can match a given name and extending a given Type
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="extendedType">The extended type.</param>
        /// <returns></returns>
        public static List<IOverloadableMemberDescriptor> GetExtensionMethodsByNameAndType(string name,
            Type extendedType)
        {
            List<UnresolvedGenericMethod> unresolvedGenerics = null;

            lock (s_Lock)
            {
                unresolvedGenerics = s_UnresolvedGenericsRegistry.Find(name).ToList();
            }

            foreach (var ugm in unresolvedGenerics)
            {
                var args = ugm.Method.GetParameters();
                if (args.Length == 0) continue;
                var extensionType = args[0].ParameterType;

                var genericType = GetGenericMatch(extensionType, extendedType);

                if (ugm.AlreadyAddedTypes.Add(genericType))
                {
                    if (genericType != null)
                    {
                        var mi = InstantiateMethodInfo(ugm.Method, extensionType, genericType, extendedType);
                        if (mi != null)
                        {
                            if (!MethodMemberDescriptor.CheckMethodIsCompatible(mi, false))
                                continue;

                            var desc = new MethodMemberDescriptor(mi, ugm.AccessMode);

                            s_Registry.Add(ugm.Method.Name, desc);
                            ++s_ExtensionMethodChangeVersion;
                        }
                    }
                }
            }

            return s_Registry.Find(name)
                .Where(d => d.ExtensionMethodType != null && d.ExtensionMethodType.IsAssignableFrom(extendedType))
                .ToList();
        }

        private static MethodInfo InstantiateMethodInfo(MethodInfo mi, Type extensionType, Type genericType,
            Type extendedType)
        {
            var defs = mi.GetGenericArguments();
            var tdefs = genericType.GetGenericArguments();

            if (tdefs.Length == defs.Length)
            {
                return mi.MakeGenericMethod(tdefs);
            }

            return null;
        }

        private static Type GetGenericMatch(Type extensionType, Type extendedType)
        {
            extensionType = extensionType.GetGenericTypeDefinition();

            foreach (var t in extendedType.GetAllImplementedTypes())
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == extensionType)
                {
                    return t;
                }
            }

            return null;
        }

        private class UnresolvedGenericMethod
        {
            public readonly InteropAccessMode AccessMode;
            public readonly HashSet<Type> AlreadyAddedTypes = new HashSet<Type>();
            public readonly MethodInfo Method;

            public UnresolvedGenericMethod(MethodInfo mi, InteropAccessMode mode)
            {
                AccessMode = mode;
                Method = mi;
            }
        }
    }
}