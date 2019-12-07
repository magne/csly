﻿using System;

namespace sly.parser.generator
{
    public enum Associativity
    {
        None = 0,
        Left = 1,
        Right = 2
    }


    public enum Affix
    {
        NotOperator = 0,
        PreFix = 1,
        InFix = 2,
        PostFix = 3
    }


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OperationAttribute : Attribute //where IN : struct
    {
        /// <summary>
        ///     token as an int as attribute can not be generics.
        /// </summary>
        /// <param name="token">token enum as int value</param>
        /// <param name="affix">operator arity</param>
        /// <param name="assoc">operator aosociativity (<see cref="Associativity" />) </param>
        /// <param name="precedence">precedence level: the greater, the higher</param>
        public OperationAttribute(int token, Affix affix, Associativity assoc, int precedence)
        {
            Token = token;
            Affix = affix;
            Assoc = assoc;
            Precedence = precedence;
        }

        public int Token { get; set; }

        public Affix Affix { get; set; }

        public Associativity Assoc { get; set; }

        public int Precedence { get; set; }
    }
}