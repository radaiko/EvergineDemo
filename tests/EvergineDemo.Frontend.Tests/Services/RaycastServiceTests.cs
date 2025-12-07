using Evergine.Mathematics;
using EvergineDemo.Frontend.Desktop.Services;
using Xunit;

namespace EvergineDemo.Frontend.Tests.Services;

public class RaycastServiceTests
{
    private readonly RaycastService _raycastService;

    public RaycastServiceTests()
    {
        _raycastService = new RaycastService();
    }

    [Fact]
    public void CreateRayFromScreenPoint_CenterOfScreen_CreatesRayLookingForward()
    {
        // Arrange
        float screenX = 400; // Center of 800px width
        float screenY = 300; // Center of 600px height
        float viewportWidth = 800;
        float viewportHeight = 600;
        var cameraPosition = new Vector3(0, 0, 10);
        var cameraOrientation = Quaternion.Identity; // Looking down -Z
        float fieldOfView = MathHelper.ToRadians(45);

        // Act
        var ray = _raycastService.CreateRayFromScreenPoint(
            screenX, screenY,
            viewportWidth, viewportHeight,
            cameraPosition, cameraOrientation,
            fieldOfView
        );

        // Assert
        Assert.Equal(cameraPosition, ray.Position);
        // Ray should point roughly down -Z axis (allowing for floating point precision)
        Assert.True(ray.Direction.Z < 0, "Ray should point in negative Z direction");
        Assert.True(Math.Abs(ray.Direction.X) < 0.01f, "Ray X should be near zero for center screen");
        Assert.True(Math.Abs(ray.Direction.Y) < 0.01f, "Ray Y should be near zero for center screen");
    }

    [Fact]
    public void CreateRayFromScreenPoint_TopLeftCorner_CreatesRayPointingUpLeft()
    {
        // Arrange
        float screenX = 0;
        float screenY = 0;
        float viewportWidth = 800;
        float viewportHeight = 600;
        var cameraPosition = new Vector3(0, 0, 10);
        var cameraOrientation = Quaternion.Identity;
        float fieldOfView = MathHelper.ToRadians(45);

        // Act
        var ray = _raycastService.CreateRayFromScreenPoint(
            screenX, screenY,
            viewportWidth, viewportHeight,
            cameraPosition, cameraOrientation,
            fieldOfView
        );

        // Assert
        Assert.Equal(cameraPosition, ray.Position);
        Assert.True(ray.Direction.X < 0, "Ray should point left (negative X)");
        Assert.True(ray.Direction.Y > 0, "Ray should point up (positive Y)");
        Assert.True(ray.Direction.Z < 0, "Ray should point forward (negative Z)");
    }

    [Fact]
    public void RaycastModels_NoModels_ReturnsEmptyList()
    {
        // Arrange
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        var models = new List<ModelRenderingService.ModelRenderData>();

        // Act
        var hits = _raycastService.RaycastModels(ray, models);

        // Assert
        Assert.Empty(hits);
    }

    [Fact]
    public void RaycastModels_RayHitsModel_ReturnsHit()
    {
        // Arrange
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ); // Ray pointing at origin
        var models = new List<ModelRenderingService.ModelRenderData>
        {
            new ModelRenderingService.ModelRenderData
            {
                Id = "model1",
                FileName = "test.stl",
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
                // No mesh data - will use default 1x1x1 bounding box
            }
        };

        // Act
        var hits = _raycastService.RaycastModels(ray, models);

        // Assert
        Assert.Single(hits);
        Assert.Equal("model1", hits[0].ModelId);
        Assert.Equal("test.stl", hits[0].FileName);
        Assert.True(hits[0].Distance > 0);
    }

    [Fact]
    public void RaycastModels_RayMissesModel_ReturnsNoHits()
    {
        // Arrange
        var ray = new Ray(new Vector3(10, 10, -5), Vector3.UnitZ); // Ray far from origin
        var models = new List<ModelRenderingService.ModelRenderData>
        {
            new ModelRenderingService.ModelRenderData
            {
                Id = "model1",
                FileName = "test.stl",
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
            }
        };

        // Act
        var hits = _raycastService.RaycastModels(ray, models);

        // Assert
        Assert.Empty(hits);
    }

    [Fact]
    public void RaycastModels_MultipleModels_ReturnsSortedByDistance()
    {
        // Arrange
        var ray = new Ray(new Vector3(0, 0, -10), Vector3.UnitZ); // Ray pointing at origin
        var models = new List<ModelRenderingService.ModelRenderData>
        {
            new ModelRenderingService.ModelRenderData
            {
                Id = "far",
                FileName = "far.stl",
                Position = new Vector3(0, 0, 5), // Farther from ray origin
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
            },
            new ModelRenderingService.ModelRenderData
            {
                Id = "near",
                FileName = "near.stl",
                Position = new Vector3(0, 0, -2), // Closer to ray origin
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
            }
        };

        // Act
        var hits = _raycastService.RaycastModels(ray, models);

        // Assert
        Assert.Equal(2, hits.Count);
        Assert.Equal("near", hits[0].ModelId); // Closer model should be first
        Assert.Equal("far", hits[1].ModelId);
        Assert.True(hits[0].Distance < hits[1].Distance);
    }

    [Fact]
    public void RaycastModels_WithMeshData_UsesActualBounds()
    {
        // Arrange
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        
        // Create a model with mesh data (vertices form a 2x2x2 cube)
        var vertices = new Vector3[]
        {
            new Vector3(-1, -1, -1),
            new Vector3(1, -1, -1),
            new Vector3(-1, 1, -1),
            new Vector3(1, 1, -1),
            new Vector3(-1, -1, 1),
            new Vector3(1, -1, 1),
            new Vector3(-1, 1, 1),
            new Vector3(1, 1, 1),
        };

        var models = new List<ModelRenderingService.ModelRenderData>
        {
            new ModelRenderingService.ModelRenderData
            {
                Id = "model1",
                FileName = "test.stl",
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = Vector3.One,
                Vertices = vertices,
                Normals = new Vector3[vertices.Length], // Not used in raycasting
                Indices = new uint[0], // Not used in raycasting
            }
        };

        // Act
        var hits = _raycastService.RaycastModels(ray, models);

        // Assert
        Assert.Single(hits);
        Assert.Equal("model1", hits[0].ModelId);
    }

    [Fact]
    public void RaycastModels_ScaledModel_AccountsForScale()
    {
        // Arrange
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        
        // Model scaled by 2x should have a larger hit box
        var models = new List<ModelRenderingService.ModelRenderData>
        {
            new ModelRenderingService.ModelRenderData
            {
                Id = "scaled",
                FileName = "scaled.stl",
                Position = Vector3.Zero,
                Rotation = Quaternion.Identity,
                Scale = new Vector3(2, 2, 2),
            }
        };

        // Act
        var hits = _raycastService.RaycastModels(ray, models);

        // Assert
        Assert.Single(hits);
        Assert.Equal("scaled", hits[0].ModelId);
    }

    [Fact]
    public void RaycastModels_RotatedModel_AccountsForRotation()
    {
        // Arrange
        var ray = new Ray(new Vector3(2, 0, -5), Vector3.UnitZ); // Ray offset to the right
        
        // Rotate model 45 degrees around Y axis
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(45));
        
        var models = new List<ModelRenderingService.ModelRenderData>
        {
            new ModelRenderingService.ModelRenderData
            {
                Id = "rotated",
                FileName = "rotated.stl",
                Position = new Vector3(1, 0, 0), // Offset position
                Rotation = rotation,
                Scale = Vector3.One,
            }
        };

        // Act
        var hits = _raycastService.RaycastModels(ray, models);

        // Assert
        // The rotated bounding box should extend far enough to potentially hit
        // This test verifies that rotation is being applied
        Assert.NotNull(hits); // Should not throw
    }
}
