using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReduxDotnet;

public delegate ValueTask EffectDelegate<TStore>(IDispatcher<TStore> dispatcher, Func<TStore> getStore);
