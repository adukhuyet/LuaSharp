﻿using System;
using System.Linq;
using System.Linq.Expressions;
using MoonSharp.Interpreter.Interop;
using MoonSharp.Interpreter.Loaders;

namespace MoonSharp.Interpreter.Platforms
{
    /// <summary>
    ///     A static class offering properties for autodetection of system/platform details
    /// </summary>
    public static class PlatformAutoDetector
    {
        private static bool? m_IsRunningOnAOT;
        private static bool m_AutoDetectionsDone;

        /// <summary>
        ///     Gets a value indicating whether this instance is running on mono.
        /// </summary>
        public static bool IsRunningOnMono { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is running on a CLR4 compatible implementation
        /// </summary>
        public static bool IsRunningOnClr4 { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is running on Unity-3D
        /// </summary>
        public static bool IsRunningOnUnity { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance has been built as a Portable Class Library
        /// </summary>
        public static bool IsPortableFramework { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is running a system using Ahead-Of-Time compilation
        ///     and not supporting JIT.
        /// </summary>
        public static bool IsRunningOnAOT
        {
            // We do a lazy eval here, so we can wire out this code by not calling it, if necessary..
            get
            {
                if (!m_IsRunningOnAOT.HasValue)
                {
                    try
                    {
                        Expression e = Expression.Constant(5, typeof (int));
                        var lambda = Expression.Lambda<Func<int>>(e);
                        lambda.Compile();
                        m_IsRunningOnAOT = false;
                    }
                    catch (Exception)
                    {
                        m_IsRunningOnAOT = true;
                    }
                }

                return m_IsRunningOnAOT.Value;
            }
        }

        private static void AutoDetectPlatformFlags()
        {
            if (m_AutoDetectionsDone)
                return;
#if PCL
			IsPortableFramework = true;
#else
            IsRunningOnUnity = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.SafeGetTypes())
                .Any(t => t.FullName.StartsWith("UnityEngine."));
#endif

            IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);

            IsRunningOnClr4 = (Type.GetType("System.Lazy`1") != null);

            m_AutoDetectionsDone = true;
        }

        internal static IPlatformAccessor GetDefaultPlatform()
        {
            AutoDetectPlatformFlags();

#if PCL
			return new LimitedPlatformAccessor();
#else
            if (IsRunningOnUnity)
                return new LimitedPlatformAccessor();

            return new StandardPlatformAccessor();
#endif
        }

        internal static IScriptLoader GetDefaultScriptLoader()
        {
            AutoDetectPlatformFlags();

#if PCL
			return new InvalidScriptLoader("Portable Framework");
#else
            if (IsRunningOnUnity)
                return new UnityAssetsScriptLoader();

            return new FileSystemScriptLoader();
#endif
        }
    }
}