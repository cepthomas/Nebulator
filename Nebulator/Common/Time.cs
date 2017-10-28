using System;
using System.Collections.Generic;


namespace Nebulator.Common
{
    /// <summary>
    /// Unit of time.
    /// </summary>
    public class Time
    {
        /// <summary>Left of the decimal point.</summary>
        public int Tick { get; set; } = 0;

        /// <summary>Right of the decimal point.</summary>
        public int Tock { get; set; } = 0;

        /// <summary>Total Tocks for the unit of time.</summary>
        public int TotalTocks { get { return Tick * Globals.TOCKS_PER_TICK + Tock; } }

        #region Constructors
        /// <summary>
        /// Default constructor does nothing.
        /// </summary>
        public Time()
        {
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
                Tick = tick + tock / Globals.TOCKS_PER_TICK;
                Tock = tock % Globals.TOCKS_PER_TICK;
            }
            else
            {
                int atock = Math.Abs(tock);
                Tick = tick - (atock / Globals.TOCKS_PER_TICK) - 1;
                Tock = Globals.TOCKS_PER_TICK - (atock % Globals.TOCKS_PER_TICK);
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

            Tick = tocks / Globals.TOCKS_PER_TICK;
            Tock = tocks % Globals.TOCKS_PER_TICK;
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

            Tick = (int)(tocks / Globals.TOCKS_PER_TICK);
            Tock = (int)(tocks % Globals.TOCKS_PER_TICK);
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

            // Split into two parts from 0.01 or 1.01 or 1.10.
            Tick = Utils.SplitDouble(tts).integral;
            Tock = Utils.SplitDouble(tts).fractional * 1000 / Globals.TOCKS_PER_TICK;
        }
        #endregion

        #region Good practice for custom classess
        public override bool Equals(object obj)
        {
            return Equals(obj as Time);
        }

        public bool Equals(Time obj)
        {
            return obj != null && obj.Tick == Tick && obj.Tock == Tock;
        }

        public override int GetHashCode()
        {
            return TotalTocks;
        }

        public static bool operator ==(Time t1, Time t2)
        {
            return (object)t1 != null && (object)t2 != null && (t1.Tick == t2.Tick) && (t1.Tock == t2.Tock);
        }
        public static bool operator !=(Time t1, Time t2)
        {
            return (object)t1 == null || (object)t2 == null || (t1.Tick != t2.Tick) || (t1.Tock != t2.Tock);
        }
        #endregion

        #region Public functions
        /// <summary>
        /// Do some math on two Time objects.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public static Time operator +(Time t1, Time t2)
        {
            int tick = t1.Tick + t2.Tick + (t1.Tock + t2.Tock) / Globals.TOCKS_PER_TICK;
            int tock = (t1.Tock + t2.Tock) % Globals.TOCKS_PER_TICK;
            return new Time(tick, tock);
        }

        /// <summary>
        /// Move to the next tock and update clock.
        /// </summary>
        /// <returns>True if it's a new Tick.</returns>
        public bool Advance()
        {
            bool newTick = false;
            Tock++;

            if(Tock >= Globals.TOCKS_PER_TICK)
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
        /// Convert to real time total msec.
        /// </summary>
        /// <param name="speed">Speed in msec per tick.</param>
        /// <returns></returns>
        public int ToMsec(int speed)
        {
            double msecpertock = speed / Globals.TOCKS_PER_TICK;
            return (int)(msecpertock * TotalTocks);
        }

        /// <summary>
        /// For viewing pleasure.
        /// </summary>
        public override string ToString()
        {
            return $"{Tick:00}.{Tock:00}";
        }
        #endregion
    }
}
