using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace March.Core.Sprite
{
	[DisallowMultipleComponent]
	[RequiresEntityConversion]
	public class SpriteAnimation : MonoBehaviour, IConvertGameObjectToEntity
	{
		public Material material;
		public FrameData[] data;

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			var buffer = dstManager.AddBuffer<FrameData>(entity);
			foreach (var x in data)
			{
				buffer.Add(x);
			}
			dstManager.AddComponentData(entity, new SpriteInformation
			{
				direction = SpriteInformation.Direction.LEFT,
				animation = 0
			});

			dstManager.AddSharedComponentData(entity, new RenderMesh
			{
				material = this.material
			});
		}
	}


}
