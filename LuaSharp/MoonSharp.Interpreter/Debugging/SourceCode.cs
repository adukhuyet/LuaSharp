﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MoonSharp.Interpreter.Debugging
{
    /// <summary>
    ///     Class representing the source code of a given script
    /// </summary>
    public class SourceCode : IScriptPrivateResource
    {
        internal SourceCode(string name, string code, int sourceID, Script ownerScript)
        {
            Refs = new List<SourceRef>();

            var lines = new List<string>();

            Name = name;
            Code = code;

            lines.Add(string.Format("-- Begin of chunk : {0} ", name));

            lines.AddRange(Code.Split('\n'));

            Lines = lines.ToArray();

            OwnerScript = ownerScript;
            SourceID = sourceID;
        }

        /// <summary>
        ///     Gets the name of the source code
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     Gets the source code as a string
        /// </summary>
        public string Code { get; }

        /// <summary>
        ///     Gets the source code lines.
        /// </summary>
        public string[] Lines { get; }

        /// <summary>
        ///     Gets the source identifier inside a script
        /// </summary>
        public int SourceID { get; private set; }

        internal List<SourceRef> Refs { get; private set; }

        /// <summary>
        ///     Gets the script owning this resource.
        /// </summary>
        public Script OwnerScript { get; }

        /// <summary>
        ///     Gets the code snippet represented by a source ref
        /// </summary>
        /// <param name="sourceCodeRef">The source code reference.</param>
        /// <returns></returns>
        public string GetCodeSnippet(SourceRef sourceCodeRef)
        {
            if (sourceCodeRef.FromLine == sourceCodeRef.ToLine)
            {
                var from = AdjustStrIndex(Lines[sourceCodeRef.FromLine], sourceCodeRef.FromChar);
                var to = AdjustStrIndex(Lines[sourceCodeRef.FromLine], sourceCodeRef.ToChar);
                return Lines[sourceCodeRef.FromLine].Substring(from, to - from);
            }

            var sb = new StringBuilder();

            for (var i = sourceCodeRef.FromLine; i <= sourceCodeRef.ToLine; i++)
            {
                if (i == sourceCodeRef.FromLine)
                {
                    var from = AdjustStrIndex(Lines[i], sourceCodeRef.FromChar);
                    sb.Append(Lines[i].Substring(from));
                }
                else if (i == sourceCodeRef.ToLine)
                {
                    var to = AdjustStrIndex(Lines[i], sourceCodeRef.ToChar);
                    sb.Append(Lines[i].Substring(0, to + 1));
                }
                else
                {
                    sb.Append(Lines[i]);
                }
            }

            return sb.ToString();
        }

        private int AdjustStrIndex(string str, int loc)
        {
            return Math.Max(Math.Min(str.Length, loc), 0);
        }
    }
}