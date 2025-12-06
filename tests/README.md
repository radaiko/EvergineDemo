# EvergineDemo Tests

This directory contains unit tests for the EvergineDemo project.

## Test Projects

### EvergineDemo.Shared.Tests

Unit tests for the shared library components, including:

- **STL Parser Tests**: Comprehensive tests for binary and ASCII STL file parsing
- **Evergine Converter Tests**: Tests for STL to Evergine format conversion utilities

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run tests for a specific project
```bash
dotnet test tests/EvergineDemo.Shared.Tests/EvergineDemo.Shared.Tests.csproj
```

### Run tests with detailed output
```bash
dotnet test --verbosity normal
```

### Run tests with code coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Coverage

Current test statistics:
- **Total Tests**: 22
- **Passed**: 22
- **Failed**: 0
- **Skipped**: 0

### Coverage by Component

#### STL Parser Service (15 tests)
- Binary STL parsing (single and multiple triangles)
- ASCII STL parsing (various formats including scientific notation)
- Error handling (null data, corrupted files, malformed data)
- Mesh data extraction (vertices, normals)

#### STL to Evergine Converter (7 tests)
- Vertex conversion
- Normal conversion
- Index generation
- Bounding box calculation
- Center point calculation
- Size calculation

## Test Framework

- **Framework**: xUnit 2.9.2
- **Target**: .NET 10.0
- **Test Runner**: VSTest / dotnet test

## Adding New Tests

1. Create a new test class in the appropriate subdirectory
2. Add test methods with `[Fact]` attribute for simple tests or `[Theory]` for parameterized tests
3. Follow the Arrange-Act-Assert pattern
4. Run tests to verify they pass

Example:
```csharp
[Fact]
public void MyTest_ValidInput_ReturnsExpectedResult()
{
    // Arrange
    var service = new MyService();
    
    // Act
    var result = service.DoSomething();
    
    // Assert
    Assert.Equal(expectedValue, result);
}
```
