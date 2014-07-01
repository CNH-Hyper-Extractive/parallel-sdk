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
using System.Reflection;

namespace FluidEarth2.Sdk
{
    // Extended, but credit to 
    /// http://blogs.msdn.com/b/haniatassi/archive/2008/10/28/generating-the-lambda-expression-dynamically.aspx
    // for basics

    public class LambdaCalc
    {
        // Delegate that holds the compiled lampda expression
        public delegate double Func(params double[] args);

        TokenEntity lastToken;
        // Holds the tokens enumurator
        IEnumerator<TokenEntity> tokens;
        // Holds the list of parameters in the math equation
        List<string> parameters;
        // Holds the lampda expression parameter, this is the params double [] args
        ParameterExpression funcParamsArg = Expression.Parameter(typeof(double[]), "args");
        // Holds the parsed lambda expression
        Expression<Func> lambdaExp;
        // Holds the compiled delegate 
        Func function;
        string _lambdaExpression;

        /// <summary>
        /// Initialize the class from the given string expression
        /// </summary>
        /// <param name="exp">Input expression, ie: (a, b) => a + b * 2</param>
        public LambdaCalc(string exp)
        {
            try
            {
                _lambdaExpression = exp;
                this.lambdaExp = ParseExpression(exp);
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format(
                    "Invalid Lambda expression \"{0}\"", exp), e);
            }
        }

        public string LambdaExpression
        {
            get { return _lambdaExpression.Trim(); }
        }

        /// <summary>
        /// Gets the compiled delegate of the input expression
        /// </summary>
        public Func Function
        {
            get
            {
                if (this.function == null)
                {
                    this.function = this.lambdaExp.Compile();
                }
                return this.function;
            }
        }

        private TokenEntity CurrentToken { get { return this.tokens.Current; } }
        private TokenEntity LastToken { get { return this.lastToken; } }

        private bool AdvanceToken()
        {
            this.lastToken = CurrentToken;
            return this.tokens.MoveNext();
        }

        private bool CheckToken(Token token)
        {
            if (CurrentToken != null && CurrentToken.Token == token)
            {
                AdvanceToken();
                return true;
            }
            return false;
        }

        // Parse the given expression, expect ([[[<param1>],<param2>],..]) => <body>
        Expression<Func> ParseExpression(string exp)
        {
            this.tokens = exp.GetLambdaCalcTokens().GetEnumerator();
            AdvanceToken();

            ParseParameters();

            if (!CheckToken(Token.Arrow))
                throw GetErrorException("Expecting equation arrow.", CurrentToken);

            Expression body = BodyExpression();

            if (CurrentToken.Value != "\0")   // check if we didn't reach the end
                throw GetErrorException("Invalid equation syntax.", CurrentToken);

            Expression<Func> lambdaExpr = Expression.Lambda<Func>(body, this.funcParamsArg);

            return lambdaExpr;
        }

        // Parse the parameters list (a, b, ...)
        private void ParseParameters()
        {
            this.parameters = new List<string>();

            if (!CheckToken(Token.OpenParan))
                throw GetErrorException("Expecting open paran.", CurrentToken);

            if (!CheckToken(Token.CloseParan))
            {
                do
                {
                    if (!CheckToken(Token.Variable))
                        throw GetErrorException("Expecting a parameter.", CurrentToken);
                    // add parameter
                    this.parameters.Add(LastToken.Value.ToLower());
                } while (CheckToken(Token.Comma));
            }
            if (!CheckToken(Token.CloseParan))
                throw GetErrorException("Expecting close paran.", CurrentToken);
        }

        // Get a literal expression if found
        private Expression Literal()
        {
            Expression literal = null;
            if (CheckToken(Token.Constant))
            {
                literal = Expression.Constant(double.Parse(LastToken.Value));
            }
            return literal;
        }

        Expression Math1Arg()
        {
            if (CurrentToken == null)
                return null;

            TokenEntity current = CurrentToken;

            switch (CurrentToken.Token)
            {
                case Token.Sin:
                case Token.Cos:
                case Token.Tan:
                case Token.Sinh:
                case Token.Cosh:
                case Token.Tanh:
                case Token.Asin:
                case Token.Acos:
                case Token.Atan:
                case Token.Log:
                case Token.Log10:
                case Token.Sqrt:
                case Token.Abs:
                case Token.Ceiling:
                case Token.Floor:
                    AdvanceToken();
                    return Expression.Call(
                        typeof(Math).GetMethod(current.Token.ToString(), BindingFlags.Static | BindingFlags.Public),
                        Expression.Convert(Paran(), typeof(double)));
                case Token.PI:
                    AdvanceToken();
                    return Expression.Constant(Math.PI);
                default:
                    return null;
            }
            /*
            if (CurrentToken != null && CurrentToken.Token == token)
            {
                AdvanceToken();
                return true;
            }
            return false;

            if (CheckToken(Token.Sin))
            {
                trig = Expression.Call(
                    typeof(Math).GetMethod("Sin", BindingFlags.Static | BindingFlags.Public),
                     Expression.Convert(Paran(), typeof(double)));
            }
            else if (CheckToken(Token.Cos))
            {
                trig = Expression.Call(
                    typeof(Math).GetMethod("Cos", BindingFlags.Static | BindingFlags.Public),
                     Expression.Convert(Paran(), typeof(double)));
            }
            else if (CheckToken(Token.Tan))
            {
                trig = Expression.Call(
                    typeof(Math).GetMethod("Tan", BindingFlags.Static | BindingFlags.Public),
                     Expression.Convert(Paran(), typeof(double)));
            }
            
            return trig;
             */
        }


        // Get an identifier reference expression if found
        private Expression Identifier()
        {
            Expression identifier = null;
            if (CheckToken(Token.Variable))
            {
                int parameterIndex = this.parameters.IndexOf(LastToken.Value.ToLower());
                if (parameterIndex < 0)
                    throw GetErrorException("Invalid reference to '" + LastToken.Value + "'.", LastToken);
                identifier = Expression.ArrayIndex(this.funcParamsArg, Expression.Constant(parameterIndex));
            }
            return identifier;
        }

        // Parse a paranthasis expression (...)
        private Expression Paran()
        {
            Expression paran = null;
            if (CheckToken(Token.OpenParan))
            {
                paran = BodyExpression();
                if (!CheckToken(Token.CloseParan))
                    throw GetErrorException("A close paranthasis is missing.", LastToken);
            }
            return paran;
        }

        // Parse and returns the constant or variable expression
        private Expression PrimaryExpression()
        {
            Expression primaryExpression;
            if ((primaryExpression = Literal()) != null ||
                (primaryExpression = Identifier()) != null ||
                (primaryExpression = Paran()) != null ||
                (primaryExpression = Math1Arg()) != null)
            {
                return primaryExpression;
            }
            throw GetErrorException("Expecting a constant or parameter reference.", CurrentToken);
        }

        // Parse and returns the unary expression
        private Expression UnaryExpression()
        {
            if (CheckToken(Token.Plus) || CheckToken(Token.Minus))
            {
                if (LastToken.Token == Token.Minus)
                    return Expression.Negate(UnaryExpression());
                else
                    return UnaryExpression();
            }
            return PrimaryExpression();
        }

        // Parse and returns the multiplication/division expression 
        private Expression MultiplicativeExpression()
        {
            Expression multiplicativeExpr = UnaryExpression();
            while (CheckToken(Token.Multiply) || CheckToken(Token.Divide))
            {
                if (LastToken.Token == Token.Multiply)
                    multiplicativeExpr = Expression.Multiply(multiplicativeExpr, UnaryExpression());
                else if (LastToken.Token == Token.Divide)
                    multiplicativeExpr = Expression.Divide(multiplicativeExpr, UnaryExpression());
            }
            return multiplicativeExpr;
        }

        // Parse and returns the addition/subtraction expression 
        private Expression AdditiveExpression()
        {
            Expression additionExpr = MultiplicativeExpression();
            while (CheckToken(Token.Plus) || CheckToken(Token.Minus))
            {
                if (LastToken.Token == Token.Plus)
                    additionExpr = Expression.Add(additionExpr, MultiplicativeExpression());
                else if (LastToken.Token == Token.Minus)
                    additionExpr = Expression.Subtract(additionExpr, MultiplicativeExpression());
            }
            return additionExpr;
        }

        // Parse and returns the body expression
        private Expression BodyExpression()
        {
            return AdditiveExpression();
        }

        // Returns back an error exception
        private Exception GetErrorException(string p, TokenEntity tokenEntity)
        {
            return new Exception(string.Format("Error at '{0}': {1}", tokenEntity != null ? tokenEntity.StartPos : 0, p));
        }
    }

        /// <summary>
        /// Test program
        /// </summary>
        //class Program
        //{
        //    static void Main(string[] args)
        //    {
        //        LambdaCalc lc = new LambdaCalc("(a, b) => -a + 2 * b");
        //        Console.WriteLine(lc.Function(1, 2));
        //    }
        //}
}

