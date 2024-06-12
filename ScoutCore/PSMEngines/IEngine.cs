using Digestor;
using ScoutCore.QueryLogic;
using ScoutCore.Results;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ScoutCore.PSMEngines
{
    public interface IEngine
    {
        BatchDigestor PrepareDatabase(string fastaFile);
        ScoutRawResults PerformSearch(BatchDigestor dbGenerator, string file, CancellationToken token);

    }
}
