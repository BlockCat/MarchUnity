using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine.Profiling;

namespace March.Core
{
	public class ServerSpecificSystemGroups: ComponentSystemGroup
	{
		protected override void OnUpdate()
		{
			Profiler.BeginSample("server_specific");
			base.OnUpdate();
			Profiler.EndSample();
		}
	}
}
