﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This type contains the light up scenarios for various platform and runtimes.  Any function
    /// in this type can, and is expected to, fail on various platforms.  These are light up scenarios
    /// only.
    /// </summary>
    internal static class CorLightup
    {
        internal static class Desktop
        {
            private static class _CultureInfo
            {
                internal static readonly Type Type = typeof(CultureInfo);

                internal static readonly PropertyInfo CultureTypes = Type
                    .GetTypeInfo()
                    .GetDeclaredProperty(nameof(CultureTypes));
            }

            private static class CultureTypes
            {
                internal const int UserCustomCulture = 8;
            }

            internal static bool? IsUserCustomCulture(CultureInfo cultureInfo)
            {
                if (_CultureInfo.CultureTypes == null)
                {
                    return null;
                }

                try
                {
                    var value = (int)_CultureInfo.CultureTypes.GetValue(cultureInfo);
                    return (value & CultureTypes.UserCustomCulture) != 0;
                }
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.Message);
                    return null;
                }
            }

            private static class _Directory
            {
                internal static readonly Type Type = typeof(Directory);

                internal static readonly Func<string[]> s_getLogicalDrivesOpt = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("GetLogicalDrives")?
                    .CreateDelegate<Func<string[]>>();
            }

            internal static string[] GetLogicalDrives()
            {
                return _Directory.s_getLogicalDrivesOpt?.Invoke() ?? Array.Empty<string>();
            }

            private static class _Assembly
            {
                internal static readonly Type Type = typeof(Assembly);

                internal static readonly Func<byte[], Assembly> Load_bytes = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("Load", typeof(byte[]))
                    .CreateDelegate<Func<byte[], Assembly>>();

                internal static readonly Func<byte[], byte[], Assembly> Load_bytes_with_Pdb = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("Load", typeof(byte[]), typeof(byte[]))
                    .CreateDelegate<Func<byte[], byte[], Assembly>>();

                internal static readonly Func<string, Assembly> LoadFile = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("LoadFile", typeof(string))
                    .CreateDelegate<Func<string, Assembly>>();

                internal static readonly Func<Assembly, string> get_Location = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("get_Location")
                    .CreateDelegate<Func<Assembly, string>>();

                internal static readonly Func<Assembly, bool> get_GlobalAssemblyCache = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("get_GlobalAssemblyCache")
                    .CreateDelegate<Func<Assembly, bool>>();
            }

            private static class _Module
            {
                internal static readonly Type Type = typeof(Module);

                internal static readonly Func<Module, Guid> get_ModuleVersionId = Type
                    .GetTypeInfo()
                    .GetDeclaredMethod("get_ModuleVersionId")
                    .CreateDelegate<Func<Module, Guid>>();
            }

            internal static Assembly LoadAssembly(byte[] peImage)
            {
                if (_Assembly.Load_bytes == null)
                {
                    throw new PlatformNotSupportedException();
                }

                return _Assembly.Load_bytes(peImage);
            }

            internal static Assembly LoadAssembly(byte[] peImage, byte[] pdbImage)
            {
                if (_Assembly.Load_bytes_with_Pdb == null)
                {
                    throw new PlatformNotSupportedException();
                }

                return _Assembly.Load_bytes_with_Pdb(peImage, pdbImage);
            }

            internal static Assembly LoadAssembly(string path)
            {
                if (_Assembly.LoadFile == null)
                {
                    throw new PlatformNotSupportedException();
                }

                return _Assembly.LoadFile(path);
            }

            internal static string GetAssemblyLocation(Assembly assembly)
            {
                if (_Assembly.get_Location == null)
                {
                    throw new PlatformNotSupportedException();
                }

                return _Assembly.get_Location(assembly);
            }

            internal static Guid GetModuleVersionId(Module module)
            {
                return _Module.get_ModuleVersionId(module);
            }

            internal static bool IsAssemblyFromGlobalAssemblyCache(Assembly assembly)
            {
                if (_Assembly.get_GlobalAssemblyCache == null)
                {
                    throw new PlatformNotSupportedException();
                }

                return _Assembly.get_GlobalAssemblyCache(assembly);
            }

            private sealed class AssemblyResolveWrapper
            {
                private readonly Func<string, Assembly, Assembly> _handler;
                private static readonly MethodInfo s_stubInfo = typeof(AssemblyResolveWrapper).GetTypeInfo().GetDeclaredMethod("Stub");

                public AssemblyResolveWrapper(Func<string, Assembly, Assembly> handler)
                {
                    _handler = handler;
                }

                private Assembly Stub(object sender, object resolveEventArgs)
                {
                    var name = (string)_ResolveEventArgs.get_Name.Invoke(resolveEventArgs, Array.Empty<object>());
                    var requestingAssembly = (Assembly)_ResolveEventArgs.get_RequestingAssembly.Invoke(resolveEventArgs, Array.Empty<object>());

                    return _handler(name, requestingAssembly);
                }

                public object GetHandler()
                {
                    return s_stubInfo.CreateDelegate(_AppDomain.ResolveEventHandlerType, this);
                }
            }

            internal static object GetCurrentAppDomain()
            {
                if (_AppDomain.get_CurrentDomain == null ||
                    _AppDomain.add_AssemblyResolve == null ||
                    _ResolveEventArgs.get_Name == null ||
                    _ResolveEventArgs.get_RequestingAssembly == null)
                {
                    throw new PlatformNotSupportedException();
                }

                return _AppDomain.get_CurrentDomain.Invoke(null, Array.Empty<object>());
            }
        }
    }
}
