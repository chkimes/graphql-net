using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.FSharp.Collections;

namespace EntityFramework.GraphQL
{
    public class Parser
    {
        public Query Parse(string query)
        {
            var parsed = GraphQLParser.parse(query);
            if (parsed.Value == null)
                throw new Exception("i dunno man");
            if (!parsed.Value.IsQueryOperation)
                throw new Exception("i dunno man");

            var op = parsed.Value as GraphQLParser.Definition.QueryOperation;
            var name = op.Item.Item1;
            var selection = op.Item.Item2;
            return new Query
            {
                Name = op.Item.Item1,
                Fields = WalkSelection(op.Item.Item2)
            };
        }

        private List<Field> WalkSelection(FSharpList<GraphQLParser.Selection> selection)
        {
            return selection.Select(f => new Field
            {
                Name = f.name,
                Alias = f.alias?.Value ?? f.name,
                Fields = WalkSelection(f.selectionSet)
            }).ToList();
        }
    }

    internal static class Lexer
    {
        public enum TokenKind
        {
            EOF,
            Bang,
            Dollar,
            LParen,
            RParen,
            Spread,
            Colon,
            Equals,
            At,
            LBracket,
            RBracket,
            LBrace,
            RBrace,
            Pipe,
            Name,
            Variable,
            Int,
            Float,
            String
        }

        public class Token
        {
            public Token(TokenKind kind, int start, int end, string value = null)
            {
                TokenKind = kind;
                Start = start;
                End = end;
                Value = value;
            }

            public readonly TokenKind TokenKind;
            public readonly int Start;
            public readonly int End;
            public readonly string Value;
        }

        internal static IEnumerable<Token> Lex(string source)
        {
            for (var i = 0; i < source.Length;)
            {
                var token = ReadToken(source, i);
                i = token.End;
                yield return token;
            }
        }

        private static Token ReadToken(string source, int position)
        {
            position = GetPositionAfterWhitespace(source, position);
            if (position >= source.Length)
                return new Token(TokenKind.EOF, position, position);

            var chr = source[position];

            switch (chr)
            {
                case '!': return new Token(TokenKind.Bang, position, position + 1);
                case '$': return new Token(TokenKind.Dollar, position, position + 1);
                case '(': return new Token(TokenKind.LParen, position, position + 1);
                case ')': return new Token(TokenKind.RParen, position, position + 1);
                case ':': return new Token(TokenKind.Colon, position, position + 1);
                case '=': return new Token(TokenKind.Equals, position, position + 1);
                case '@': return new Token(TokenKind.At, position, position + 1);
                case '[': return new Token(TokenKind.LBracket, position, position + 1);
                case ']': return new Token(TokenKind.RBracket, position, position + 1);
                case '{': return new Token(TokenKind.LBrace, position, position + 1);
                case '}': return new Token(TokenKind.RBrace, position, position + 1);
                case '|': return new Token(TokenKind.Pipe, position, position + 1);
                case 'A': case 'B': case 'C': case 'D': case 'E': case 'F': case 'G':
                case 'H': case 'I': case 'J': case 'K': case 'L': case 'M': case 'N':
                case 'O': case 'P': case 'Q': case 'R': case 'S': case 'T': case 'U':
                case 'V': case 'W': case 'X': case 'Y': case 'Z':
                case 'a': case 'b': case 'c': case 'd': case 'e': case 'f': case 'g':
                case 'h': case 'i': case 'j': case 'k': case 'l': case 'm': case 'n':
                case 'o': case 'p': case 'q': case 'r': case 's': case 't': case 'u':
                case 'v': case 'w': case 'x': case 'y': case 'z':
                case '_':
                    return ReadName(source, position);
                case '0': case '1': case '2': case '3': case '4':
                case '5': case '6': case '7': case '8': case '9':
                case '-':
                    return ReadNumber(source, position);
                case '"':
                    return ReadString(source, position);
                case '.':
                    if (position + 2 < source.Length && source[position + 1] == '.' && source[position + 2] == '.')
                        return new Token(TokenKind.Spread, position, position + 3);
                    goto default;
                default:
                    throw new LexerException(source, position, $"Unexpected character {chr}");
            }
        }

        private static int GetPositionAfterWhitespace(string source, int position)
        {
            while(position < source.Length)
            {
                var chr = source[position];
                if (IsWhitespaceCharacter(chr))
                {
                    position++;
                }
                else if (chr == '#')
                {
                    position++;
                    while (position < source.Length && !IsLineTerminator(source[position]))
                        position++;
                } else
                {
                    break;
                }
            }

            return position;
        }

        private static bool IsWhitespaceCharacter(char chr)
        {
            return chr == ' '
                || chr == ','       // graphQL treats commas like whitespace
                || IsLineTerminator(chr)
                || chr == '\t'
                || chr == '\xa0'    // nbsp
                || chr == 11
                || chr == 12
                ;
        }

        private static bool IsLineTerminator(char chr)
        {
            return chr == '\r'
                || chr == '\n'
                || chr == '\x2028'  // line separator
                || chr == '\x2029'  // paragraph separator
                ;
        }

        private static Token ReadNumber(string source, int start)
        {
            var position = start;
            var chr = source[position];
            var isFloat = false;

            if (chr == '-')
                chr = source[++position];

            if (chr == '0')
            {
                chr = source[++position]; // ignore leading zeros
            }
            else if (chr >= 48 && chr <= 57)
            {
                do
                {
                    chr = source[++position];
                } while (chr >= 48 && chr <= 57);
            }
            else
            {
                throw new LexerException(source, position, "Invalid number");
            }

            if (chr == '.')
            {
                isFloat = true;

                chr = source[++position];
                if (chr >= 48 && chr <= 57)
                {
                    do
                    {
                        chr = source[++position];
                    } while (chr >= 48 && chr <= 57);
                }
                else
                {
                    throw new LexerException(source, position, "Invalid number");
                }

                if (chr == 'e')
                {
                    chr = source[++position];
                    if (chr == '-')
                        chr = source[++position];
                    if (chr >= 48 && chr <= 57)
                    {
                        do
                        {
                            chr = source[++position];
                        } while (chr >= 48 && chr <= 57);
                    }
                    else
                    {
                        throw new LexerException(source, position, "Invalid number");
                    }
                }
            }

            return new Token(isFloat ? TokenKind.Float : TokenKind.Int, start, position, source.Substring(start, position - start));
        }

        private static Token ReadString(string source, int start)
        {
            var position = start++;

            throw new NotImplementedException();
        }

        private static Token ReadName(string source, int position)
        {
            throw new NotImplementedException();
        }
    }

    public class LexerException : Exception
    {
        public LexerException(string text, int position, string message) : base(message)
        {
            Text = text;
            Position = position;
        }

        public string Text { get; set; }
        public int Position { get; set; }
    }
}
