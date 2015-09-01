﻿using System;
using System.Collections.Generic;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;
using MoonSharp.Interpreter.Tree.Statements;

namespace MoonSharp.Interpreter.Tree.Expressions
{
    internal class FunctionDefinitionExpression : Expression, IClosureBuilder
    {
        private readonly SourceRef m_Begin;
        private readonly List<SymbolRef> m_Closure = new List<SymbolRef>();
        private readonly SymbolRef m_Env;
        private readonly Table m_GlobalEnv;
        private readonly SymbolRef[] m_ParamNames;
        private readonly RuntimeScopeFrame m_StackFrame;
        private readonly Statement m_Statement;
        private Instruction m_ClosureInstruction;
        private SourceRef m_End;
        private bool m_HasVarArgs;

        public FunctionDefinitionExpression(ScriptLoadingContext lcontext, Table globalContext)
            : this(lcontext, false, globalContext, false)
        {
        }

        public FunctionDefinitionExpression(ScriptLoadingContext lcontext, bool pushSelfParam, bool isLambda)
            : this(lcontext, pushSelfParam, null, isLambda)
        {
        }

        private FunctionDefinitionExpression(ScriptLoadingContext lcontext, bool pushSelfParam, Table globalContext,
            bool isLambda)
            : base(lcontext)
        {
            if (globalContext != null)
                CheckTokenType(lcontext, TokenType.Function);

            // here lexer should be at the '(' or at the '|'
            var openRound = CheckTokenType(lcontext, isLambda ? TokenType.Lambda : TokenType.Brk_Open_Round);

            var paramnames = BuildParamList(lcontext, pushSelfParam, openRound, isLambda);
            // here lexer is at first token of body

            m_Begin = openRound.GetSourceRefUpTo(lcontext.Lexer.Current);

            // create scope
            lcontext.Scope.PushFunction(this, m_HasVarArgs);

            if (globalContext != null)
            {
                m_GlobalEnv = globalContext;
                m_Env = lcontext.Scope.TryDefineLocal(WellKnownSymbols.ENV);
            }
            else
            {
                lcontext.Scope.ForceEnvUpValue();
            }

            m_ParamNames = DefineArguments(paramnames, lcontext);

            if (isLambda)
                m_Statement = CreateLambdaBody(lcontext);
            else
                m_Statement = CreateBody(lcontext);

            m_StackFrame = lcontext.Scope.PopFunction();

            lcontext.Source.Refs.Add(m_Begin);
            lcontext.Source.Refs.Add(m_End);
        }

        public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol)
        {
            for (var i = 0; i < m_Closure.Count; i++)
            {
                if (m_Closure[i].i_Name == symbol.i_Name)
                {
                    return SymbolRef.Upvalue(symbol.i_Name, i);
                }
            }

            m_Closure.Add(symbol);

            if (m_ClosureInstruction != null)
            {
                m_ClosureInstruction.SymbolList = m_Closure.ToArray();
            }

            return SymbolRef.Upvalue(symbol.i_Name, m_Closure.Count - 1);
        }

        private Statement CreateLambdaBody(ScriptLoadingContext lcontext)
        {
            var start = lcontext.Lexer.Current;
            var e = Expr(lcontext);
            var end = lcontext.Lexer.Current;
            var sref = start.GetSourceRefUpTo(end);
            Statement s = new ReturnStatement(lcontext, e, sref);
            return s;
        }

        private Statement CreateBody(ScriptLoadingContext lcontext)
        {
            Statement s = new CompositeStatement(lcontext);

            if (lcontext.Lexer.Current.Type != TokenType.End)
                throw new SyntaxErrorException(lcontext.Lexer.Current, "'end' expected near '{0}'",
                    lcontext.Lexer.Current.Text)
                {
                    IsPrematureStreamTermination = (lcontext.Lexer.Current.Type == TokenType.Eof)
                };

            m_End = lcontext.Lexer.Current.GetSourceRef();

            lcontext.Lexer.Next();
            return s;
        }

        private List<string> BuildParamList(ScriptLoadingContext lcontext, bool pushSelfParam, Token openBracketToken,
            bool isLambda)
        {
            var closeToken = isLambda ? TokenType.Lambda : TokenType.Brk_Close_Round;

            var paramnames = new List<string>();

            // method decls with ':' must push an implicit 'self' param
            if (pushSelfParam)
                paramnames.Add("self");

            while (lcontext.Lexer.Current.Type != closeToken)
            {
                var t = lcontext.Lexer.Current;

                if (t.Type == TokenType.Name)
                {
                    paramnames.Add(t.Text);
                }
                else if (t.Type == TokenType.VarArgs)
                {
                    m_HasVarArgs = true;
                    paramnames.Add(WellKnownSymbols.VARARGS);
                }
                else
                    UnexpectedTokenType(t);

                lcontext.Lexer.Next();

                t = lcontext.Lexer.Current;

                if (t.Type == TokenType.Comma)
                {
                    lcontext.Lexer.Next();
                }
                else
                {
                    CheckMatch(lcontext, openBracketToken, closeToken, isLambda ? "|" : ")");
                    break;
                }
            }

            if (lcontext.Lexer.Current.Type == closeToken)
                lcontext.Lexer.Next();

            return paramnames;
        }

        private SymbolRef[] DefineArguments(List<string> paramnames, ScriptLoadingContext lcontext)
        {
            var names = new HashSet<string>();

            var ret = new SymbolRef[paramnames.Count];

            for (var i = paramnames.Count - 1; i >= 0; i--)
            {
                if (!names.Add(paramnames[i]))
                    paramnames[i] = paramnames[i] + "@" + i;

                ret[i] = lcontext.Scope.DefineLocal(paramnames[i]);
            }

            return ret;
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            throw new DynamicExpressionException("Dynamic Expressions cannot define new functions.");
        }

        public int CompileBody(ByteCode bc, string friendlyName)
        {
            var funcName = friendlyName ?? ("<" + m_Begin.FormatLocation(bc.Script, true) + ">");

            bc.PushSourceRef(m_Begin);

            var I = bc.Emit_Jump(OpCode.Jump, -1);

            var meta = bc.Emit_FuncMeta(funcName);
            var metaip = bc.GetJumpPointForLastInstruction();

            bc.Emit_BeginFn(m_StackFrame);

            bc.LoopTracker.Loops.Push(new LoopBoundary());

            var entryPoint = bc.GetJumpPointForLastInstruction();

            if (m_GlobalEnv != null)
            {
                bc.Emit_Literal(DynValue.NewTable(m_GlobalEnv));
                bc.Emit_Store(m_Env, 0, 0);
                bc.Emit_Pop();
            }

            if (m_ParamNames.Length > 0)
                bc.Emit_Args(m_ParamNames);

            m_Statement.Compile(bc);

            bc.PopSourceRef();
            bc.PushSourceRef(m_End);

            bc.Emit_Ret(0);

            bc.LoopTracker.Loops.Pop();

            I.NumVal = bc.GetJumpPointForNextInstruction();
            meta.NumVal = bc.GetJumpPointForLastInstruction() - metaip;

            bc.PopSourceRef();

            return entryPoint;
        }

        public int Compile(ByteCode bc, Func<int> afterDecl, string friendlyName)
        {
            using (bc.EnterSource(m_Begin))
            {
                var symbs = m_Closure
                    //.Select((s, idx) => s.CloneLocalAndSetFrame(m_ClosureFrames[idx]))
                    .ToArray();

                m_ClosureInstruction = bc.Emit_Closure(symbs, bc.GetJumpPointForNextInstruction());
                var ops = afterDecl();

                m_ClosureInstruction.NumVal += 2 + ops;
            }

            return CompileBody(bc, friendlyName);
        }

        public override void Compile(ByteCode bc)
        {
            Compile(bc, () => 0, null);
        }
    }
}