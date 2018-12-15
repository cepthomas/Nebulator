using System;
using System.Collections.Generic;


namespace Nebulator.Common
{
    /// <summary>
    /// Unit of time.
    /// </summary>
    public class Time
    {
        #region Constants
        /// <summary>Subdivision setting.</summary>
        public const int TOCKS_PER_TICK = 96;
        #endregion

        #region Properties
        /// <summary>Left of the decimal point.</summary>
        public int Tick { get; set; } = 0;

        /// <summary>Right of the decimal point.</summary>
        public int Tock { get; set; } = 0;

        /// <summary>Total Tocks for the unit of time.</summary>
        public int TotalTocks { get { return Tick * TOCKS_PER_TICK + Tock; } }

        /// <summary>Convert to double representation.</summary>
        public double AsDouble { get { return Tick + Tock / 100.0; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Time()
        {
            Tick = 0;
            Tock = 0;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Time(Time note)
        {
            Tick = note.Tick;
            Tock = note.Tock;
        }

        /// <summary>
        /// Constructor from discrete components.
        /// </summary>
        /// <param name="tick"></param>
        /// <param name="tock">Tock to set - can be negative.</param>
        public Time(int tick, int tock)
        {
            if (tick < 0)
            {
                //throw new Exception("Negative value is invalid");
                tick = 0;
            }

            if (tock >= 0)
            {
                Tick = tick + tock / TOCKS_PER_TICK;
                Tock = tock % TOCKS_PER_TICK;
            }
            else
            {
                int atock = Math.Abs(tock);
                Tick = tick - (atock / TOCKS_PER_TICK) - 1;
                Tock = TOCKS_PER_TICK - (atock % TOCKS_PER_TICK);
            }
        }

        /// <summary>
        /// Constructor from tocks.
        /// </summary>
        /// <param name="tocks"></param>
        public Time(int tocks)
        {
            if (tocks < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            Tick = tocks / TOCKS_PER_TICK;
            Tock = tocks % TOCKS_PER_TICK;
        }

        /// <summary>
        /// Constructor from tocks.
        /// </summary>
        /// <param name="tocks"></param>
        public Time(long tocks)
        {
            if(tocks < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            Tick = (int)(tocks / TOCKS_PER_TICK);
            Tock = (int)(tocks % TOCKS_PER_TICK);
        }

        /// <summary>
        /// Constructor from Tick.Tock representation as a double.
        /// </summary>
        /// <param name="tts"></param>
        public Time(double tts)
        {
            if (tts < 0)
            {
                throw new Exception("Negative value is invalid");
            }

            var (integral, fractional) = Utils.SplitDouble(tts);
            Tick = (int)integral;
            Tock = (int)(fractional * 100);
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

            return Tick.Equals(other.Tick) && Tock.Equals(other.Tock);
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

            return (obj1.Tick == obj2.Tick && obj1.Tock == obj2.Tock);
        }

        public static bool operator !=(Time obj1, Time obj2)
        {
            return !(obj1 == obj2);
        }

        public static bool operator >(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTocks > t2.TotalTocks;
        }

        public static bool operator >=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTocks >= t2.TotalTocks;
        }

        public static bool operator <(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTocks < t2.TotalTocks;
        }

        public static bool operator <=(Time t1, Time t2)
        {
            return t1 is null || t2 is null || t1.TotalTocks <= t2.TotalTocks;
        }

        public static Time operator +(Time t1, Time t2)
        {
            int tick = t1.Tick + t2.Tick + (t1.Tock + t2.Tock) / TOCKS_PER_TICK;
            int tock = (t1.Tock + t2.Tock) % TOCKS_PER_TICK;
            return new Time(tick, tock);
        }

        public override int GetHashCode()
        {
            return TotalTocks;
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Move to the next tock and update clock.
        /// </summary>
        /// <returns>True if it's a new Tick.</returns>
        public bool Advance()
        {
            bool newTick = false;
            Tock++;

            if(Tock >= TOCKS_PER_TICK)
            {
                Tick++;
                Tock = 0;
                newTick = true;
            }

            return newTick;
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        public void Reset()
        {
            Tick = 0;
            Tock = 0;
        }

        /// <summary>
        /// Round up to next Tick.
        /// </summary>
        public void RoundUp()
        {
            if(Tock != 0)
            {
                Tick++;
                Tock = 0;
            }
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"{Tick:000}.{Tock:000}";
        }
        #endregion
    }
}
