using System.Collections.ObjectModel;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Swapper;
using IconSwapperGui.UI.Services;
using IconSwapperGui.UI.ViewModels;
using Moq;

namespace IconSwapperGui.UI.UnitTests.ViewModels;

[TestFixture]
public sealed class ConverterViewModelTests
{
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "converter", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public async Task AddConverterImages_WhenImageIsAdded_EnablesClearQueueCommand()
    {
        var imagePath = Path.Combine(_tempDir, "image.png");
        await File.WriteAllTextAsync(imagePath, "png");
        var fileDialogService = new Mock<IFileDialogService>();
        fileDialogService
            .Setup(service => service.OpenFiles(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns([imagePath]);

        var viewModel = CreateViewModel(fileDialogService.Object);

        await viewModel.AddConverterImagesCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.Icons, Has.Count.EqualTo(1));
            Assert.That(viewModel.RemoveAllConverterImagesCommand.CanExecute(null), Is.True);
            Assert.That(viewModel.CanConvertImages, Is.True);
        });
    }

    private static ConverterViewModel CreateViewModel(IFileDialogService fileDialogService)
    {
        var settingsService = new Mock<ISettingsService>();
        settingsService.SetupGet(service => service.Settings).Returns(new AppSettings());
        settingsService.Setup(service => service.SaveSettingsAsync()).Returns(Task.CompletedTask);

        var iconManagementService = new Mock<IIconManagementService>();
        iconManagementService
            .Setup(service => service.FilterIcons(It.IsAny<ObservableCollection<Icon>>(), It.IsAny<string?>()))
            .Returns((ObservableCollection<Icon> icons, string? _) => new ObservableCollection<Icon>(icons));

        var dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(service => service.Invoke(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        return new ConverterViewModel(
            settingsService.Object,
            new Mock<INotificationService>().Object,
            new Mock<IIconCreatorService>().Object,
            iconManagementService.Object,
            new Mock<ILoggingService>().Object,
            new Mock<IDialogService>().Object,
            dispatcher.Object,
            fileDialogService,
            new Mock<IWindowService>().Object);
    }
}
