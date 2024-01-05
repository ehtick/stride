﻿using BepuPhysics;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.DebugRender.Components;
using Stride.BepuPhysics.DebugRender.Effects;
using Stride.BepuPhysics.DebugRender.Effects.RenderFeatures;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;

namespace Stride.BepuPhysics.DebugRender.Processors
{
    public class DebugRenderProcessor : EntityProcessor<DebugRenderComponent>
    {
        private IGame? _game = null;
        private SceneSystem _sceneSystem;
        private BepuShapeCacheSystem _bepuShapeCacheSystem;
        private InputManager _input;
        private SinglePassWireframeRenderFeature? _wireframeRenderFeature;
        private VisibilityGroup _visibilityGroup;
        private List<WireFrameRenderObject> _wireFrameRenderObject = new();

        public DebugRenderProcessor()
        {
            Order = 10200;
        }
        protected override void OnSystemAdd()
        {
            _game = Services.GetService<IGame>();
            _sceneSystem = Services.GetService<SceneSystem>(); //_game.GameSystems.OfType<SceneSystem>().First()
            _bepuShapeCacheSystem = Services.GetService<BepuShapeCacheSystem>() ?? new BepuShapeCacheSystem(Services);
#warning THIS SHOULD CHANGE, cacheSystem should be added automatically
            _input = Services.GetService<InputManager>();

            if (!_sceneSystem.GraphicsCompositor.RenderFeatures.Any(e => e is SinglePassWireframeRenderFeature))
            {
                _sceneSystem.GraphicsCompositor.RenderFeatures.Add(new SinglePassWireframeRenderFeature());
            }
            _wireframeRenderFeature = _sceneSystem.GraphicsCompositor.RenderFeatures.OfType<SinglePassWireframeRenderFeature>().FirstOrDefault();//We should add the RenderFeature if missing
            _visibilityGroup = _sceneSystem.SceneInstance.VisibilityGroups.First();
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] DebugRenderComponent component, [NotNull] DebugRenderComponent data)
        {
            UpdateRender();
            base.OnEntityComponentAdding(entity, component, data);
        }

        public override void Update(GameTime time)
        {
            if (_input.IsKeyDown(Keys.F10))
                UpdateRender();

            base.Update(time);
        }

        public void UpdateRender()
        {
            while (_wireFrameRenderObject.Any())
            {
                _visibilityGroup.RenderObjects.Remove(_wireFrameRenderObject[0]);
                _wireFrameRenderObject[0].VertexBuffer.Dispose();
                _wireFrameRenderObject[0].IndiceBuffer.Dispose();
                _wireFrameRenderObject.RemoveAt(0);
            }

            foreach (var entityformScene in _sceneSystem.SceneInstance.First().EntityManager)
            {
                var containerCompo = entityformScene.Get<ContainerComponent>();
                if (containerCompo != null)
                {
                    var color = Color.Black;

                    if (containerCompo is IContainerWithColliders)
                    {
                        color = Color.Red;
                    }
                    else if (containerCompo is IContainerWithColliders)
                    {
                        color = Color.Blue;
                    }

                    var shapeAndOffsets = _bepuShapeCacheSystem.GetShapeAndOffsets(containerCompo);

                    foreach (var shapeAndOffset in shapeAndOffsets)
                    {
                        var local = shapeAndOffset;
                        var one = Vector3.One;

                        Matrix.Transformation(ref local.transform.Scale, ref local.transform.RotationOffset, ref local.transform.LinearOffset, out var containerMatrix);

                        containerMatrix *= Matrix.RotationQuaternion(containerCompo.Entity.Transform.GetWorldRot());
                        containerMatrix *= Matrix.Translation(containerCompo.Entity.Transform.GetWorldPos());



                        var wfro = new WireFrameRenderObject() { Color = color, WorldMatrix = containerMatrix };
                        wfro.Prepare(_game.GraphicsDevice, shapeAndOffset.data.Indices, shapeAndOffset.data.Vertex);
                        _wireFrameRenderObject.Add(wfro);
                        _visibilityGroup.RenderObjects.Add(wfro);
                    }


                }
            }
        }

    }
}
