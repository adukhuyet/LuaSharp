﻿using System.Collections.Generic;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.Tree.Expressions
{
    internal class TableConstructor : Expression
    {
        private readonly List<KeyValuePair<Expression, Expression>> m_CtorArgs =
            new List<KeyValuePair<Expression, Expression>>();

        private readonly List<Expression> m_PositionalValues = new List<Expression>();

        public TableConstructor(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            // here lexer is at the '{', go on
            CheckTokenType(lcontext, TokenType.Brk_Open_Curly);

            while (lcontext.Lexer.Current.Type != TokenType.Brk_Close_Curly)
            {
                switch (lcontext.Lexer.Current.Type)
                {
                    case TokenType.Name:
                    {
                        var assign = lcontext.Lexer.PeekNext();

                        if (assign.Type == TokenType.Op_Assignment)
                            StructField(lcontext);
                        else
                            ArrayField(lcontext);
                    }
                        break;
                    case TokenType.Brk_Open_Square:
                        MapField(lcontext);
                        break;
                    default:
                        ArrayField(lcontext);
                        break;
                }

                var curr = lcontext.Lexer.Current;

                if (curr.Type == TokenType.Comma || curr.Type == TokenType.SemiColon)
                {
                    lcontext.Lexer.Next();
                }
                else
                {
                    break;
                }
            }

            CheckTokenType(lcontext, TokenType.Brk_Close_Curly);
        }

        private void MapField(ScriptLoadingContext lcontext)
        {
            lcontext.Lexer.Next(); // skip '['

            var key = Expr(lcontext);

            CheckTokenType(lcontext, TokenType.Brk_Close_Square);

            CheckTokenType(lcontext, TokenType.Op_Assignment);

            var value = Expr(lcontext);

            m_CtorArgs.Add(new KeyValuePair<Expression, Expression>(key, value));
        }

        private void StructField(ScriptLoadingContext lcontext)
        {
            Expression key = new LiteralExpression(lcontext, DynValue.NewString(lcontext.Lexer.Current.Text));
            lcontext.Lexer.Next();

            CheckTokenType(lcontext, TokenType.Op_Assignment);

            var value = Expr(lcontext);

            m_CtorArgs.Add(new KeyValuePair<Expression, Expression>(key, value));
        }

        private void ArrayField(ScriptLoadingContext lcontext)
        {
            var e = Expr(lcontext);
            m_PositionalValues.Add(e);
        }

        public override void Compile(ByteCode bc)
        {
            bc.Emit_NewTable();

            foreach (var kvp in m_CtorArgs)
            {
                kvp.Key.Compile(bc);
                kvp.Value.Compile(bc);
                bc.Emit_TblInitN();
            }

            for (var i = 0; i < m_PositionalValues.Count; i++)
            {
                m_PositionalValues[i].Compile(bc);
                bc.Emit_TblInitI(i == m_PositionalValues.Count - 1);
            }
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            throw new DynamicExpressionException("Dynamic Expressions cannot define new tables.");
        }
    }
}