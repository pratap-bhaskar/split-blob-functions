using System.Collections.Generic;

namespace VSMCFunctions.Models
{
    internal record VesselSchedules
    {
        public IEnumerable<VesselSchedule> Schedules { get; set; }
    }
}
