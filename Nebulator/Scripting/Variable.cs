using System;
using System.Collections.Generic;


namespace Nebulator.Scripting
{
    public class Variable
    {
        /// <summary>Var name.</summary>
        public string Name { get; set; } = "";

        /// <summary>Value as int. It is initialized from the script supplied value.</summary>
        public int Value { get; set; } = 0;

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"Name:{Name} Value:{Value}";
        }
    }
}
