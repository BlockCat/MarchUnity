using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace March.Core.Sprite
{	
	public struct SpriteSheetAnimationData : IComponentData
	{
		public int state;
		public int frameOffset;
		public int totalFrames;
		public int currentFrame;
		public float frameTimer;
		public float frameTimerMax;
		public float offsetX;
		public float scaleX => 1f / totalFrames;
	}
	
	[Serializable]
	public struct FrameData : IBufferElementData
	{
		public int offset;
		public int count;
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class SpriteSheetAnimationAnimateSystem : SystemBase
	{
		protected override void OnUpdate()
		{
			float deltaTime = Time.DeltaTime;

			Entities
				.ForEach((ref SpriteSheetAnimationData data, in DynamicBuffer<FrameData> buffer) =>
			{
				data.frameTimer += deltaTime;

				while (data.frameTimer >= data.frameTimerMax)
				{
					FrameData frameData = buffer[data.state];
					data.frameTimer -= data.frameTimerMax;
					data.currentFrame = (data.currentFrame + 1) % frameData.count;
					
					float uvOffsetX = (data.currentFrame + frameData.offset) / (float)data.totalFrames;					

					data.offsetX = uvOffsetX;
				}
			}).ScheduleParallel();
		}
	}
}
