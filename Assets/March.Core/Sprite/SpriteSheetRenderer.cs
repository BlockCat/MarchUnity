using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace March.Core.Sprite
{
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(SpriteSheetAnimationAnimateSystem))]
	public class SpriteSheetRenderer : SystemBase
	{
		protected override void OnUpdate()
		{
			
			Entities
				.WithoutBurst()
				.ForEach((RenderMesh mesh, in SpriteSheetAnimationData animationData, in Translation translation) =>
			{
				
				mesh.material.SetTextureOffset("_BaseMap", new Vector2(animationData.offsetX, 0));
				mesh.material.SetTextureScale("_BaseMap", new Vector2(-animationData.scaleX, 1f));

			}).Run();
		}
	}
}
