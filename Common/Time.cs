using System;
using System.Collections.Generic;
using NBagOfTricks;


namespace Nebulator.Common
{
    /// <summary>
    /// Unit of musical time.
    /// </summary>
    public class Time
    {
        #region Constants
        /// <summary>
        /// Subdivision setting per beat/quarter note. 4 means 1/16 note resolution, 8 means 1/32 note, etc.
        /// </summary>
        /// <remarks>
        /// If we use ppq of 8 (32nd notes):
        ///   - 100 bpm = 800 subdiv/min = 13.33 subdiv/sec = 0.01333 subdiv/msec = 75.0 msec/subdiv
        ///   - 99 bpm = 792 subdiv/min = 13.20 subdiv/sec = 0.0132 subdiv/msec  = 75.757 msec/subdiv
        /// </remarks>
        public const int SubdivsPerBeat  = 8;
        #endregion

        #region Properties
        /// <summary>Left of the decimal point. From 0 to N.</summary>
        public int Beat { get; set; } = 0;

        /// <summary>Right of the decimal point. From 0 to SubdivsPerBeat-1.</summary>
        public int Subdiv { get; set; } = 0;

        /// <summary>Total subdivisions for the unit of time.</summary>
        public int TotalSubdivs { get { return Beat * SubdivsPerBeat + Subdiv; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Time()
        {
            Beat = 0;
            Subdiv = 0;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Time(Time other)
        {
            Beat = other.Beat;
            Subdiv = other.Subdiv;
        }

        /// <summary>
        /// Constructor from discrete parts.
        /// </summary>
        /// <param name="beat"></param>
        /// <param name="subdiv">Sub to set - can be negative.</param>
        public Time(int beat, int subdiv)
        {
            if (beat < 0)
            {
                //throw new Exception("Negative value is invalid");
                beat = 0;
            }

            if (subdiv >= 0)
            {
                Beat = beat + subdiv / SubdivsPerBeat;
                Subdiv = subdiv % SubdivsPerBeat;
            }
            else
            {
                subdiv = Math.Abs(subdiv);
                Beat = beat - (subdiv / SubdivsPerBeat) - 1;
                Subdiv = SubdivsPerBeat - (subdiv % SubdivsPerBeat);
            }
        }

        /// <summary>
        /// Constructor from total subdivs.
        /// </summary>
        /// <param name="subdivs"></param>
        public Time(int subdivs)
        {
            if (subdivs < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            Beat = subdivs / SubdivsPerBeat;
            Subdiv = subdivs % SubdivsPerBeat;
        }

        /// <summary>
        /// Constructor from total subdivs.
        /// </summary>
        /// <param name="subdivs"></param>
        public Time(long subdivs) : this((int)subdivs)
        {
        }

        /// <summary>
        /// Constructor from Beat.Subdiv representation as a double. TODO2 a bit crude but other ways (e.g. string) clutter the syntax.
        /// </summary>
        /// <param name="tts"></param>
        public Time(double tts)
        {
            if (tts < 0)
            {
                throw new Exception($"Negative value is invalid: {tts}");
            }

            var (integral, fractional) = MathUtils.SplitDouble(tts);
            Beat = (int)integral;
            Subdiv = (int)Math.Round(fractional * 10.0);

            if (Subdiv >= SubdivsPerBeat)
            {
                throw new Exception($"Invalid subdiv value: {tts}");
            }

            Subdiv = (int)(fractional * 100);
        }
        #endregion

        #region Overrides and operators for custom classess
        // Compare contents.
        public override bool Equals(object? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.GetType() == GetType() && Equals((Time)other);
        }

        // Compare contents.
        public bool Equals(Time other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Beat.Equals(other.Beat) && Subdiv.Equals(other.Subdiv);
        }

        // Compare identity.
        public static bool operator ==(Time obj1, Time obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (obj1 is null)
            {
                return false;
            }

            if (obj2 is null)
            {
                return false;
            }

            return (obj1.Beat == obj2.Beat && obj1.Subdiv == obj2.Subdiv);
        }

        // Compare identity.
        public static bool operator !=(Time obj1, Time obj2)
        {
            return !(obj1 == obj2);
        }

        // Compare identity.
        public static bool operator >(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalSubdivs > t2.TotalSubdivs;
        }

        // Compare identity.
        public static bool operator >=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalSubdivs >= t2.TotalSubdivs;
        }

        // Compare identity.
        public static bool operator <(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalSubdivs < t2.TotalSubdivs;
        }

        // Compare identity.
        public static bool operator <=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalSubdivs <= t2.TotalSubdivs;
        }

        public static Time operator +(Time t1, Time t2)
        {
            int beat = t1.Beat + t2.Beat + (t1.Subdiv + t2.Subdiv) / SubdivsPerBeat;
            int incr = (t1.Subdiv + t2.Subdiv) % SubdivsPerBeat;
            return new Time(beat, incr);
        }

        public override int GetHashCode()
        {
            return TotalSubdivs;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Move to the next subdiv and update clock.
        /// </summary>
        /// <returns>True if it's a new beat.</returns>
        public bool Advance()
        {
            bool newdiv = false;
            Subdiv++;

            if(Subdiv >= SubdivsPerBeat)
            {
                Beat++;
                Subdiv = 0;
                newdiv = true;
            }

            return newdiv;
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        public void Reset()
        {
            Beat = 0;
            Subdiv = 0;
        }

        /// <summary>
        /// Round up to next beat.
        /// </summary>
        public void RoundUp()
        {
            if(Subdiv != 0)
            {
                Beat++;
                Subdiv = 0;
            }
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"{Beat}.{Subdiv}";
        }
        #endregion
    }
}
