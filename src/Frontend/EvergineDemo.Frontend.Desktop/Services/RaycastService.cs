using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EvergineDemo.Frontend.Desktop.Services;

/// <summary>
/// Service for performing raycasting operations for model picking
/// </summary>
public class RaycastService
{
    /// <summary>
    /// Cast a ray from screen coordinates into the 3D scene
    /// </summary>
    /// <param name="screenX">Screen X coordinate (0 at left edge)</param>
    /// <param name="screenY">Screen Y coordinate (0 at top edge)</param>
    /// <param name="viewportWidth">Width of the viewport in pixels</param>
    /// <param name="viewportHeight">Height of the viewport in pixels</param>
    /// <param name="cameraPosition">Camera position in world space</param>
    /// <param name="cameraOrientation">Camera orientation quaternion</param>
    /// <param name="fieldOfView">Camera field of view in radians</param>
    /// <returns>Ray in world space</returns>
    public Ray CreateRayFromScreenPoint(
        float screenX, float screenY,
        float viewportWidth, float viewportHeight,
        Vector3 cameraPosition, Quaternion cameraOrientation,
        float fieldOfView)
    {
        // Convert screen coordinates to normalized device coordinates (-1 to 1)
        float ndcX = (2.0f * screenX) / viewportWidth - 1.0f;
        float ndcY = 1.0f - (2.0f * screenY) / viewportHeight; // Y is inverted

        // Calculate aspect ratio
        float aspectRatio = viewportWidth / viewportHeight;

        // Calculate ray direction in view space
        float tanHalfFov = MathF.Tan(fieldOfView / 2.0f);
        float viewX = ndcX * aspectRatio * tanHalfFov;
        float viewY = ndcY * tanHalfFov;
        
        // Ray direction in view space (camera looks down -Z)
        Vector3 rayDirectionView = Vector3.Normalize(new Vector3(viewX, viewY, -1.0f));

        // Transform ray direction to world space using camera orientation
        Vector3 rayDirectionWorld = Vector3.Transform(rayDirectionView, cameraOrientation);

        return new Ray(cameraPosition, rayDirectionWorld);
    }

    /// <summary>
    /// Find models intersected by a ray, sorted by distance
    /// </summary>
    /// <param name="ray">Ray in world space</param>
    /// <param name="models">Collection of models to test</param>
    /// <returns>List of intersected models sorted by distance (closest first)</returns>
    public List<RaycastHit> RaycastModels(Ray ray, IEnumerable<ModelRenderingService.ModelRenderData> models)
    {
        var hits = new List<RaycastHit>();

        foreach (var model in models)
        {
            if (RayIntersectsModel(ray, model, out float distance))
            {
                hits.Add(new RaycastHit
                {
                    ModelId = model.Id,
                    FileName = model.FileName,
                    Distance = distance,
                    Position = model.Position
                });
            }
        }

        // Sort by distance (closest first)
        return hits.OrderBy(h => h.Distance).ToList();
    }

    /// <summary>
    /// Test if a ray intersects a model's bounding box
    /// </summary>
    /// <param name="ray">Ray in world space</param>
    /// <param name="model">Model to test</param>
    /// <param name="distance">Distance to intersection point</param>
    /// <returns>True if the ray intersects the model</returns>
    private bool RayIntersectsModel(Ray ray, ModelRenderingService.ModelRenderData model, out float distance)
    {
        distance = float.MaxValue;

        // If model has mesh data, calculate accurate bounding box
        if (model.HasMeshData)
        {
            var bounds = CalculateBoundingBox(model.Vertices!);
            
            // Transform bounding box by model's transform
            var transformedBounds = TransformBoundingBox(bounds, model.Position, model.Rotation, model.Scale);
            
            return RayIntersectsBoundingBox(ray, transformedBounds, out distance);
        }
        else
        {
            // Fallback: use a default bounding box (1x1x1 cube centered at model position)
            var defaultBounds = new BoundingBox(
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, 0.5f)
            );
            
            var transformedBounds = TransformBoundingBox(defaultBounds, model.Position, model.Rotation, model.Scale);
            
            return RayIntersectsBoundingBox(ray, transformedBounds, out distance);
        }
    }

    /// <summary>
    /// Calculate axis-aligned bounding box from vertices
    /// </summary>
    private BoundingBox CalculateBoundingBox(Vector3[] vertices)
    {
        if (vertices.Length == 0)
        {
            return new BoundingBox(Vector3.Zero, Vector3.Zero);
        }

        var min = vertices[0];
        var max = vertices[0];

        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Transform bounding box by position, rotation, and scale
    /// </summary>
    private BoundingBox TransformBoundingBox(BoundingBox bounds, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        // Get the 8 corners of the bounding box
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Min.Z);
        corners[1] = new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Min.Z);
        corners[2] = new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Min.Z);
        corners[3] = new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Min.Z);
        corners[4] = new Vector3(bounds.Min.X, bounds.Min.Y, bounds.Max.Z);
        corners[5] = new Vector3(bounds.Max.X, bounds.Min.Y, bounds.Max.Z);
        corners[6] = new Vector3(bounds.Min.X, bounds.Max.Y, bounds.Max.Z);
        corners[7] = new Vector3(bounds.Max.X, bounds.Max.Y, bounds.Max.Z);

        // Transform all corners
        for (int i = 0; i < 8; i++)
        {
            // Scale
            corners[i] *= scale;
            
            // Rotate
            corners[i] = Vector3.Transform(corners[i], rotation);
            
            // Translate
            corners[i] += position;
        }

        // Calculate new AABB from transformed corners
        var min = corners[0];
        var max = corners[0];
        
        for (int i = 1; i < 8; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Test ray intersection with axis-aligned bounding box
    /// Uses the slab method for ray-AABB intersection
    /// </summary>
    private bool RayIntersectsBoundingBox(Ray ray, BoundingBox box, out float distance)
    {
        distance = 0f;
        const float epsilon = 1e-8f;

        // Calculate inverse direction for efficiency, handling near-zero components
        float invDirX = MathF.Abs(ray.Direction.X) > epsilon ? 1.0f / ray.Direction.X : float.MaxValue;
        float invDirY = MathF.Abs(ray.Direction.Y) > epsilon ? 1.0f / ray.Direction.Y : float.MaxValue;
        float invDirZ = MathF.Abs(ray.Direction.Z) > epsilon ? 1.0f / ray.Direction.Z : float.MaxValue;

        // Calculate intersection distances for each axis
        float t1 = (box.Min.X - ray.Position.X) * invDirX;
        float t2 = (box.Max.X - ray.Position.X) * invDirX;
        float t3 = (box.Min.Y - ray.Position.Y) * invDirY;
        float t4 = (box.Max.Y - ray.Position.Y) * invDirY;
        float t5 = (box.Min.Z - ray.Position.Z) * invDirZ;
        float t6 = (box.Max.Z - ray.Position.Z) * invDirZ;

        // Find min and max for each axis
        float tmin = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        float tmax = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

        // Ray intersects if tmax >= tmin and tmax >= 0
        if (tmax < 0 || tmin > tmax)
        {
            return false;
        }

        // Return the near intersection distance
        distance = tmin >= 0 ? tmin : tmax;
        return true;
    }

    /// <summary>
    /// Result of a raycast hit
    /// </summary>
    public class RaycastHit
    {
        public string ModelId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public float Distance { get; set; }
        public Vector3 Position { get; set; }
    }
}
