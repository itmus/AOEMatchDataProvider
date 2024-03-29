﻿using AOEMatchDataProvider.Models;
using AOEMatchDataProvider.Models.User;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOEMatchDataProvider.Events.Views.TeamsPanel
{
    public class UserCollectionChangedEvent : PubSubEvent<IEnumerable<UserMatchData>> { }
}
