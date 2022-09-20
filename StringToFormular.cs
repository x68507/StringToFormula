using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CustomParser
{

    public class StringToFormula
    {
        private static string _and = "&";
        private static string _or = "|";
        private static string _userAnd = "&";
        private static string _userOr = "|";

        private string[] _operators = {
            "-", "+", "/", "*", "^" ,
            "!",
            "<", ">",
            "==", "!=", "<=", ">="};

        // left and right strings for equality expressions
        private string leftString = null;
        private string rightString = null;

        public string ErrorMessage = null;

        // ugh, Math.Pow() has a weirdness with negative numbers
        // https://stackoverflow.com/questions/14575697/math-pow-with-negative-numbers-and-non-integer-powers
        private Func<double, double, double>[] _operations = {
                (a1, a2) => a1 - a2,
                (a1, a2) => a1 + a2,
                (a1, a2) => a1 / a2,
                (a1, a2) => a1 * a2,
                (a1, a2) => a2 < 1 ? Math.Sign(a1) * Math.Pow(Math.Abs(a1), a2) : Math.Pow(a1, a2),

                (a1, a2) => a1 == 0 ? 1 : 0,

                (a1, a2) => a1 < a2 ? 1 : 0,
                (a1, a2) => a1 > a2 ? 1 : 0,

                (a1, a2) => a1 == a2 ? 1 : 0,
                (a1, a2) => a1 != a2 ? 1 : 0,
                (a1, a2) => a1 <= a2 ? 1 : 0,
                (a1, a2) => a1 >= a2 ? 1 : 0,
            };

        /// <summary>
        /// Settings for the parser.  Currently, you can change the Boolean And and Boolean Or operators.
        /// [default: and='&', or='|']
        /// </summary>
        /// <returns></returns>
        public S_Parser GetSettings()
        { return new S_Parser(); }

        public class S_Parser
        {
            /// <summary>
            /// String representing boolean AND logic.  Default is '&'
            /// </summary>
            public string AndString { get => _userAnd; set => _userAnd = value; }
            /// <summary>
            /// String representing boolean OR logic.  Default is '|'
            /// </summary>
            public string OrString { get => _userOr; set => _userOr = value; }
        }
        
       

        /// <summary>
        /// Evaluates the expression or equality. 
        /// Handles and parses all math expression with the order of operations following PEMDAS
        /// If equality, False = 0 and True = 1
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public double Eval(string expression)
        {
            // just do it the easy way...there are only 6 possibilities
            string eq = null;
            if (expression.Contains("=="))
            { eq = "=="; }
            else if (expression.Contains("!="))
            { eq = "!="; }
            else if (expression.Contains("<="))
            { eq = "<="; }
            else if (expression.Contains(">="))
            { eq = ">="; }
            else if (expression.Contains("<"))
            { eq = "<"; }
            else if (expression.Contains(">"))
            { eq = ">"; }


            // there is always a leftstring...it's the rightstring==null which determines Equalities vs Expressions
            if (eq != null)
            {

                // let's split in left/right sides
                string[] parts = expression.Split(new[] { eq }, StringSplitOptions.None);

                if (parts.Length != 2)
                { return double.NaN; }

                leftString = parts[0];
                rightString = parts[1];
            }
            else
            {
                leftString = expression;
            }

            // let's actually calculate the express
            double res1 = CalculateExpression(leftString);

            // we have an equality and the "right side" has an expression to evaluate
            if (rightString != null)
            {
                double res2 = CalculateExpression(rightString);

                // since this is an equality, we should get either 0 or 1
                res1 = _operations[Array.IndexOf(_operators, eq)](res1, res2);
            }


            return res1;
        }

        /// <summary>
        /// Attempts to check & apply a NOT to the following expression.
        /// </summary>
        /// <param name="input">individual string to be parsed</param>
        /// <param name="val">current value being evaluated. If '!' exists, this will be the new value </param>
        /// <returns></returns>
        private bool TryNegation(string input, ref double val)
        {
            bool res = true; ;


            if (
                input[0] == '!' &&
                double.TryParse(input.Substring(1), out double temp)
                )
            {
                val = temp == 0 ? 1 : 0;
            }

            return res;
        }


        /// <summary>
        /// Calculates an individual expression.
        /// There should be no equalities/inequalities in this expression.
        /// 
        /// The Eval function should have already split on equalities
        /// </summary>
        /// <param name="expression">Math expresion</param>
        /// <returns></returns>
        private double CalculateExpression(string expression)
        {

            // m_todo: handle boolean operations such as && and ||
            List<string> tokens;
            bool success = Tokenize(expression, out tokens);

            if (tokens.Count == 1)
            {
                double temp = double.NaN;

                // need to try to check for negate
                if (double.TryParse(tokens[0], out double t2))
                {
                    temp = t2;
                }
                else
                {
                    TryNegation(tokens[0], ref temp);
                }
                return temp;
            }


            if (!success)
            {
                ErrorMessage = "Could not properly tokenize the input string";
                return double.NaN;
            }

            double res = double.NaN;


            // let's loop through and build up substring in between parenthesis
            int open = tokens.Where(x => x == "(").Count();
            int close = tokens.Where(x => x == ")").Count();
            if (open != close)
            {
                ErrorMessage = "Mis-match in number of parentheis";
                return res;
            }

            double subRes = double.NaN; ;

            // we have substrings
            while (open > 0)
            {
                int sidx = tokens.LastIndexOf("(");
                int eidx = tokens.IndexOf(")", sidx);


                List<string> sub = tokens.GetRange(sidx + 1, eidx - sidx - 1);

                subRes = Pre_EMDAS(sub);

                // need to also remove the parenthesis
                tokens.RemoveRange(sidx, eidx - sidx + 1);
                tokens.Insert(sidx, subRes.ToString());

                // need to customize this for negation...see if we now have a !
                if (sidx > 0 && tokens[sidx - 1] == "!")
                {
                    tokens[sidx - 1] = tokens[sidx - 1] + tokens[sidx];
                    tokens.RemoveAt(sidx);

                    // let's just do the negation here rather than at the end
                    if (TryNegation(tokens[sidx - 1], ref subRes))
                    {
                        tokens[sidx - 1] = subRes.ToString();
                    }
                }

                open--;
            }

            // one final pemdas
            if (tokens.Count > 1)
            {
                subRes = Pre_EMDAS(tokens);
            }



            res = subRes;
            return res;
        }

        /// <summary>
        /// EMDAS pre-processor to separator expression between boolean operators.
        /// 
        /// If there are no boolean operators, then this is a EMDAS passthrough
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private double Pre_EMDAS(List<string> expression)
        {
            double res = double.NaN;
            int aa = expression.Where(x => x == _and).Count();
            int oo = expression.Where(x => x == _or).Count();

            if (aa == 0 && oo == 0)
            {
                res = EMDAS(expression);
            }
            else
            {
                // need to evaluate all the ANDs first and then evalulate the ORs
                int idx;
                List<List<string>> inputOrList = new List<List<string>>();
                List<List<string>> inputAndList = new List<List<string>>();
                List<double> outputOrList = new List<double>();
                List<double> outputAndList = new List<double>();

                int total, zeroes, ones;
                double AND, OR;

                // brute force method
                // 1. Break the expression into groupings based off of OR operator.
                //    If only AND operator, then we'll catch this with 'inputOrList.Count == 0'
                // 2. Evaluate each individual OR grouping.  These groupings will only have AND operators
                // 3. Each AND will either be all 0's or all non-0's.  If all are the same, return true
                // 4. Evaluate all OR groupings.  If even one grouping is non-0, return true

                //    NOTE: even if #3 returns all 0's, we still process #4.  This is inefficient but reliable

                while (oo > 0)
                {
                    // get first OR
                    idx = expression.IndexOf(_or);
                    // add left part to new list
                    inputOrList.Add(expression.GetRange(0, idx));
                    // remove left part & repeat
                    expression.RemoveRange(0, idx + 1);

                    // add the last right side to the list
                    if (oo == 1)
                    { inputOrList.Add(expression); }

                    // decrement
                    oo--;
                }

                // we have no OR operators but we still need something to pass to the AND operator
                if (inputOrList.Count == 0)
                { inputOrList.Add(expression); }

                // need to loop through the OR list and combine the ANDs
                foreach (var entry in inputOrList)
                {
                    inputAndList.Clear();
                    aa = entry.Where(x => x == _and).Count();

                    if (aa == 0)
                    {
                        // there is no AND operator. just add to input list to reuse logic
                        inputAndList.Add(entry);
                    }
                    else
                    {
                        while (aa > 0)
                        {
                            idx = entry.IndexOf(_and);
                            inputAndList.Add(entry.GetRange(0, idx));
                            entry.RemoveRange(0, idx + 1);

                            // add the last right side to the list
                            if (aa == 1)
                            { inputAndList.Add(entry); }

                            // decrement
                            aa--;
                        }
                    }



                    // convert entry to numeric values
                    outputAndList.Clear();
                    for (int i = 0; i < inputAndList.Count; i++)
                    { outputAndList.Add(EMDAS(inputAndList[i])); }

                    // convert numeric values to boolean
                    total = outputAndList.Count();
                    zeroes = outputAndList.Where(x => x == 0).Count();
                    ones = outputAndList.Where(x => x != 0).Count();

                    if (total == 1)
                    {
                        AND = ones > 0 ? 1.0 : 0.0;
                    }
                    else
                    {
                        AND = (total == zeroes || total == ones) ? 1.0 : 0.0;
                    }
                    outputOrList.Add(AND);
                }

                // evaluate the OR list
                total = outputOrList.Count();
                ones = outputOrList.Where(x => x != 0).Count();

                OR = ones > 0 ? 1.0 : 0.0;

                //return OR;

                res = OR; 
            }

            return res;
        }

        /// <summary>
        /// Applies the correct order of operations
        /// Exponent, Multiply/Divide, Add/Subtract (we're already inside any parenthesis)
        /// 
        /// We also account for boolean operations in this method
        /// </summary>
        /// <param name="expression">Math expresion</param>
        /// <returns></returns>
        private double EMDAS(List<string> expression)
        {
            double res = double.NaN;

            int E = expression.Where(x => x == "^").Count();
            int MD = expression.Where(x => x == "*" || x == "/").Count();
            int AS = expression.Where(x => x == "+" || x == "-").Count();

            string op;


            // let's take care of exponents first
            while (E > 0)
            {
                op = expression.Where(x => x == "^").First();
                subEMDAS(ref expression, op);
                E--;
            }

            // then do multiply/divide (make sure we go from left-to-right)
            while (MD > 0)
            {
                op = expression.Where(x => x == "*" || x == "/").First();
                subEMDAS(ref expression, op);
                MD--;
            }

            // finally do addition/subtraction (order doesn't matter but we'll still go from left-to-right)
            while (AS > 0)
            {
                op = expression.Where(x => x == "+" || x == "-").First();
                subEMDAS(ref expression, op);
                AS--;
            }

            // finally convert the last string back to a double (we should only have 1 index left)
            // sweet, this actually works because we 
            if (expression.Count == 1)
            {
                if (!double.TryParse(expression[0], out res))
                {
                    TryNegation(expression[0], ref res);
                }

            }


            return res;
        }


        /// <summary>
        /// Performs the actual math calculation for a1 & a2 based on the operator between the 2.
        /// The search will always be from left to right.
        /// The 3 indices from the list are removed and the output is placed back in the list at the correct index.
        /// 
        /// Should always be used in conjunction with EMDAS since this function ensures proper evaluation order
        /// </summary>
        /// <param name="expression">List of tokenized inputs</param>
        /// <param name="op">First instance of the operator to evaluate</param>
        private void subEMDAS(ref List<string> expression, string op)
        {

            int idx;
            double a1 = double.NaN;
            double a2 = double.NaN;
            double res;

            string s1, s2;

            idx = expression.IndexOf(op);
            if (idx == -1)
            { return; }

            s1 = expression[idx - 1];
            s2 = expression[idx + 1];


            if (!double.TryParse(s1, out a1))
            {
                a1 = double.NaN;
                TryNegation(s1, ref a1);

            }
            if (!double.TryParse(s2, out a2))
            {
                a2 = double.NaN;
                TryNegation(s2, ref a2);
            }

            expression.RemoveRange(idx - 1, 3);
            res = _operations[Array.IndexOf(_operators, op)](a1, a2);
            expression.Insert(idx - 1, res.ToString());

        }




        /// <summary>
        /// Splits the input string into pure numbers and mathematical operators
        /// </summary>
        /// <param name="expression">Expression without equality symbols</param>
        /// <param name="tokens">Tokenized version of the input expresion</param>
        /// <returns></returns>
        private bool Tokenize(string expression, out List<string> tokens)
        {
            List<string> _tokenizeSingleOperators = new List<string>() { "(", ")", "^", "*", "/", "+", "-", "<", ">", "=" };
            List<string> _alwaysBreak = new List<string>() { "(", ")", "^", "*", "/", "+", "-" };

            // we cannot tokenize on double characters easily
            // let's split on the new delimiter and then join on the single delimiter
            expression = string.Join("|", expression.Split(new string[] { _userOr }, StringSplitOptions.None));
            expression = string.Join("&", expression.Split(new string[] { _userAnd }, StringSplitOptions.None));

            // add single token operators to split array
            _tokenizeSingleOperators.Add("&");
            _tokenizeSingleOperators.Add("|");
            _alwaysBreak.Add("&");
            _alwaysBreak.Add("|");

            string[] tokenizeSingleOperators = _tokenizeSingleOperators.ToArray();
            string[] alwaysBreak = _alwaysBreak.ToArray();

            // we'll remove all the white spaces so we can split easier...we only care about numbers & operators
            string nospaces = expression.Replace(" ", string.Empty);

            // Excel replaces repeating + or - with single operator, so we will also
            nospaces = Regex.Replace(nospaces, @"-((-){2})*", "-");
            nospaces = Regex.Replace(nospaces, @"-{2,}", "+");
            nospaces = Regex.Replace(nospaces, @"\+{2,}", "+");
            nospaces = Regex.Replace(nospaces, @"\*\*", "^");

            // todo: this is ZPL specific logic...it probably does not belong in most c# parsers
            //       maybe should pull this into the PreParser and keep this C# specific
            nospaces = Regex.Replace(nospaces, @"!{2,}", "!");


            tokens = new List<string>();
            string str = null;
            bool wasToken = false;
            for (int i = 0; i < nospaces.Length; i++)
            {
                //char c = nospaces[i];
                string s = nospaces[i].ToString();
                bool isToken = Array.IndexOf(tokenizeSingleOperators, s) > -1;

                // parentheses are always their individual tokens
                if (Array.IndexOf(alwaysBreak, s) > -1)
                {
                    // we have previous information...need to push it
                    if (str != null)
                    { tokens.Add(str); }
                    str = null;

                    // add the operator
                    tokens.Add(s);
                }
                else
                {
                    // if we switch from non-token to token, we split
                    if (wasToken)
                    {
                        if (str != null)
                        {
                            str += s;
                            tokens.Add(str);
                            str = s;
                        }
                        else
                        {
                            str += s;
                        }
                    }
                    else
                    {
                        // non-token character...never break;
                        str += s;
                    }
                }

                // store whether the last character was a '+' or '-' (only dual purpose operators)
                wasToken = isToken && i > 0;
            }


            // we always need to push the str to the tokens
            if (str != null)
            { tokens.Add(str); }

            // now we're finally going to place the + & - correctly
            List<string> newTokens = new List<string>();
            if (tokens.Count > 1 && (tokens[0] == "+" || tokens[0] == "-"))
            {
                tokens[1] = tokens[0] + tokens[1];
                tokens.RemoveAt(0);
            }

            // loop through the rest of the 2-n values in the token
            newTokens.Add(tokens[0]);
            for (int i = 1; i < tokens.Count - 1; i++)
            {
                // we found a "hanging" plus or minus...attach it to the following number
                if (
                    (tokens[i] == "+" || tokens[i] == "-") &&
                    Array.IndexOf(tokenizeSingleOperators, tokens[i - 1]) > -1 &&
                    Array.IndexOf(tokenizeSingleOperators, tokens[i + 1]) == -1
                    )
                {
                    newTokens.Add(tokens[i] + tokens[i + 1]);
                    i++;

                }
                else
                {
                    newTokens.Add(tokens[i]);
                }

            }

            // sometimes skipping the indexer i by 2x in for loop, need to grab the last value
            string s1 = string.Join("", tokens);
            string s2 = string.Join("", newTokens);
            if (s1 != s2)
            { newTokens.Add(tokens[tokens.Count - 1]); }

            tokens.Clear();
            tokens = newTokens;

            // the token order should completely match the input string
            return string.Join("", tokens) == nospaces;
        }

    }


}
