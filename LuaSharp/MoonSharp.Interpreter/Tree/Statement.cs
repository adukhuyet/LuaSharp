﻿using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Tree.Expressions;
using MoonSharp.Interpreter.Tree.Statements;

namespace MoonSharp.Interpreter.Tree
{
    internal abstract class Statement : NodeBase
    {
        public Statement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
        }

        protected static Statement CreateStatement(ScriptLoadingContext lcontext, out bool forceLast)
        {
            var tkn = lcontext.Lexer.Current;

            forceLast = false;

            switch (tkn.Type)
            {
                case TokenType.DoubleColon:
                    return new LabelStatement(lcontext);
                case TokenType.Goto:
                    return new GotoStatement(lcontext);
                case TokenType.SemiColon:
                    lcontext.Lexer.Next();
                    return new EmptyStatement(lcontext);
                case TokenType.If:
                    return new IfStatement(lcontext);
                case TokenType.While:
                    return new WhileStatement(lcontext);
                case TokenType.Do:
                    return new ScopeBlockStatement(lcontext);
                case TokenType.For:
                    return DispatchForLoopStatement(lcontext);
                case TokenType.Repeat:
                    return new RepeatStatement(lcontext);
                case TokenType.Function:
                    return new FunctionDefinitionStatement(lcontext, false, null);
                case TokenType.Local:
                    var localToken = lcontext.Lexer.Current;
                    lcontext.Lexer.Next();
                    if (lcontext.Lexer.Current.Type == TokenType.Function)
                        return new FunctionDefinitionStatement(lcontext, true, localToken);
                    return new AssignmentStatement(lcontext, localToken);
                case TokenType.Return:
                    forceLast = true;
                    return new ReturnStatement(lcontext);
                case TokenType.Break:
                    return new BreakStatement(lcontext);
                default:
                {
                    var l = lcontext.Lexer.Current;
                    var exp = Expression.PrimaryExp(lcontext);
                    var fnexp = exp as FunctionCallExpression;

                    if (fnexp != null)
                        return new FunctionCallStatement(lcontext, fnexp);
                    return new AssignmentStatement(lcontext, exp, l);
                }
            }
        }

        private static Statement DispatchForLoopStatement(ScriptLoadingContext lcontext)
        {
            //	for Name ‘=’ exp ‘,’ exp [‘,’ exp] do block end | 
            //	for namelist in explist do block end | 		

            var forTkn = CheckTokenType(lcontext, TokenType.For);

            var name = CheckTokenType(lcontext, TokenType.Name);

            if (lcontext.Lexer.Current.Type == TokenType.Op_Assignment)
                return new ForLoopStatement(lcontext, name, forTkn);
            return new ForEachLoopStatement(lcontext, name, forTkn);
        }
    }
}