using Unity.Entities;
using UnityEngine;

public struct PlayerTag : IComponentData {
	public float speed;
	public float jumpForce;
};
public struct RequestFreeze : IComponentData { }

[DisallowMultipleComponent]
[RequiresEntityConversion]
public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
	public float Speed;
	public float JumpForce;

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponentData(entity, new PlayerTag
		{
			speed = this.Speed,
			jumpForce = this.JumpForce
		});
		dstManager.AddComponent<RequestFreeze>(entity);
	}
}


