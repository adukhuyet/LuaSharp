﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using MoonSharp.Interpreter.Interop.BasicDescriptors;

namespace MoonSharp.Interpreter.Interop.UserDataRegistries
{
    /// <summary>
    ///     Registry of all type descriptors. Use UserData statics to access these.
    /// </summary>
    internal class TypeDescriptorRegistry
    {
        private static readonly object s_Lock = new object();

        private static readonly Dictionary<Type, IUserDataDescriptor> s_Registry =
            new Dictionary<Type, IUserDataDescriptor>();

        private static InteropAccessMode s_DefaultAccessMode;

        /// <summary>
        ///     Gets or sets the default access mode to be used in the whole application
        /// </summary>
        /// <value>
        ///     The default access mode.
        /// </value>
        /// <exception cref="System.ArgumentException">InteropAccessMode is InteropAccessMode.Default</exception>
        internal static InteropAccessMode DefaultAccessMode
        {
            get { return s_DefaultAccessMode; }
            set
            {
                if (value == InteropAccessMode.Default)
                    throw new ArgumentException("InteropAccessMode is InteropAccessMode.Default");

                s_DefaultAccessMode = value;
            }
        }

        /// <summary>
        ///     Gets or sets the registration policy to be used in the whole application
        /// </summary>
        internal static InteropRegistrationPolicy RegistrationPolicy { get; set; }

        /// <summary>
        ///     Registers all types marked with a MoonSharpUserDataAttribute that ar contained in an assembly.
        /// </summary>
        /// <param name="asm">The assembly.</param>
        /// <param name="includeExtensionTypes">if set to <c>true</c> extension types are registered to the appropriate registry.</param>
        internal static void RegisterAssembly(Assembly asm = null, bool includeExtensionTypes = false)
        {
            asm = asm ?? Assembly.GetCallingAssembly();

            if (includeExtensionTypes)
            {
                var extensionTypes = from t in asm.SafeGetTypes()
                    let attributes = t.GetCustomAttributes(typeof (ExtensionAttribute), true)
                    where attributes != null && attributes.Length > 0
                    select new {Attributes = attributes, DataType = t};

                foreach (var extType in extensionTypes)
                {
                    UserData.RegisterExtensionType(extType.DataType);
                }
            }


            var userDataTypes = from t in asm.SafeGetTypes()
                let attributes = t.GetCustomAttributes(typeof (MoonSharpUserDataAttribute), true)
                where attributes != null && attributes.Length > 0
                select new {Attributes = attributes, DataType = t};

            foreach (var userDataType in userDataTypes)
            {
                UserData.RegisterType(userDataType.DataType, userDataType.Attributes
                    .OfType<MoonSharpUserDataAttribute>()
                    .First()
                    .AccessMode);
            }
        }

        /// <summary>
        ///     Determines whether the specified type is registered. Note that this should be used only to check if a descriptor
        ///     has been registered EXACTLY. For many types a descriptor can still be created, for example through the descriptor
        ///     of a base type or implemented interfaces.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns></returns>
        internal static bool IsTypeRegistered(Type type)
        {
            lock (s_Lock)
                return s_Registry.ContainsKey(type);
        }

        /// <summary>
        ///     Unregisters a type.
        ///     WARNING: unregistering types at runtime is a dangerous practice and may cause unwanted errors.
        ///     Use this only for testing purposes or to re-register the same type in a slightly different way.
        ///     Additionally, it's a good practice to discard all previous loaded scripts after calling this method.
        /// </summary>
        /// <param name="t">The The type to be unregistered</param>
        internal static void UnregisterType(Type t)
        {
            lock (s_Lock)
            {
                if (s_Registry.ContainsKey(t))
                    s_Registry.Remove(t);
            }
        }

        /// <summary>
        ///     Registers a type
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="accessMode">The access mode (used only if a default type descriptor is created).</param>
        /// <param name="friendlyName">Friendly name of the descriptor.</param>
        /// <param name="descriptor">The descriptor, or null to use a default one.</param>
        /// <returns></returns>
        internal static IUserDataDescriptor RegisterType_Impl(Type type, InteropAccessMode accessMode,
            string friendlyName, IUserDataDescriptor descriptor)
        {
            if (accessMode == InteropAccessMode.Default)
            {
                var attr = type.GetCustomAttributes(true).OfType<MoonSharpUserDataAttribute>()
                    .SingleOrDefault();

                if (attr != null)
                    accessMode = attr.AccessMode;
            }


            if (accessMode == InteropAccessMode.Default)
                accessMode = s_DefaultAccessMode;

            lock (s_Lock)
            {
                if (!s_Registry.ContainsKey(type))
                {
                    if (descriptor == null)
                    {
                        if (IsTypeBlacklisted(type))
                            return null;

                        if (type.GetInterfaces().Any(ii => ii == typeof (IUserDataType)))
                        {
                            var audd = new AutoDescribingUserDataDescriptor(type, friendlyName);
                            s_Registry.Add(type, audd);
                            return audd;
                        }
                        if (type.IsGenericTypeDefinition)
                        {
                            var typeGen = new StandardGenericsUserDataDescriptor(type, accessMode);
                            s_Registry.Add(type, typeGen);
                            return typeGen;
                        }
                        if (type.IsEnum)
                        {
                            var enumDescr = new StandardEnumUserDataDescriptor(type, friendlyName);
                            s_Registry.Add(type, enumDescr);
                            return enumDescr;
                        }
                        var udd = new StandardUserDataDescriptor(type, accessMode, friendlyName);
                        s_Registry.Add(type, udd);

                        if (accessMode == InteropAccessMode.BackgroundOptimized)
                        {
                            ThreadPool.QueueUserWorkItem(o => ((IOptimizableDescriptor) udd).Optimize());
                        }

                        return udd;
                    }
                    s_Registry.Add(type, descriptor);
                    return descriptor;
                }
                return s_Registry[type];
            }
        }

        /// <summary>
        ///     Gets the best possible type descriptor for a specified CLR type.
        /// </summary>
        /// <param name="type">The CLR type for which the descriptor is desired.</param>
        /// <param name="searchInterfaces">if set to <c>true</c> interfaces are used in the search.</param>
        /// <returns></returns>
        internal static IUserDataDescriptor GetDescriptorForType(Type type, bool searchInterfaces)
        {
            lock (s_Lock)
            {
                IUserDataDescriptor typeDescriptor = null;

                // if the type has been explicitly registered, return its descriptor as it's complete
                if (s_Registry.ContainsKey(type))
                    return s_Registry[type];

                if (RegistrationPolicy == InteropRegistrationPolicy.Automatic)
                {
                    // no autoreg of delegates
                    if (!(typeof (Delegate)).IsAssignableFrom(type))
                    {
                        return RegisterType_Impl(type, DefaultAccessMode, type.FullName, null);
                    }
                }

                // search for the base object descriptors
                for (var t = type; t != null; t = t.BaseType)
                {
                    IUserDataDescriptor u;

                    if (s_Registry.TryGetValue(t, out u))
                    {
                        typeDescriptor = u;
                        break;
                    }
                    if (t.IsGenericType)
                    {
                        if (s_Registry.TryGetValue(t.GetGenericTypeDefinition(), out u))
                        {
                            typeDescriptor = u;
                            break;
                        }
                    }
                }

                if (typeDescriptor is IGeneratorUserDataDescriptor)
                    typeDescriptor = ((IGeneratorUserDataDescriptor) typeDescriptor).Generate(type);


                // we should not search interfaces (for example, it's just for statics..), no need to look further
                if (!searchInterfaces)
                    return typeDescriptor;

                var descriptors = new List<IUserDataDescriptor>();

                if (typeDescriptor != null)
                    descriptors.Add(typeDescriptor);


                if (searchInterfaces)
                {
                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        IUserDataDescriptor interfaceDescriptor;

                        if (s_Registry.TryGetValue(interfaceType, out interfaceDescriptor))
                        {
                            if (interfaceDescriptor is IGeneratorUserDataDescriptor)
                                interfaceDescriptor = ((IGeneratorUserDataDescriptor) interfaceDescriptor).Generate(type);

                            if (interfaceDescriptor != null)
                                descriptors.Add(interfaceDescriptor);
                        }
                        else if (interfaceType.IsGenericType)
                        {
                            if (s_Registry.TryGetValue(interfaceType.GetGenericTypeDefinition(), out interfaceDescriptor))
                            {
                                if (interfaceDescriptor is IGeneratorUserDataDescriptor)
                                    interfaceDescriptor =
                                        ((IGeneratorUserDataDescriptor) interfaceDescriptor).Generate(type);

                                if (interfaceDescriptor != null)
                                    descriptors.Add(interfaceDescriptor);
                            }
                        }
                    }
                }

                if (descriptors.Count == 1)
                    return descriptors[0];
                if (descriptors.Count == 0)
                    return null;
                return new CompositeUserDataDescriptor(descriptors, type);
            }
        }

        /// <summary>
        ///     Determines whether the specified type is blacklisted.
        ///     Blacklisted types CANNOT be registered using default descriptors but they can still be registered
        ///     with custom descriptors. Forcing registration of blacklisted types in this way can introduce
        ///     side effects.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        public static bool IsTypeBlacklisted(Type t)
        {
            if (t.IsValueType && t.GetInterfaces().Contains(typeof (IEnumerator)))
                return true;

            return false;
        }
    }
}