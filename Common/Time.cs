using System;
using System.Collections.Generic;
using NBagOfTricks;


namespace Nebulator.Common
{
    /// <summary>
    /// Unit of time.
    /// </summary>
    public class Time
    {
        #region Constants
        /// <summary>Subdivision setting.</summary>
        public const int BEATS_PER_MEAS = 4;//TODO needed?

        /// <summary>Subdivision setting. Currently 1/16 notes.</summary>
        public const int INCRS_PER_BEAT  = 4;
        #endregion

        #region Properties
        /// <summary>Left of the decimal point.</summary>
        public int Beat { get; set; } = 0;

        /// <summary>Right of the decimal point.</summary>
        public int Increment { get; set; } = 0;

        /// <summary>Total subs for the unit of time.</summary>
        public int TotalIncrs { get { return Beat * INCRS_PER_BEAT + Increment; } }

        /// <summary>Convert to double representation.</summary>
        public double AsDouble { get { return Beat + Increment / 100.0; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Time()
        {
            Beat = 0;
            Increment = 0;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Time(Time other)
        {
            Beat = other.Beat;
            Increment = other.Increment;
        }

        /// <summary>
        /// Constructor from discrete components.
        /// </summary>
        /// <param name="beat"></param>
        /// <param name="incr">Sub to set - can be negative.</param>
        public Time(int beat, int incr)
        {
            if (beat < 0)
            {
                //throw new Exception("Negative value is invalid");
                beat = 0;
            }

            if (incr >= 0)
            {
                Beat = beat + incr / INCRS_PER_BEAT;
                Increment = incr % INCRS_PER_BEAT;
            }
            else
            {
                incr = Math.Abs(incr);
                Beat = beat - (incr / INCRS_PER_BEAT) - 1;
                Increment = INCRS_PER_BEAT - (incr % INCRS_PER_BEAT);
            }
        }

        /// <summary>
        /// Constructor from total incrs.
        /// </summary>
        /// <param name="incrs"></param>
        public Time(int incrs)
        {
            if (incrs < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            Beat = incrs / INCRS_PER_BEAT;
            Increment = incrs % INCRS_PER_BEAT;
        }

        /// <summary>
        /// Constructor from total incrs.
        /// </summary>
        /// <param name="incrs"></param>
        public Time(long incrs)
        {
            if(incrs < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            Beat = (int)(incrs / INCRS_PER_BEAT);
            Increment = (int)(incrs % INCRS_PER_BEAT);
        }

        /// <summary>
        /// Constructor from Beat.Incr representation as a double.
        /// </summary>
        /// <param name="tts"></param>
        public Time(double tts)
        {
            if (tts < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            var (integral, fractional) = MathUtils.SplitDouble(tts);
            Beat = (int)integral;
            Increment = (int)(fractional * 100);
        }
        #endregion

        #region Overrides and operators for custom classess
        // The Equality Operator (==) is the comparison operator and the Equals() method compares the contents of a string.
        // The == Operator compares the reference identity while the Equals() method compares only contents.

        public override bool Equals(object other)
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

            return Beat.Equals(other.Beat) && Increment.Equals(other.Increment);
        }

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

            return (obj1.Beat == obj2.Beat && obj1.Increment == obj2.Increment);
        }

        public static bool operator !=(Time obj1, Time obj2)
        {
            return !(obj1 == obj2);
        }

        public static bool operator >(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalIncrs > t2.TotalIncrs;
        }

        public static bool operator >=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalIncrs >= t2.TotalIncrs;
        }

        public static bool operator <(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalIncrs < t2.TotalIncrs;
        }

        public static bool operator <=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalIncrs <= t2.TotalIncrs;
        }

        public static Time operator +(Time t1, Time t2)
        {
            int beat = t1.Beat + t2.Beat + (t1.Increment + t2.Increment) / INCRS_PER_BEAT;
            int incr = (t1.Increment + t2.Increment) % INCRS_PER_BEAT;
            return new Time(beat, incr);
        }

        public override int GetHashCode()
        {
            return TotalIncrs;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Move to the next increment and update clock.
        /// </summary>
        /// <returns>True if it's a new beat.</returns>
        public bool Advance()
        {
            bool newIncr = false;
            Increment++;

            if(Increment >= INCRS_PER_BEAT)
            {
                Beat++;
                Increment = 0;
                newIncr = true;
            }

            return newIncr;
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        public void Reset()
        {
            Beat = 0;
            Increment = 0;
        }

        /// <summary>
        /// Round up to next beat.
        /// </summary>
        public void RoundUp()
        {
            if(Increment != 0)
            {
                Beat++;
                Increment = 0;
            }
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"{Beat:00}.{Increment:00}";
        }
        #endregion
    }
}
