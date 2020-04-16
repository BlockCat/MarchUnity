using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace March.Core.Sprite
{
	[DisallowMultipleComponent]
	[RequiresEntityConversion]
	public class SpriteAnimation : MonoBehaviour, IConvertGameObjectToEntity
	{
		[MinValue(0)]
		public int State;
		[MinValue(0)]
		public int FrameCount;

		public float frameDuration = 0.2f;

		public FrameData[] data;
		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			var buffer = dstManager.AddBuffer<FrameData>(entity);
			foreach (var x in data)
			{
				buffer.Add(x);
			}

			dstManager.AddComponentData(entity, new SpriteSheetAnimationData
			{
				state = State,
				frameOffset = 0,
				totalFrames = FrameCount,
				currentFrame = 0,
				frameTimer = 0f,
				frameTimerMax = frameDuration,
				offsetX = 0,
			});
		}
	}


}
