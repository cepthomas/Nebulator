using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebulator.Common;


namespace Nebulator.Midi
{
    /// <summary>A collection of Steps.</summary>
    public class StepCollection
    {
        ///<summary>The main collection of Steps. The key is the time to send the list.</summary>
        Dictionary<Time, List<Step>> _steps = new Dictionary<Time, List<Step>>();

        #region Properties
        ///<summary>Gets a collection of the list.</summary>
        public IEnumerable<Time> Times
        {
            get { return (_steps.Keys); }
        }

        ///<summary>Gets the count property of the list.</summary>
        public int Count
        {
            get { return _steps.Count; }
        }

        ///<summary>The duration of the whole thing.</summary>
        public int MaxTick { get; private set; } = 0;
        #endregion

        #region Methods
        /// <summary>
        /// Add a step at the given time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="step"></param>
        public void AddStep(Time time, Step step)
        {
            if (!_steps.ContainsKey(time))
            {
                _steps.Add(time, new List<Step>());
            }
            _steps[time].Add(step);

            MaxTick = Math.Max(MaxTick, time.Tick);
        }

        /// <summary>
        /// Get the steps for the given time.
        /// </summary>
        public IEnumerable<Step> GetSteps(Time time)
        {
            return _steps.ContainsKey(time) ? _steps[time] : new List<Step>();
        }

        /// <summary>
        /// Delete the steps for the given time.
        /// </summary>
        public void DeleteSteps(Time time)
        {
            _steps.Remove(time);
        }

        /// <summary>
        /// Cleanse me.
        /// </summary>
        public void Clear()
        {
            _steps.Clear();
        }

        /// <summary>
        /// Display the content steps.
        /// </summary>
        public override string ToString()
        {
            int total = 0;
            foreach (IList<Step> steps in _steps.Values)
            {
                total += steps.Count;
            }
            return $"Count:{Count} Total:{total}";

            //List<string> ls = new List<string>();
            //Times.ToList().ForEach(t => ls.Add(new Time(t).ToString()));
            //return string.Join(", ", ls);

            //List<int> ticktocks = Times.ToList();
            //ticktocks.Sort();
            //foreach (int i in ticktocks)
            //{
            //    Time t = new Time() { Packed = i };
            //    ls.AppendFormat("Time:{0}{1}", t, Environment.NewLine);
            //    _steps[i].ForEach(x => ls.AppendFormat("  {0}{1}", x, Environment.NewLine));
            //}
        }
        #endregion
    }
}
