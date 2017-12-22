using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Aika.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub for pushing real-time value changes to subscribers.
    /// </summary>
    public class DataStreamHub : Hub {
    }
}
