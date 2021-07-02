using System;
using System.Collections.Generic;
using NBagOfTricks;


namespace Nebulator.Common
{
    /// <summary>
    /// Unit of time.
    /// msec per tick = 1000/((bpm*TICKS_PER_BEAT)/60)
    ///               = 60*1000/bpm*TICKS_PER_BEAT
    /// At 100 bpm 4 TICKS_PER_BEAT gives note spacing of 150 msec.
    /// </summary>
    public class Time
    {
        #region Constants
        /// <summary>Subdivision setting. 4 means 1/16 notes, 8 means 1/32 notes.</summary>
        public const int TICKS_PER_BEAT  = 4;
        #endregion

        #region Properties
        /// <summary>Left of the decimal point. From 0 to N.</summary>
        public int Beat { get; set; } = 0;

        /// <summary>Right of the decimal point. From 0 to TICKS_PER_BEAT-1.</summary>
        public int Tick { get; set; } = 0;

        /// <summary>Total ticks for the unit of time.</summary>
        public int TotalTicks { get { return Beat * TICKS_PER_BEAT + Tick; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Time()
        {
            Beat = 0;
            Tick = 0;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Time(Time other)
        {
            Beat = other.Beat;
            Tick = other.Tick;
        }

        /// <summary>
        /// Constructor from discrete parts.
        /// </summary>
        /// <param name="beat"></param>
        /// <param name="tick">Sub to set - can be negative.</param>
        public Time(int beat, int tick)
        {
            if (beat < 0)
            {
                //throw new Exception("Negative value is invalid");
                beat = 0;
            }

            if (tick >= 0)
            {
                Beat = beat + tick / TICKS_PER_BEAT;
                Tick = tick % TICKS_PER_BEAT;
            }
            else
            {
                tick = Math.Abs(tick);
                Beat = beat - (tick / TICKS_PER_BEAT) - 1;
                Tick = TICKS_PER_BEAT - (tick % TICKS_PER_BEAT);
            }
        }

        /// <summary>
        /// Constructor from total ticks.
        /// </summary>
        /// <param name="ticks"></param>
        public Time(int ticks)
        {
            if (ticks < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            Beat = ticks / TICKS_PER_BEAT;
            Tick = ticks % TICKS_PER_BEAT;
        }

        /// <summary>
        /// Constructor from total ticks.
        /// </summary>
        /// <param name="ticks"></param>
        public Time(long ticks) : this((int)ticks)
        {
        }

        /// <summary>
        /// Constructor from Beat.Tick representation as a double.
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

            if (fractional >= TICKS_PER_BEAT)
            {
                throw new Exception("Invalid tick value");
            }

            Tick = (int)(fractional * 100);
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

            return Beat.Equals(other.Beat) && Tick.Equals(other.Tick);
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

            return (obj1.Beat == obj2.Beat && obj1.Tick == obj2.Tick);
        }

        public static bool operator !=(Time obj1, Time obj2)
        {
            return !(obj1 == obj2);
        }

        public static bool operator >(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTicks > t2.TotalTicks;
        }

        public static bool operator >=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTicks >= t2.TotalTicks;
        }

        public static bool operator <(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTicks < t2.TotalTicks;
        }

        public static bool operator <=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTicks <= t2.TotalTicks;
        }

        public static Time operator +(Time t1, Time t2)
        {
            int beat = t1.Beat + t2.Beat + (t1.Tick + t2.Tick) / TICKS_PER_BEAT;
            int incr = (t1.Tick + t2.Tick) % TICKS_PER_BEAT;
            return new Time(beat, incr);
        }

        public override int GetHashCode()
        {
            return TotalTicks;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Move to the next tick and update clock.
        /// </summary>
        /// <returns>True if it's a new beat.</returns>
        public bool Advance()
        {
            bool newTick = false;
            Tick++;

            if(Tick >= TICKS_PER_BEAT)
            {
                Beat++;
                Tick = 0;
                newTick = true;
            }

            return newTick;
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        public void Reset()
        {
            Beat = 0;
            Tick = 0;
        }

        /// <summary>
        /// Round up to next beat.
        /// </summary>
        public void RoundUp()
        {
            if(Tick != 0)
            {
                Beat++;
                Tick = 0;
            }
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"{Beat:00}.{Tick:00}";
        }
        #endregion
    }
}
