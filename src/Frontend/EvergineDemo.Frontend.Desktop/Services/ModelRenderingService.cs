using Evergine.Mathematics;
using EvergineDemo.Shared.Models;
using EvergineDemo.Shared.Models.Stl;
using EvergineDemo.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EvergineDemo.Frontend.Desktop.Services;

/// <summary>
/// Service responsible for managing STL model data and transformations for rendering
/// </summary>
public class ModelRenderingService
{
    private readonly Dictionary<string, ModelRenderData> _modelData = new();
    private readonly object _lock = new();

    /// <summary>
    /// Represents the render data for a model including mesh and transformation
    /// </summary>
    public class ModelRenderData
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Vector3[]? Vertices { get; set; }
        public Vector3[]? Normals { get; set; }
        public uint[]? Indices { get; set; }
        public bool HasMeshData => Vertices != null && Normals != null && Indices != null;
    }

    /// <summary>
    /// Add or update a model's render data
    /// </summary>
    public void AddOrUpdateModel(ModelState model, StlMesh? stlMesh = null)
    {
        lock (_lock)
        {
            // Check if model already exists
            if (_modelData.TryGetValue(model.Id, out var existingData))
            {
                // Update transformations
                existingData.Position = model.Position;
                existingData.Rotation = model.Rotation;
                existingData.Scale = model.Scale;
                
                // Update mesh data if provided and not already set
                if (stlMesh != null && !existingData.HasMeshData)
                {
                    existingData.Vertices = StlToEvergineConverter.GetVertices(stlMesh);
                    existingData.Normals = StlToEvergineConverter.GetNormals(stlMesh);
                    existingData.Indices = StlToEvergineConverter.GetIndices(stlMesh);
                    Console.WriteLine($"[ModelRenderingService] Updated mesh data for model {model.Id} ({model.FileName}): {stlMesh.Triangles.Count} triangles, {existingData.Vertices?.Length ?? 0} vertices");
                }
                else
                {
                    Console.WriteLine($"[ModelRenderingService] Updated transforms for model {model.Id} ({model.FileName}): Pos={model.Position}, Rot={model.Rotation}");
                }
            }
            else
            {
                // Create new model data
                var renderData = new ModelRenderData
                {
                    Id = model.Id,
                    FileName = model.FileName,
                    Position = model.Position,
                    Rotation = model.Rotation,
                    Scale = model.Scale
                };

                // Convert STL mesh if provided
                if (stlMesh != null)
                {
                    renderData.Vertices = StlToEvergineConverter.GetVertices(stlMesh);
                    renderData.Normals = StlToEvergineConverter.GetNormals(stlMesh);
                    renderData.Indices = StlToEvergineConverter.GetIndices(stlMesh);
                    Console.WriteLine($"[ModelRenderingService] Added model {model.Id} ({model.FileName}) with mesh data: {stlMesh.Triangles.Count} triangles, {renderData.Vertices?.Length ?? 0} vertices");
                }
                else
                {
                    Console.WriteLine($"[ModelRenderingService] Added model {model.Id} ({model.FileName}) without mesh data (placeholder)");
                }

                _modelData[model.Id] = renderData;
            }
        }
    }

    /// <summary>
    /// Remove a model's render data
    /// </summary>
    public void RemoveModel(string modelId)
    {
        lock (_lock)
        {
            if (_modelData.Remove(modelId))
            {
                Console.WriteLine($"[ModelRenderingService] Removed model: {modelId}");
            }
        }
    }

    /// <summary>
    /// Update all models from room state (transforms only)
    /// </summary>
    public void UpdateModels(RoomState roomState)
    {
        lock (_lock)
        {
            // Remove models that no longer exist
            var modelsToRemove = _modelData.Keys.Except(roomState.Models.Select(m => m.Id)).ToList();
            foreach (var modelId in modelsToRemove)
            {
                RemoveModel(modelId);
            }

            // Update transforms for existing models
            foreach (var model in roomState.Models)
            {
                if (_modelData.TryGetValue(model.Id, out var renderData))
                {
                    renderData.Position = model.Position;
                    renderData.Rotation = model.Rotation;
                    renderData.Scale = model.Scale;
                }
            }
        }
    }

    /// <summary>
    /// Get all model render data for rendering
    /// </summary>
    public IReadOnlyList<ModelRenderData> GetModelRenderData()
    {
        lock (_lock)
        {
            return _modelData.Values.ToList();
        }
    }

    /// <summary>
    /// Get render data for a specific model
    /// </summary>
    public ModelRenderData? GetModelRenderData(string modelId)
    {
        lock (_lock)
        {
            return _modelData.TryGetValue(modelId, out var data) ? data : null;
        }
    }
}
