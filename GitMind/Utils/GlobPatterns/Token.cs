﻿namespace GitMind.Utils.GlobPatterns
{
    class Token
    {
        public Token(TokenKind kind, string spelling)
        {
            this.Kind = kind;
            this.Spelling = spelling;
        }

        public TokenKind Kind { get; }
        public string Spelling { get; }

        public override string ToString()
        {
            return Kind + ": " + Spelling;
        }
    }
}
