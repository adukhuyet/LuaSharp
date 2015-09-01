﻿using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;
using MoonSharp.Interpreter.Tree.Expressions;

namespace MoonSharp.Interpreter.Tree.Statements
{
    internal class FunctionCallStatement : Statement
    {
        private readonly FunctionCallExpression m_FunctionCallExpression;

        public FunctionCallStatement(ScriptLoadingContext lcontext, FunctionCallExpression functionCallExpression)
            : base(lcontext)
        {
            m_FunctionCallExpression = functionCallExpression;
            lcontext.Source.Refs.Add(m_FunctionCallExpression.SourceRef);
        }

        public override void Compile(ByteCode bc)
        {
            using (bc.EnterSource(m_FunctionCallExpression.SourceRef))
            {
                m_FunctionCallExpression.Compile(bc);
                bc.Emit_Pop();
            }
        }
    }
}