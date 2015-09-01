using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.Tree.Statements
{
    internal class ChunkStatement : Statement, IClosureBuilder
    {
        private readonly Statement m_Block;
        private readonly SymbolRef m_Env;
        private readonly Table m_GlobalEnv;
        private readonly RuntimeScopeFrame m_StackFrame;
        private readonly SymbolRef m_VarArgs;

        public ChunkStatement(ScriptLoadingContext lcontext, Table globalEnv)
            : base(lcontext)
        {
            lcontext.Scope.PushFunction(this, true);
            m_Env = lcontext.Scope.DefineLocal(WellKnownSymbols.ENV);
            m_VarArgs = lcontext.Scope.DefineLocal(WellKnownSymbols.VARARGS);

            m_GlobalEnv = globalEnv;

            m_Block = new CompositeStatement(lcontext);

            if (lcontext.Lexer.Current.Type != TokenType.Eof)
                throw new SyntaxErrorException(lcontext.Lexer.Current, "<eof> expected near '{0}'",
                    lcontext.Lexer.Current.Text);

            m_StackFrame = lcontext.Scope.PopFunction();
        }

        public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol)
        {
            return null;
        }

        public override void Compile(ByteCode bc)
        {
            var meta = bc.Emit_FuncMeta("<chunk-root>");
            var metaip = bc.GetJumpPointForLastInstruction();

            bc.Emit_BeginFn(m_StackFrame);
            bc.Emit_Args(m_VarArgs);

            bc.Emit_Literal(DynValue.NewTable(m_GlobalEnv));
            bc.Emit_Store(m_Env, 0, 0);
            bc.Emit_Pop();

            m_Block.Compile(bc);
            bc.Emit_Ret(0);

            meta.NumVal = bc.GetJumpPointForLastInstruction() - metaip;
        }
    }
}