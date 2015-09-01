﻿using System;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.Tree.Expressions
{
    /// <summary>
    /// </summary>
    internal class BinaryOperatorExpression : Expression
    {
        private const Operator POWER = Operator.Power;
        private const Operator MUL_DIV_MOD = Operator.Mul | Operator.Div | Operator.Mod;
        private const Operator ADD_SUB = Operator.Add | Operator.Sub;
        private const Operator STRCAT = Operator.StrConcat;

        private const Operator COMPARES =
            Operator.Less | Operator.Greater | Operator.GreaterOrEqual | Operator.LessOrEqual | Operator.Equal |
            Operator.NotEqual;

        private const Operator LOGIC_AND = Operator.And;
        private const Operator LOGIC_OR = Operator.Or;
        private readonly Expression m_Exp1;
        private readonly Expression m_Exp2;
        private readonly Operator m_Operator;

        private BinaryOperatorExpression(Expression exp1, Expression exp2, Operator op, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            m_Exp1 = exp1;
            m_Exp2 = exp2;
            m_Operator = op;
        }

        public static object BeginOperatorChain()
        {
            return new LinkedList();
        }

        public static void AddExpressionToChain(object chain, Expression exp)
        {
            var list = (LinkedList) chain;
            var node = new Node {Expr = exp};
            AddNode(list, node);
        }

        public static void AddOperatorToChain(object chain, Token op)
        {
            var list = (LinkedList) chain;
            var node = new Node {Op = ParseBinaryOperator(op)};
            AddNode(list, node);
        }

        public static Expression CommitOperatorChain(object chain, ScriptLoadingContext lcontext)
        {
            return CreateSubTree((LinkedList) chain, lcontext);
        }

        public static Expression CreatePowerExpression(Expression op1, Expression op2, ScriptLoadingContext lcontext)
        {
            return new BinaryOperatorExpression(op1, op2, Operator.Power, lcontext);
        }

        private static void AddNode(LinkedList list, Node node)
        {
            list.OperatorMask |= node.Op;

            if (list.Nodes == null)
            {
                list.Nodes = list.Last = node;
            }
            else
            {
                list.Last.Next = node;
                node.Prev = list.Last;
                list.Last = node;
            }
        }

        /// <summary>
        ///     Creates a sub tree of binary expressions
        /// </summary>
        private static Expression CreateSubTree(LinkedList list, ScriptLoadingContext lcontext)
        {
            var opfound = list.OperatorMask;

            var nodes = list.Nodes;

            if ((opfound & POWER) != 0)
                nodes = PrioritizeRightAssociative(nodes, lcontext, POWER);

            if ((opfound & MUL_DIV_MOD) != 0)
                nodes = PrioritizeLeftAssociative(nodes, lcontext, MUL_DIV_MOD);

            if ((opfound & ADD_SUB) != 0)
                nodes = PrioritizeLeftAssociative(nodes, lcontext, ADD_SUB);

            if ((opfound & STRCAT) != 0)
                nodes = PrioritizeRightAssociative(nodes, lcontext, STRCAT);

            if ((opfound & COMPARES) != 0)
                nodes = PrioritizeLeftAssociative(nodes, lcontext, COMPARES);

            if ((opfound & LOGIC_AND) != 0)
                nodes = PrioritizeLeftAssociative(nodes, lcontext, LOGIC_AND);

            if ((opfound & LOGIC_OR) != 0)
                nodes = PrioritizeLeftAssociative(nodes, lcontext, LOGIC_OR);


            if (nodes.Next != null || nodes.Prev != null)
                throw new InternalErrorException("Expression reduction didn't work! - 1");
            if (nodes.Expr == null)
                throw new InternalErrorException("Expression reduction didn't work! - 2");

            return nodes.Expr;
        }

        private static Node PrioritizeLeftAssociative(Node nodes, ScriptLoadingContext lcontext,
            Operator operatorsToFind)
        {
            for (var N = nodes; N != null; N = N.Next)
            {
                var o = N.Op;

                if ((o & operatorsToFind) != 0)
                {
                    N.Op = Operator.NotAnOperator;
                    N.Expr = new BinaryOperatorExpression(N.Prev.Expr, N.Next.Expr, o, lcontext);
                    N.Prev = N.Prev.Prev;
                    N.Next = N.Next.Next;

                    if (N.Next != null)
                        N.Next.Prev = N;

                    if (N.Prev != null)
                        N.Prev.Next = N;
                    else
                        nodes = N;
                }
            }

            return nodes;
        }

        private static Node PrioritizeRightAssociative(Node nodes, ScriptLoadingContext lcontext,
            Operator operatorsToFind)
        {
            Node last;
            for (last = nodes; last.Next != null; last = last.Next) ;

            for (var N = last; N != null; N = N.Prev)
            {
                var o = N.Op;

                if ((o & operatorsToFind) != 0)
                {
                    N.Op = Operator.NotAnOperator;
                    N.Expr = new BinaryOperatorExpression(N.Prev.Expr, N.Next.Expr, o, lcontext);
                    N.Prev = N.Prev.Prev;
                    N.Next = N.Next.Next;

                    if (N.Next != null)
                        N.Next.Prev = N;

                    if (N.Prev != null)
                        N.Prev.Next = N;
                    else
                        nodes = N;
                }
            }

            return nodes;
        }

        private static Operator ParseBinaryOperator(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Or:
                    return Operator.Or;
                case TokenType.And:
                    return Operator.And;
                case TokenType.Op_LessThan:
                    return Operator.Less;
                case TokenType.Op_GreaterThan:
                    return Operator.Greater;
                case TokenType.Op_LessThanEqual:
                    return Operator.LessOrEqual;
                case TokenType.Op_GreaterThanEqual:
                    return Operator.GreaterOrEqual;
                case TokenType.Op_NotEqual:
                    return Operator.NotEqual;
                case TokenType.Op_Equal:
                    return Operator.Equal;
                case TokenType.Op_Concat:
                    return Operator.StrConcat;
                case TokenType.Op_Add:
                    return Operator.Add;
                case TokenType.Op_MinusOrSub:
                    return Operator.Sub;
                case TokenType.Op_Mul:
                    return Operator.Mul;
                case TokenType.Op_Div:
                    return Operator.Div;
                case TokenType.Op_Mod:
                    return Operator.Mod;
                case TokenType.Op_Pwr:
                    return Operator.Power;
                default:
                    throw new InternalErrorException("Unexpected binary operator '{0}'", token.Text);
            }
        }

        private static bool ShouldInvertBoolean(Operator op)
        {
            return (op == Operator.NotEqual)
                   || (op == Operator.GreaterOrEqual)
                   || (op == Operator.Greater);
        }

        private static OpCode OperatorToOpCode(Operator op)
        {
            switch (op)
            {
                case Operator.Less:
                case Operator.GreaterOrEqual:
                    return OpCode.Less;
                case Operator.LessOrEqual:
                case Operator.Greater:
                    return OpCode.LessEq;
                case Operator.Equal:
                case Operator.NotEqual:
                    return OpCode.Eq;
                case Operator.StrConcat:
                    return OpCode.Concat;
                case Operator.Add:
                    return OpCode.Add;
                case Operator.Sub:
                    return OpCode.Sub;
                case Operator.Mul:
                    return OpCode.Mul;
                case Operator.Div:
                    return OpCode.Div;
                case Operator.Mod:
                    return OpCode.Mod;
                case Operator.Power:
                    return OpCode.Power;
                default:
                    throw new InternalErrorException("Unsupported operator {0}", op);
            }
        }

        public override void Compile(ByteCode bc)
        {
            m_Exp1.Compile(bc);

            if (m_Operator == Operator.Or)
            {
                var i = bc.Emit_Jump(OpCode.JtOrPop, -1);
                m_Exp2.Compile(bc);
                i.NumVal = bc.GetJumpPointForNextInstruction();
                return;
            }

            if (m_Operator == Operator.And)
            {
                var i = bc.Emit_Jump(OpCode.JfOrPop, -1);
                m_Exp2.Compile(bc);
                i.NumVal = bc.GetJumpPointForNextInstruction();
                return;
            }


            if (m_Exp2 != null)
            {
                m_Exp2.Compile(bc);
            }

            bc.Emit_Operator(OperatorToOpCode(m_Operator));

            if (ShouldInvertBoolean(m_Operator))
                bc.Emit_Operator(OpCode.Not);
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            var v1 = m_Exp1.Eval(context).ToScalar();

            if (m_Operator == Operator.Or)
            {
                if (v1.CastToBool())
                    return v1;
                return m_Exp2.Eval(context).ToScalar();
            }

            if (m_Operator == Operator.And)
            {
                if (!v1.CastToBool())
                    return v1;
                return m_Exp2.Eval(context).ToScalar();
            }

            var v2 = m_Exp2.Eval(context).ToScalar();

            if ((m_Operator & COMPARES) != 0)
            {
                return DynValue.NewBoolean(EvalComparison(v1, v2, m_Operator));
            }
            if (m_Operator == Operator.StrConcat)
            {
                var s1 = v1.CastToString();
                var s2 = v2.CastToString();

                if (s1 == null || s2 == null)
                    throw new DynamicExpressionException("Attempt to perform concatenation on non-strings.");

                return DynValue.NewString(s1 + s2);
            }
            return DynValue.NewNumber(EvalArithmetic(v1, v2));
        }

        private double EvalArithmetic(DynValue v1, DynValue v2)
        {
            var nd1 = v1.CastToNumber();
            var nd2 = v2.CastToNumber();

            if (nd1 == null || nd2 == null)
                throw new DynamicExpressionException("Attempt to perform arithmetic on non-numbers.");

            var d1 = nd1.Value;
            var d2 = nd2.Value;

            switch (m_Operator)
            {
                case Operator.Add:
                    return d1 + d2;
                case Operator.Sub:
                    return d1 - d2;
                case Operator.Mul:
                    return d1*d2;
                case Operator.Div:
                    return d1/d2;
                case Operator.Mod:
                {
                    var mod = Math.IEEERemainder(d1, d2);
                    if (mod < 0) mod += d2;
                    return mod;
                }
                default:
                    throw new DynamicExpressionException("Unsupported operator {0}", m_Operator);
            }
        }

        private bool EvalComparison(DynValue l, DynValue r, Operator op)
        {
            switch (op)
            {
                case Operator.Less:
                    if (l.Type == DataType.Number && r.Type == DataType.Number)
                    {
                        return (l.Number < r.Number);
                    }
                    if (l.Type == DataType.String && r.Type == DataType.String)
                    {
                        return (l.String.CompareTo(r.String) < 0);
                    }
                    throw new DynamicExpressionException("Attempt to compare non-numbers, non-strings.");
                case Operator.LessOrEqual:
                    if (l.Type == DataType.Number && r.Type == DataType.Number)
                    {
                        return (l.Number <= r.Number);
                    }
                    if (l.Type == DataType.String && r.Type == DataType.String)
                    {
                        return (l.String.CompareTo(r.String) <= 0);
                    }
                    throw new DynamicExpressionException("Attempt to compare non-numbers, non-strings.");
                case Operator.Equal:
                    if (ReferenceEquals(r, l))
                    {
                        return true;
                    }
                    if (r.Type != l.Type)
                    {
                        if ((l.Type == DataType.Nil && r.Type == DataType.Void)
                            || (l.Type == DataType.Void && r.Type == DataType.Nil))
                            return true;
                        return false;
                    }
                    return r.Equals(l);
                case Operator.Greater:
                    return !EvalComparison(l, r, Operator.LessOrEqual);
                case Operator.GreaterOrEqual:
                    return !EvalComparison(l, r, Operator.Less);
                case Operator.NotEqual:
                    return !EvalComparison(l, r, Operator.Equal);
                default:
                    throw new DynamicExpressionException("Unsupported operator {0}", op);
            }
        }

        [Flags]
        private enum Operator
        {
            NotAnOperator = 0,

            Or = 0x1,
            And = 0x2,
            Less = 0x4,
            Greater = 0x8,
            LessOrEqual = 0x10,

            GreaterOrEqual = 0x20,
            NotEqual = 0x40,
            Equal = 0x80,
            StrConcat = 0x100,
            Add = 0x200,
            Sub = 0x400,
            Mul = 0x1000,
            Div = 0x2000,
            Mod = 0x4000,
            Power = 0x8000
        }

        private class Node
        {
            public Expression Expr;
            public Node Next;
            public Operator Op;
            public Node Prev;
        }

        private class LinkedList
        {
            public Node Last;
            public Node Nodes;
            public Operator OperatorMask;
        }
    }
}