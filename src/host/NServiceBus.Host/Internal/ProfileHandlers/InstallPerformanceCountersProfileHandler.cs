﻿using System.Diagnostics;

namespace NServiceBus.Host.Internal.ProfileHandlers
{
    /// <summary>
    /// Installs performance counters.
    /// </summary>
    public class InstallPerformanceCountersProfileHandler : IHandleProfile<InstallPerformanceCounters>
    {
        void IHandleProfile.ProfileActivated()
        {
            var categoryName = "NServiceBus";
            var counterName = "Critical Time";

            if (PerformanceCounterCategory.Exists(categoryName))
                PerformanceCounterCategory.Delete(categoryName);

            var data = new CounterCreationDataCollection();

            var c1 = new CounterCreationData(counterName, "Age of the oldest message in the queue",
                                             PerformanceCounterType.NumberOfItems32);
            data.Add(c1);

            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics",
                                              PerformanceCounterCategoryType.MultiInstance, data);
        }
    }
}
