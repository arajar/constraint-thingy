﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ConstraintThingy
{
    /// <summary>
    /// A Variable whose value can take any value from a finite set of atomic values
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebugString}")]
    public class FiniteDomainVariable : Variable<UInt64>
    {
        /// <summary>
        /// Creates a variable over a specified finite domain.  Variable starts as having all possible values in the domain.
        /// </summary>
        public FiniteDomainVariable(string name, FiniteDomain d) : base(name, ((ulong)1<<d.Size)-1)
        {
            Domain = d;
        }

        /// <summary>
        /// The domain of the variable
        /// </summary>
        public FiniteDomain Domain { get; private set; }

        /// <summary>
        /// Number of values from Domain that remain as candidate values.
        /// </summary>
        public int Candidates
        {
            get
            {
                UInt64 w = Value;
                w -= (w >> 1) & 0x5555555555555555UL;
                w = (w & 0x3333333333333333UL) + ((w >> 2) & 0x3333333333333333UL);
                w = (w + (w >> 4)) & 0x0f0f0f0f0f0f0f0fUL;
                return (int)((w*0x0101010101010101UL) >> 56);
            }
        }

        /// <summary>
        /// True if the variable has exactly one possible value, i.e. Candidates == 1
        /// </summary>
        public override bool IsUnique
        {
            get
            {
                // Fast test for whether Value is a power of 2 (i.e. has only a single bit set.
                return (Value != 0) && ((Value & (Value - 1)) == 0);
            }
        }

        /// <summary>
        /// True if variable has been narrowed to the empty set, i.e. has no consistent values.
        /// </summary>
        public override bool IsEmpty
        {
            get
            {
                return Value == 0;
            }
        }

        /// <summary>
        /// The elements that were narrowed away in the last update.
        /// </summary>
        public UInt64 NarrowedElements
        {
            get { return Value ^ PreviousValue; }
        }

        /// <summary>
        /// Try each remaining possible value of the variable, bind the variable to just that value, and yield.
        /// </summary>
        /// <returns>Always returns true.</returns>
        public override IEnumerable<bool> UniqueValues()
        {
            if (IsUnique)
                yield return true;
            else
            {
                Shuffler shuffler = new Shuffler(Domain.Size);
                int mark = SaveValues();
                for (int count = 0; count < Domain.Size; count++)
                {
                    int i = shuffler.Next();
                    // Try the ith element of Domain
                    UInt64 candidate = 1UL << i;
                    if ((Value & candidate) != 0)
                    {
                        bool succeeded = true;

                        TrySetValue(candidate, ref succeeded);

                        if (succeeded)
                            yield return false;
                        RestoreValues(mark);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the string name of the value of the variable, if the variable has been narrowed to a unique value, else null.
        /// </summary>
        public string UniqueValue
        {
            get
            {
                if (!IsUnique)
                    return null;
                else
                {
                    int index = (int)Math.Floor(Math.Log(Value)/Math.Log(2));
                    return Domain.Elements[index];
                }
            }

            set {
                int index = Array.IndexOf(Domain.Elements, value);
                if (index<0)
                    throw new ArgumentException("Unknown domain element: "+value);
                bool succeeded = true;
                TrySetValue(1UL << index, ref succeeded);
                if (!succeeded)
                    throw new Exception("Attempting to set FD variable to a unique string value failed.");
            }
        }

        string DebugString
        {
            get
            {
                StringBuilder b = new StringBuilder();
                b.Append(Name);
                b.Append(" = ");
                FormatValueString(b);
                return b.ToString();
            }
        }

        /// <summary>
        /// Formats the value of the variable in set notation, or as a single value, if it's singleton set.
        /// </summary>
        public string ValueString
        {
            get
            {
                if (IsUnique)
                    return UniqueValue;
                StringBuilder b = new StringBuilder();
                FormatValueString(b);
                return b.ToString();
            }
        }

        private void FormatValueString(StringBuilder b)
        {
            b.Append("{ ");
            bool firstone = true;
            for (int i = 0; i < Domain.Size; i++)
            {
                if ((Value & ((ulong) 1 << i)) != 0)
                {
                    if (!firstone)
                        b.Append(", ");
                    firstone = false;
                    b.Append(Domain.Elements[i]);
                }
            }
            b.Append(" }");
        }

        /// <summary>
        /// Renders the variable in the form "NAME={VALUES ...}".
        /// </summary>
        public override string ToString()
        {
            return DebugString;
        }

        /// <summary>
        /// True if the set of possible values for this variable contains any of the elements of set.
        /// </summary>
        public bool ContainsAny(UInt64 set)
        {
            return (set & Value) != 0;
        }

        /// <summary>
        /// True if the set of possible values for this variable contains all the elements of set.
        /// </summary>
        public bool ContainsAll(UInt64 set)
        {
            return (set & Value) == set;
        }
    }
}
