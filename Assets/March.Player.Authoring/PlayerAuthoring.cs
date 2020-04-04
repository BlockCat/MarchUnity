using Assets.March.Player;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct PlayerTag : IComponentData { };

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{


	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, new PlayerTag());
	}
}


