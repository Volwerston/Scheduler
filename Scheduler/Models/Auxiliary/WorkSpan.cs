using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorithms
{
    public class WorkSpan
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<WorkSpan> ReservedSpans { get; set; }

        public string TaskName { get; set; }

        public WorkSpan()
        {
            ReservedSpans = new List<WorkSpan>();
        }

        public bool TryInsertTarget(Target target)
        {
            int busyTime = Convert.ToInt32(ReservedSpans.Select(x => x.EndTime.TotalMinutes - x.StartTime.TotalMinutes).Sum());

            if (EndTime.TotalMinutes - StartTime.TotalMinutes - busyTime >= target.Duration)
            {
                if (ReservedSpans.Count() != 0)
                {
                    if (ReservedSpans[ReservedSpans.Count() - 1].StartTime.TotalMinutes -
                       StartTime.TotalMinutes >= target.Duration)
                    {
                        ReservedSpans.Add(new WorkSpan() { StartTime = StartTime, EndTime = StartTime, TaskName = target.Name });
                        ReservedSpans[ReservedSpans.Count() - 1].EndTime =
                            ReservedSpans[ReservedSpans.Count() - 1].EndTime.Add(new TimeSpan(0, target.Duration, 0));
                        return true;
                    }

                    if (EndTime.TotalMinutes - ReservedSpans[ReservedSpans.Count() - 1]
                        .EndTime.TotalMinutes >= target.Duration)
                    {
                        ReservedSpans.Add(new WorkSpan() { StartTime = 
                            ReservedSpans[ReservedSpans.Count() - 1].EndTime,
                            EndTime = ReservedSpans[ReservedSpans.Count() - 1].EndTime,
                            TaskName = target.Name
                        });
                        ReservedSpans[ReservedSpans.Count() - 1].EndTime =
                            ReservedSpans[ReservedSpans.Count() - 1].EndTime.Add(new TimeSpan(0, target.Duration, 0));
                        return true;
                    }
                }


                for (int i = 0; i < ReservedSpans.Count() - 1; ++i)
                {
                    if (ReservedSpans[i + 1].StartTime.TotalMinutes - ReservedSpans[i].EndTime.TotalMinutes >= target.Duration)
                    {
                        ReservedSpans.Insert(i + 1, new WorkSpan() { StartTime = ReservedSpans[i].EndTime,
                            EndTime = ReservedSpans[i].EndTime,
                            TaskName = target.Name
                        });
                        ReservedSpans[i + 1].EndTime =
                            ReservedSpans[i + 1].EndTime.Add(new TimeSpan(0, 0, target.Duration, 0));
                        return true;
                    }
                }

                if (ReservedSpans.Count() != 0)
                {
                    ShiftSpans();
                    ReservedSpans.Add(new WorkSpan() { StartTime = ReservedSpans[ReservedSpans.Count() - 1].EndTime,
                        EndTime = ReservedSpans[ReservedSpans.Count() - 1].EndTime,
                        TaskName = target.Name
                    });
                    ReservedSpans[ReservedSpans.Count() - 1].EndTime =
                        ReservedSpans[ReservedSpans.Count() - 1].EndTime.Add(new TimeSpan(0, target.Duration, 0));
                    return true;
                }

                TimeSpan insertionTime = target.BestWorkSpan.StartTime.Hours > StartTime.Hours ?
                        target.BestWorkSpan.StartTime : StartTime;

                ReservedSpans.Add(new WorkSpan() { StartTime = insertionTime,
                    EndTime = insertionTime,
                    TaskName = target.Name
                });
                ReservedSpans[0].EndTime =
                    ReservedSpans[0].EndTime.Add(new TimeSpan(0, target.Duration, 0));
                return true;
            }
            return false;
        }

        private void ShiftSpans()
        {
            int firstDuration = Convert.ToInt32(ReservedSpans[0].EndTime.TotalMinutes - ReservedSpans[0].StartTime.TotalMinutes);

            ReservedSpans[0].StartTime = StartTime;
            ReservedSpans[0].EndTime = StartTime;
            ReservedSpans[0].EndTime = ReservedSpans[0].EndTime.Add(new TimeSpan(0, firstDuration, 0));

            for (int i = 1; i < ReservedSpans.Count(); ++i)
            {
                int currDuration = Convert.ToInt32(ReservedSpans[i].EndTime.TotalMinutes - ReservedSpans[i].StartTime.TotalMinutes);
                ReservedSpans[i].StartTime = ReservedSpans[i - 1].EndTime;
                ReservedSpans[i].EndTime = ReservedSpans[i - 1].EndTime;
                ReservedSpans[i].EndTime =
                    ReservedSpans[i].EndTime.Add(new TimeSpan(0, currDuration, 0));
            }
        }
    }
}
