﻿using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.Tree.Statements
{
    internal class RepeatStatement : Statement
    {
        private readonly Statement m_Block;
        private readonly Expression m_Condition;
        private readonly SourceRef m_Repeat;
        private readonly RuntimeScopeBlock m_StackFrame;
        private readonly SourceRef m_Until;

        public RepeatStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            m_Repeat = CheckTokenType(lcontext, TokenType.Repeat).GetSourceRef();

            lcontext.Scope.PushBlock();
            m_Block = new CompositeStatement(lcontext);

            var until = CheckTokenType(lcontext, TokenType.Until);

            m_Condition = Expression.Expr(lcontext);

            m_Until = until.GetSourceRefUpTo(lcontext.Lexer.Current);

            m_StackFrame = lcontext.Scope.PopBlock();
            lcontext.Source.Refs.Add(m_Repeat);
            lcontext.Source.Refs.Add(m_Until);
        }

        public override void Compile(ByteCode bc)
        {
            var L = new Loop
            {
                Scope = m_StackFrame
            };

            bc.PushSourceRef(m_Repeat);

            bc.LoopTracker.Loops.Push(L);

            var start = bc.GetJumpPointForNextInstruction();

            bc.Emit_Enter(m_StackFrame);
            m_Block.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(m_Until);
            bc.Emit_Debug("..end");

            m_Condition.Compile(bc);
            bc.Emit_Leave(m_StackFrame);
            bc.Emit_Jump(OpCode.Jf, start);

            bc.LoopTracker.Loops.Pop();

            var exitpoint = bc.GetJumpPointForNextInstruction();

            foreach (var i in L.BreakJumps)
                i.NumVal = exitpoint;

            bc.PopSourceRef();
        }
    }
}