﻿using Mixed;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[DisallowMultipleComponent]
[RequiresEntityConversion
	]
public class LevelSettings : MonoBehaviour, IConvertGameObjectToEntity
{

	public float Size = 4;
	public int ChunkResolution = 4;



	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, new LevelLoadRequest
		{
			Size = Size,
			ChunkResolution = ChunkResolution
		});
	}

	private void OnDrawGizmos()
	{

		Gizmos.DrawWireCube(new Vector3(Size / 2, Size / 2, 0), new Vector3(Size, Size, 0));
	}
}
