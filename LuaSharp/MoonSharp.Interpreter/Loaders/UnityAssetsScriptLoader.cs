using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MoonSharp.Interpreter.Loaders
{
    /// <summary>
    ///     A script loader which can load scripts from assets in Unity3D.
    ///     Scripts should be saved as .txt files in a subdirectory of Assets/Resources.
    ///     When MoonSharp is activated on Unity3D and the default script loader is used,
    ///     scripts should be saved as .txt files in Assets/Resources/MoonSharp/Scripts.
    /// </summary>
    public class UnityAssetsScriptLoader : ScriptLoaderBase
    {
        /// <summary>
        ///     The default path where scripts are meant to be stored (if not changed)
        /// </summary>
        public const string DEFAULT_PATH = "MoonSharp/Scripts";

        private readonly Dictionary<string, string> m_Resources = new Dictionary<string, string>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="UnityAssetsScriptLoader" /> class.
        /// </summary>
        /// <param name="assetsPath">
        ///     The path, relative to Assets/Resources. For example
        ///     if your scripts are stored under Assets/Resources/Scripts, you should
        ///     pass the value "Scripts". If null, "MoonSharp/Scripts" is used.
        /// </param>
        public UnityAssetsScriptLoader(string assetsPath = null)
        {
            assetsPath = assetsPath ?? DEFAULT_PATH;
            LoadResourcesWithReflection(assetsPath);
        }

        private void LoadResourcesWithReflection(string assetsPath)
        {
            try
            {
                var resourcesType = Type.GetType("UnityEngine.Resources, UnityEngine");
                var textAssetType = Type.GetType("UnityEngine.TextAsset, UnityEngine");

                var textAssetNameGet = textAssetType.GetProperty("name").GetGetMethod();
                var textAssetTextGet = textAssetType.GetProperty("text").GetGetMethod();


                var loadAll = resourcesType.GetMethod("LoadAll",
                    new[] {typeof (string), typeof (Type)});

                var array = (Array) loadAll.Invoke(null, new object[] {assetsPath, textAssetType});

                for (var i = 0; i < array.Length; i++)
                {
                    var o = array.GetValue(i);

                    var name = textAssetNameGet.Invoke(o, null) as string;
                    var text = textAssetTextGet.Invoke(o, null) as string;

                    m_Resources.Add(name, text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UnityAssetsScriptLoader error : " + ex.Message);
            }
        }

        private string GetFileName(string filename)
        {
            var b = Math.Max(filename.LastIndexOf('\\'), filename.LastIndexOf('/'));

            if (b > 0)
                filename = filename.Substring(b + 1);

            return filename;
        }

        /// <summary>
        ///     Opens a file for reading the script code.
        ///     It can return either a string, a byte[] or a Stream.
        ///     If a byte[] is returned, the content is assumed to be a serialized (dumped) bytecode. If it's a string, it's
        ///     assumed to be either a script or the output of a string.dump call. If a Stream, autodetection takes place.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>
        ///     A string, a byte[] or a Stream.
        /// </returns>
        /// <exception cref="System.Exception">UnityAssetsScriptLoader.LoadFile : Cannot load  + file</exception>
        public override object LoadFile(string file, Table globalContext)
        {
            file = GetFileName(file);

            if (m_Resources.ContainsKey(file))
                return m_Resources[file];
            var error = string.Format(
                @"Cannot load script '{0}'. By default, scripts should be .txt files placed under a Assets/Resources/{1} directory.
If you want scripts to be put in another directory or another way, use a custom instance of UnityAssetsScriptLoader or implement
your own IScriptLoader (possibly extending ScriptLoaderBase).", file, DEFAULT_PATH);

            throw new Exception(error);
        }

        /// <summary>
        ///     Checks if a given file exists
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        public override bool ScriptFileExists(string file)
        {
            file = GetFileName(file);
            return m_Resources.ContainsKey(file);
        }

        /// <summary>
        ///     Gets the list of loaded scripts filenames (useful for debugging purposes).
        /// </summary>
        /// <returns></returns>
        public string[] GetLoadedScripts()
        {
            return m_Resources.Keys.ToArray();
        }
    }
}