using Microsoft.AspNetCore.SignalR;
using Moq;
using NojectServer.Hubs;
using NojectServer.Models;
using NojectServer.Repositories.Interfaces;
using NojectServer.Repositories.UnitOfWork;
using NojectServer.Services.Collaborators.Implementations;
using NojectServer.Tests.MockHelpers.AsyncQuerySupport;
using NojectServer.Utils.ResultPattern;
using System.Linq.Expressions;
using Task = System.Threading.Tasks.Task;

namespace NojectServer.Tests.Services.Collaborators;

public class CollaboratorsServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CollaboratorsService _collaboratorsService;
    private readonly Mock<IHubContext<SharedProjectsHub>> _mockHubContext;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<ICollaboratorRepository> _mockCollaboratorRepository;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly List<User> _usersList;
    private readonly List<Collaborator> _collaboratorsList;
    private readonly List<Project> _projectsList;

    public CollaboratorsServiceTests()
    {
        // Initialize test data
        _usersList =
        [
            new() { Id = new Guid("00000000-0000-0000-0000-000000000001"), Email = "owner@example.com", FullName = "Project Owner" },
            new() { Id = new Guid("00000000-0000-0000-0000-000000000002"), Email = "user1@example.com", FullName = "User One" },
            new() { Id = new Guid("00000000-0000-0000-0000-000000000003"), Email = "user2@example.com", FullName = "User Two" },
            new() { Id = new Guid("00000000-0000-0000-0000-000000000004"), Email = "user3@example.com", FullName = "User Three" }
        ];

        var projectId = Guid.NewGuid();
        _projectsList =
        [
            new() { Id = projectId, Name = "Test Project", CreatedBy = new Guid("00000000-0000-0000-0000-000000000001") }
        ];

        _collaboratorsList =
        [
            new() { ProjectId = projectId, CollaboratorId = new Guid("00000000-0000-0000-0000-000000000002") }
        ];

        // Set up mock DbSets
        _mockUserRepository = new Mock<IUserRepository>();
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockCollaboratorRepository = new Mock<ICollaboratorRepository>();

        // Configure User repository
        _mockUserRepository.Setup(r => r.GetByEmailAsync("owner@example.com"))
            .ReturnsAsync(_usersList[0]);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("user1@example.com"))
            .ReturnsAsync(_usersList[1]);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("user2@example.com"))
            .ReturnsAsync(_usersList[2]);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("user3@example.com"))
            .ReturnsAsync(_usersList[3]);
        _mockUserRepository.Setup(r => r.GetByEmailAsync("nonexistent@example.com"))
            .ReturnsAsync((User?)null);

        // Configure Project repository
        _mockProjectRepository.Setup(r => r.GetByIdAsync(projectId.ToString()))
            .ReturnsAsync(_projectsList[0]);

        // Configure Collaborator repository
        _mockCollaboratorRepository.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Collaborator, bool>>>()))
            .ReturnsAsync(_collaboratorsList);
        _mockCollaboratorRepository.Setup(r => r.AnyAsync(It.Is<Expression<Func<Collaborator, bool>>>(
            expr => expr.Compile().Invoke(new Collaborator
            {
                ProjectId = projectId,
                CollaboratorId = new Guid("00000000-0000-0000-0000-000000000002")
            }))))
            .ReturnsAsync(true);
        _mockCollaboratorRepository.Setup(r => r.AnyAsync(It.Is<Expression<Func<Collaborator, bool>>>(
            expr => expr.Compile().Invoke(new Collaborator
            {
                ProjectId = projectId,
                CollaboratorId = new Guid("00000000-0000-0000-0000-000000000003")
            }))))
            .ReturnsAsync(false);

        // Set up UnitOfWork mock
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.Projects).Returns(_mockProjectRepository.Object);
        _mockUnitOfWork.Setup(u => u.Collaborators).Returns(_mockCollaboratorRepository.Object);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Set up SignalR Hub mock
        _mockClientProxy = new Mock<IClientProxy>();
        _mockHubContext = new Mock<IHubContext<SharedProjectsHub>>();
        _mockHubContext.Setup(h => h.Clients).Returns(new TestHubClients(_mockClientProxy.Object));

        // Create the service to test
        _collaboratorsService = new CollaboratorsService(_mockUnitOfWork.Object, _mockHubContext.Object);
    }

    // This class is used to mock the IHubClients interface
    private class TestHubClients(IClientProxy clientProxy) : IHubClients
    {
        private readonly IClientProxy _clientProxy = clientProxy;

        public IClientProxy All => _clientProxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _clientProxy;
        public IClientProxy Client(string connectionId) => _clientProxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _clientProxy;
        public IClientProxy Group(string groupName) => _clientProxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => _clientProxy;
        // This method handles the string parameter version of Groups that's being used in AddCollaboratorAsync
        public IClientProxy Groups() => _clientProxy;
        public IClientProxy Others => _clientProxy;
        public IClientProxy OthersInGroup() => _clientProxy;
        public IClientProxy User(string userId) => _clientProxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => _clientProxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _clientProxy;
    }

    #region AddCollaborator Tests

    [Fact]
    public async Task AddCollaboratorAsync_WithOwnerEmail_ShouldReturnFailureAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var ownerEmail = "owner@example.com";

        // Act
        var result = await _collaboratorsService.AddCollaboratorAsync(projectId, ownerEmail);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("ValidationError", failureResult.Error.Error);
        Assert.Equal("You cannot add yourself as collaborator", failureResult.Error.Message);
    }

    [Fact]
    public async Task AddCollaboratorAsync_WithNonExistentUser_ShouldReturnNotFoundAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var nonExistentUserEmail = "nonexistent@example.com";

        // Act
        var result = await _collaboratorsService.AddCollaboratorAsync(projectId, nonExistentUserEmail);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal("The specified user doesn't exist", failureResult.Error.Message);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task AddCollaboratorAsync_WithExistingCollaborator_ShouldReturnConflictAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var existingCollaboratorEmail = "user1@example.com";

        // Act
        var result = await _collaboratorsService.AddCollaboratorAsync(projectId, existingCollaboratorEmail);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("Conflict", failureResult.Error.Error);
        Assert.Equal("This collaborator is already associated with this project", failureResult.Error.Message);
        Assert.Equal(409, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task AddCollaboratorAsync_WithValidInput_ShouldAddCollaboratorAndNotifyUserAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var newCollaboratorId = new Guid("00000000-0000-0000-0000-000000000003");
        var newCollaboratorEmail = "user2@example.com";

        // Act
        var result = await _collaboratorsService.AddCollaboratorAsync(projectId, newCollaboratorEmail);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal($"Successfully added {newCollaboratorEmail} as a collaborator", successResult.Value);

        // Verify collaborator was added to database
        _mockCollaboratorRepository.Verify(r => r.AddAsync(It.Is<Collaborator>(
            c => c.ProjectId == projectId && c.CollaboratorId == newCollaboratorId
        )), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

        // WE DO NOT verify SignalR notification here as SendAsync is an extension method
        // We're only checking that the method completes successfully
        //_mockHubContext.Verify(h => h.Clients.Groups(newCollaboratorEmail).SendAsync(
        //    "NewSharedProject",
        //    It.IsAny<object>(),
        //    It.IsAny<object>(),
        //    default
        //), Times.Once);
    }

    #endregion

    #region SearchCollaborators Tests

    [Fact]
    public async Task SearchCollaboratorsAsync_ShouldReturnNonCollaboratorsMatchingPrefixAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var searchPrefix = "user";

        // Arrange
        var usersQueryable = new TestAsyncEnumerable<User>(_usersList);
        var collaboratorsQueryable = new TestAsyncEnumerable<Collaborator>(_collaboratorsList);

        _mockUserRepository.Setup(r => r.Query()).Returns(usersQueryable);
        _mockCollaboratorRepository.Setup(r => r.Query()).Returns(collaboratorsQueryable);

        // Act
        var result = await _collaboratorsService.SearchCollaboratorsAsync(projectId, searchPrefix);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<List<string>>>(result);
        Assert.Collection(successResult.Value,
            item => Assert.Equal("user2@example.com", item),
            item => Assert.Equal("user3@example.com", item)
        );
    }

    [Fact]
    public async Task SearchCollaboratorsAsync_ProjectNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();
        _mockProjectRepository.Setup(r => r.GetByIdAsync(nonExistentProjectId.ToString()))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _collaboratorsService.SearchCollaboratorsAsync(nonExistentProjectId, "user");

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<List<string>>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal("The specified project doesn't exist", failureResult.Error.Message);
    }

    #endregion

    #region GetAllCollaborators Tests

    [Fact]
    public async Task GetAllCollaboratorsAsync_ShouldReturnAllCollaboratorsAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var collaboratorId = new Guid("00000000-0000-0000-0000-000000000002");

        _mockCollaboratorRepository.Setup(r => r.FindAsync(
            It.IsAny<Expression<Func<Collaborator, bool>>>()))
            .ReturnsAsync(
            [
                new() { ProjectId = projectId, CollaboratorId = collaboratorId }
            ]);

        // Act
        var result = await _collaboratorsService.GetAllCollaboratorsAsync(projectId);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<List<string>>>(result);
        Assert.Single(successResult.Value);
        Assert.Equal(collaboratorId.ToString(), successResult.Value.First());
    }

    #endregion

    #region RemoveCollaborator Tests

    [Fact]
    public async Task RemoveCollaboratorAsync_WithNonExistentUser_ShouldReturnNotFoundAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var nonExistentUserEmail = "nonexistent@example.com";

        // Act
        var result = await _collaboratorsService.RemoveCollaboratorAsync(projectId, nonExistentUserEmail);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal("The specified user doesn't exist", failureResult.Error.Message);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task RemoveCollaboratorAsync_WithNonExistentCollaborator_ShouldReturnNotFoundAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var userEmail = "user3@example.com";  // User exists but is not a collaborator

        // Set up collaborator search to return empty list
        _mockCollaboratorRepository.Setup(r => r.FindAsync(
            It.Is<Expression<Func<Collaborator, bool>>>(expr =>
                expr.Compile().Invoke(new Collaborator
                {
                    ProjectId = projectId,
                    CollaboratorId = new Guid("00000000-0000-0000-0000-000000000004")
                }))))
            .ReturnsAsync([]);

        // Act
        var result = await _collaboratorsService.RemoveCollaboratorAsync(projectId, userEmail);

        // Assert
        Assert.False(result.IsSuccess);
        var failureResult = Assert.IsType<FailureResult<string>>(result);
        Assert.Equal("NotFound", failureResult.Error.Error);
        Assert.Equal("The specified collaborator doesn't exist", failureResult.Error.Message);
        Assert.Equal(404, failureResult.Error.StatusCode);
    }

    [Fact]
    public async Task RemoveCollaboratorAsync_WithValidCollaborator_ShouldRemoveAndNotifyAsync()
    {
        // Arrange
        var projectId = _projectsList.First().Id;
        var collaboratorEmail = "user1@example.com";
        var collaboratorId = new Guid("00000000-0000-0000-0000-000000000002");

        var collaborator = new Collaborator { ProjectId = projectId, CollaboratorId = collaboratorId };

        _mockCollaboratorRepository.Setup(r => r.FindAsync(
            It.Is<Expression<Func<Collaborator, bool>>>(expr =>
                expr.Compile().Invoke(collaborator))))
            .ReturnsAsync([collaborator]);

        // Act
        var result = await _collaboratorsService.RemoveCollaboratorAsync(projectId, collaboratorEmail);

        // Assert
        Assert.True(result.IsSuccess);
        var successResult = Assert.IsType<SuccessResult<string>>(result);
        Assert.Equal($"Successfully removed {collaboratorEmail} as a collaborator", successResult.Value);

        // Verify collaborator was removed
        _mockCollaboratorRepository.Verify(r => r.Remove(It.Is<Collaborator>(
            c => c.ProjectId == projectId && c.CollaboratorId == collaboratorId)), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);

        // We don't verify SignalR notification since SendAsync is an extension method
        //_mockHubContext.Verify(h => h.Clients.Group(collaboratorToRemove).SendAsync(
        //    "RemovedSharedProject",
        //    It.IsAny<object>(),
        //    It.IsAny<object>(),
        //    default
        //), Times.Once);
    }

    #endregion
}
