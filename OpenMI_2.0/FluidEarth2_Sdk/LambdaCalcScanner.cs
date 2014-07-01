#region GNU GPL - Fluid Earth SDK
/*
    This file is part of 'Fluid Earth SDK'.

    'Fluid Earth SDK' is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    'Fluid Earth SDK' is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with 'Fluid Earth SDK'.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion GNU GPL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace FluidEarth2.Sdk
{
    // Extended, but credit to 
    // http://blogs.msdn.com/b/haniatassi/archive/2008/10/23/writing-a-simple-scanner-in-.net.aspx
    // for basics

    // Tokens that represent the input
    internal enum Token
    {
        OpenParan, CloseParan,
        Arrow,
        Comma,
        Plus, Minus, Multiply, Divide,
        Sin, Cos, Tan, Sinh, Cosh, Tanh, Asin, Acos, Atan,
        Log, Log10, Sqrt, Abs, PI, Ceiling, Floor,
        Constant,
        Variable,
        Other       // Represents unrecognized charachters
    }

    // Scanner used to find the tokens from a calc lampda string expression
    internal static class LambdaCalcScanner
    {
        // The pattern used with the regular expression class to scan the input
        const string Pattern = @"
                (?'OpenParan' \( ) | (?'CloseParan' \) ) |
                (?'Arrow' => ) |
                (?'Comma' ,  ) |
                (?'Plus' \+ ) | (?'Minus' - ) | (?'Multiply' \* ) | (?'Divide' / ) |
                (?'Sin' Sin) | (?'Cos' Cos ) | (?'Tan' Tan ) |
                (?'Sinh' Sinh) | (?'Cosh' Cosh ) | (?'Tanh' Tanh ) |
                (?'Asin' Asin) | (?'Acos' Acos ) | (?'Atan' Atan ) |
                (?'Log' Log) | (?'Log10' Log10 ) |
                (?'Sqrt' Sqrt) | (?'Abs' Abs ) | (?'PI' PI ) |
                (?'Ceiling' Ceiling) | (?'Floor' Floor ) |
                (?'Constant' (\.\d+|\d+(\.\d+)?) ) |
                (?'Variable' [a-zA-Z]\w* ) |
                (?'Other' [^ \r\n\t])";

        // Regular expression used to scan the input
        private static Regex MathRegex = new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        // Enumurable to get tokens from the given expression (scanner)
        public static IEnumerable<TokenEntity> GetLambdaCalcTokens(this string exp)
        {
            Token[] tokens = Enum.GetValues(typeof(Token)).OfType<Token>().ToArray();
            foreach (Match m in MathRegex.Matches(exp))
            {
                // Check which token is matched by this match object
                foreach (Token token in tokens)
                {
                    if (m.Groups[token.ToString()].Success)
                    {
                        yield return new TokenEntity(
                            token,
                            m.Index,
                            m.Value);
                    }
                }
            }
            // return the end string token, to indecate we are done
            yield return new TokenEntity(Token.Other, exp.Length, "\0");
        }
    }

    // Holds token info
    internal class TokenEntity
    {
        public TokenEntity(Token token, int startPos, string value)
        {
            this.Token = token;
            this.StartPos = startPos;
            this.Value = value;
        }

        // Token type
        public Token Token { get; private set; }

        // Start position in the original string
        public int StartPos { get; private set; }

        // Value
        public string Value { get; private set; }

        public override string ToString()
        {
            return string.Format("{0} at {1}: {2}", Token, StartPos, Value);
        }
    }
}

