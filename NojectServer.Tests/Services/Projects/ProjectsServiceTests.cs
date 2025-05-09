using Microsoft.AspNetCore.SignalR;
using Moq;
using NojectServer.Hubs;
using NojectServer.Models;
using NojectServer.Models.Requests.Projects;
using NojectServer.Repositories.Interfaces;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Projects.Implementations;
using NojectServer.Tests.MockHelpers.AsyncQuerySupport;
using NojectServer.Utils.ResultPattern;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Projects;

public class ProjectsServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IHubContext<SharedProjectsHub>> _mockHubContext;
    private readonly ProjectsService _projectsService;
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<ICollaboratorRepository> _mockCollaboratorRepository;

    #region Setup

    public ProjectsServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockHubContext = new Mock<IHubContext<SharedProjectsHub>>();

        // Setup mock repositories
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockCollaboratorRepository = new Mock<ICollaboratorRepository>();

        // Configure UnitOfWork to return the mock repositories
        _mockUnitOfWork.Setup(uow => uow.Projects).Returns(_mockProjectRepository.Object);
        _mockUnitOfWork.Setup(uow => uow.Collaborators).Returns(_mockCollaboratorRepository.Object);

        _projectsService = new ProjectsService(_mockUnitOfWork.Object, _mockHubContext.Object);
    }

    #endregion

    #region CreateProjectAsync

    [Fact]
    public async Task CreateProjectAsync_ShouldCreateProjectAndReturnSuccess()
    {
        // Arrange
        var request = new CreateUpdateProjectRequest { Name = "Test Project" };
        var userId = Guid.NewGuid();
        Project? addedProject = null;

        _mockProjectRepository.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .Callback<Project>(p => addedProject = p)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _projectsService.CreateProjectAsync(request, userId);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<Project>>(result);
        Assert.NotNull(successResult.Value);
        Assert.Equal(request.Name, successResult.Value.Name);
        Assert.Equal(userId, successResult.Value.CreatedBy);
        Assert.False(string.IsNullOrEmpty(successResult.Value.Color));
        Assert.False(string.IsNullOrEmpty(successResult.Value.BackgroundColor));
        Assert.Equal(addedProject, successResult.Value);

        _mockProjectRepository.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetOwnProjectsAsync

    [Fact]
    public async Task GetOwnProjectsAsync_ShouldReturnProjectsOwnedByUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projects = new List<Project>
            {
                new() { Id = Guid.NewGuid(), Name = "Project 1", CreatedBy = userId },
                new() { Id = Guid.NewGuid(), Name = "Project 2", CreatedBy = userId },
                new() { Id = Guid.NewGuid(), Name = "Other Project", CreatedBy = Guid.NewGuid() }
            };

        _mockProjectRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Project, bool>>>()))
            .ReturnsAsync((Expression<Func<Project, bool>> expr) =>
                projects.Where(expr.Compile()));

        // Act
        var result = await _projectsService.GetOwnProjectsAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<List<Project>>>(result);
        Assert.Equal(2, successResult.Value.Count);
        Assert.All(successResult.Value, p => Assert.Equal(userId, p.CreatedBy));
    }

    [Fact]
    public async Task GetOwnProjectsAsync_ShouldReturnEmptyListWhenNoProjects()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockProjectRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Project, bool>>>()))
            .ReturnsAsync([]);

        // Act
        var result = await _projectsService.GetOwnProjectsAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<List<Project>>>(result);
        Assert.Empty(successResult.Value);
    }

    #endregion

    #region GetProjectsAsCollaboratorAsync

    [Fact]
    public async Task GetProjectsAsCollaboratorAsync_ShouldReturnSharedProjects()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();

        var projects = new List<Project>
            {
                new() { Id = projectId1, Name = "Shared Project 1" },
                new() { Id = projectId2, Name = "Shared Project 2" },
                new() { Id = Guid.NewGuid(), Name = "Not Shared Project" }
            };

        var collaborators = new List<Collaborator>
            {
                new() { ProjectId = projectId1, CollaboratorId = userId },
                new() { ProjectId = projectId2, CollaboratorId = userId }
            };

        var projectsQueryable = new TestAsyncEnumerable<Project>(projects);
        var collaboratorsQueryable = new TestAsyncEnumerable<Collaborator>(collaborators);

        _mockProjectRepository.Setup(r => r.Query()).Returns(projectsQueryable);
        _mockCollaboratorRepository.Setup(r => r.Query()).Returns(collaboratorsQueryable);

        // Act
        var result = await _projectsService.GetProjectsAsCollaboratorAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<List<Project>>>(result);
        Assert.Equal(2, successResult.Value.Count);
        Assert.Contains(successResult.Value, p => p.Id == projectId1);
        Assert.Contains(successResult.Value, p => p.Id == projectId2);
    }

    [Fact]
    public async Task GetProjectsAsCollaboratorAsync_ShouldReturnEmptyListWhenNoSharedProjects()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var projects = new List<Project>();
        var collaborators = new List<Collaborator>();

        // Arrange
        var projectsQueryable = new TestAsyncEnumerable<Project>(projects);
        var collaboratorsQueryable = new TestAsyncEnumerable<Collaborator>(collaborators);

        _mockProjectRepository.Setup(r => r.Query()).Returns(projectsQueryable);
        _mockCollaboratorRepository.Setup(r => r.Query()).Returns(collaboratorsQueryable);

        // Act
        var result = await _projectsService.GetProjectsAsCollaboratorAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<List<Project>>>(result);
        Assert.Empty(successResult.Value);
    }

    #endregion

    #region UpdateProjectNameAsync

    [Fact]
    public async Task UpdateProjectNameAsync_WithValidId_ShouldUpdateNameAndReturnSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new CreateUpdateProjectRequest { Name = "Updated Project Name" };
        var project = new Project { Id = projectId, Name = "Original Name" };

        _mockProjectRepository.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _mockProjectRepository.Setup(r => r.Update(It.IsAny<Project>()));
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _projectsService.UpdateProjectNameAsync(projectId, request);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Contains(projectId.ToString(), successResult.Value);
        Assert.Equal(request.Name, project.Name);

        _mockProjectRepository.Verify(r => r.Update(It.Is<Project>(p => p.Id == projectId && p.Name == request.Name)), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProjectNameAsync_WithInvalidId_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new CreateUpdateProjectRequest { Name = "Updated Project Name" };

        _mockProjectRepository.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _projectsService.UpdateProjectNameAsync(projectId, request);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);

        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region DeleteProjectAsync

    [Fact]
    public async Task DeleteProjectAsync_WithValidId_ShouldDeleteAndReturnSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Name = "Project to delete" };

        _mockProjectRepository.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(project);

        _mockProjectRepository.Setup(r => r.Remove(project));
        _mockUnitOfWork.Setup(uow => uow.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _projectsService.DeleteProjectAsync(projectId);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Contains(projectId.ToString(), successResult.Value);

        _mockProjectRepository.Verify(r => r.Remove(It.Is<Project>(p => p.Id == projectId)), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteProjectAsync_WithInvalidId_ShouldReturnNotFoundFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _mockProjectRepository.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _projectsService.DeleteProjectAsync(projectId);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal(404, failureResult.Error.StatusCode);

        _mockProjectRepository.Verify(r => r.Remove(It.IsAny<Project>()), Times.Never);
        _mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(), Times.Never);
    }

    #endregion
}
