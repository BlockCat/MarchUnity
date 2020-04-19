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
	public class SpriteSheetRenderer : SystemBase
	{
		private Mesh quad;

		protected override void OnCreate()
		{
			quad = new Mesh();
			Vector3[] vertices = new Vector3[4];
			vertices[0] = new Vector3(0, 0, 0);
			vertices[1] = new Vector3(1, 0, 0);
			vertices[2] = new Vector3(0, 1, 0);
			vertices[3] = new Vector3(1, 1, 0);
			quad.vertices = vertices;

			int[] tri = new int[6];
			tri[0] = 0;
			tri[1] = 1;
			tri[2] = 2;
			tri[3] = 2;
			tri[4] = 1;
			tri[5] = 3;
			quad.triangles = tri;

			Vector3[] normals = new Vector3[4];
			normals[0] = Vector3.forward;
			normals[1] = Vector3.forward;
			normals[2] = Vector3.forward;
			normals[3] = Vector3.forward;
			quad.normals = normals;

			Vector2[] uv = new Vector2[4];
			uv[0] = new Vector2(0, 0);
			uv[1] = new Vector2(1, 0);
			uv[2] = new Vector2(0, 1);
			uv[3] = new Vector2(1, 1);
			quad.uv = uv;
		}

		protected override void OnUpdate()
		{
			Entities
				.WithoutBurst()
				.ForEach((Entity entity, DynamicBuffer<FrameData> dataBuffer, RenderMesh mesh, in SpriteInformation info, in Translation translation) =>
			{
				MaterialPropertyBlock properties = new MaterialPropertyBlock();
				var data = dataBuffer[info.animation];
				
				properties.SetInt("Vector1_4A447B85", data.offset);
				properties.SetInt("Vector1_C85BB47D", data.count);

				if (info.direction == SpriteInformation.Direction.LEFT)
				{
					properties.SetFloat("Boolean_A9A184AB", 0);
				}
				else
				{
					properties.SetFloat("Boolean_A9A184AB", 1);
				}

				Graphics.DrawMesh(quad, translation.Value, Quaternion.identity, mesh.material, 0, Camera.main, 0, properties, false, false);

			}).Run();
		}
	}
}
