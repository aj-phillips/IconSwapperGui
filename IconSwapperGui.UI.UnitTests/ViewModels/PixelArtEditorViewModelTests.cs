using Moq;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.PixelArt;
using IconSwapperGui.UI.ViewModels;
using IconSwapperGui.UI.Services.PixelArtEditor;
using System.Windows.Media;

namespace IconSwapperGui.UI.UnitTests.ViewModels;

[TestFixture]
public class PixelArtEditorViewModelTests
{
    private Mock<INotificationService> _notificationServiceMock;
    private Mock<ISettingsService> _settingsServiceMock;
    private Mock<ILoggingService> _loggingServiceMock;
    private Mock<IFileDialogService> _fileDialogServiceMock;
    private PixelArtExportService _exportService;

    [SetUp]
    public void SetUp()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _loggingServiceMock = new Mock<ILoggingService>();
        _fileDialogServiceMock = new Mock<IFileDialogService>();
        _settingsServiceMock.SetupGet(s => s.Settings).Returns(new AppSettings());
        _settingsServiceMock.Setup(s => s.SaveSettingsAsync()).Returns(Task.CompletedTask);
        
        var iconCreatorServiceMock = new Mock<IIconCreatorService>();
        _exportService = new PixelArtExportService(iconCreatorServiceMock.Object);
    }

    [Test]
    public void ApplyLayout_WithSmallCanvasSize_AllowsToolsToWork()
    {
        var vm = CreateViewModel();

        vm.Rows = 10;
        vm.Columns = 10;
        vm.ApplyLayoutCommand.Execute(null);

        Assert.That(vm.Layers.Count, Is.EqualTo(1));
        Assert.That(vm.Layers[0].PixelsArgb.Length, Is.EqualTo(100));
        Assert.That(vm.PixelsArgb.Length, Is.EqualTo(100));
    }

    [Test]
    public void ApplyLayout_WithCurrentCanvasSize_DoesNotCreateUndoEntry()
    {
        var vm = CreateViewModel();

        vm.ApplyLayoutCommand.Execute(null);

        Assert.That(vm.UndoCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void ApplyLayout_WithSmallCanvasSize_ToolsCanDrawPixels()
    {
        var vm = CreateViewModel();

        vm.Rows = 10;
        vm.Columns = 10;
        vm.ApplyLayoutCommand.Execute(null);
        vm.SelectedColor = Colors.Red;

        vm.BeginStroke(0);

        var layerPixels = vm.Layers[0].PixelsArgb;
        Assert.That(layerPixels[0], Is.Not.EqualTo(0u));
    }

    [Test]
    public void ApplyLayout_WithLargeCanvasSize_AllowsToolsToWork()
    {
        var vm = CreateViewModel();

        vm.Rows = 256;
        vm.Columns = 256;
        vm.ApplyLayoutCommand.Execute(null);

        Assert.That(vm.Layers.Count, Is.EqualTo(1));
        Assert.That(vm.Layers[0].PixelsArgb.Length, Is.EqualTo(65536));
        Assert.That(vm.PixelsArgb.Length, Is.EqualTo(65536));
    }

    [Test]
    public void ApplyLayout_WithZeroRows_DoesNotApplyLayout()
    {
        var vm = CreateViewModel();

        vm.Rows = 0;
        vm.Columns = 10;
        vm.ApplyLayoutCommand.Execute(null);

        Assert.That(vm.Layers[0].PixelsArgb.Length, Is.EqualTo(1024));
    }

    [Test]
    public void ApplyLayout_WithZeroColumns_DoesNotApplyLayout()
    {
        var vm = CreateViewModel();

        vm.Rows = 10;
        vm.Columns = 0;
        vm.ApplyLayoutCommand.Execute(null);

        Assert.That(vm.Layers[0].PixelsArgb.Length, Is.EqualTo(1024));
    }

    [Test]
    public void ApplyLayout_WithNegativeRows_DoesNotApplyLayout()
    {
        var vm = CreateViewModel();

        vm.Rows = -10;
        vm.Columns = 10;
        vm.ApplyLayoutCommand.Execute(null);

        Assert.That(vm.Layers[0].PixelsArgb.Length, Is.EqualTo(1024));
    }

    [TestCase(1, 1)]
    [TestCase(5, 5)]
    [TestCase(10, 10)]
    [TestCase(32, 32)]
    [TestCase(64, 64)]
    [TestCase(96, 96)]
    [TestCase(128, 128)]
    [TestCase(256, 256)]
    [TestCase(512, 512)]
    public void ApplyLayout_WithVariousCanvasSizes_ResizesLayersCorrectly(int rows, int columns)
    {
        var vm = CreateViewModel();

        vm.Rows = rows;
        vm.Columns = columns;
        vm.ApplyLayoutCommand.Execute(null);

        var expectedLength = rows * columns;
        Assert.That(vm.Layers[0].PixelsArgb.Length, Is.EqualTo(expectedLength));
        Assert.That(vm.PixelsArgb.Length, Is.EqualTo(expectedLength));
    }

    [Test]
    public void ApplyLayout_WithMultipleLayers_ResizesAllLayers()
    {
        var vm = CreateViewModel();

        vm.AddLayerCommand.Execute(null);
        vm.AddLayerCommand.Execute(null);

        vm.Rows = 10;
        vm.Columns = 10;
        vm.ApplyLayoutCommand.Execute(null);

        Assert.That(vm.Layers.Count, Is.EqualTo(3));
        Assert.That(vm.Layers[0].PixelsArgb.Length, Is.EqualTo(100));
        Assert.That(vm.Layers[1].PixelsArgb.Length, Is.EqualTo(100));
        Assert.That(vm.Layers[2].PixelsArgb.Length, Is.EqualTo(100));
    }

    [Test]
    public void ExportCommand_WithoutExportFolder_IsDisabled()
    {
        var vm = CreateViewModel();

        Assert.That(vm.ExportCommand.CanExecute(null), Is.False);
        Assert.That(vm.ExportFolderStatus, Is.EqualTo("No export folder selected"));
    }

    [Test]
    public async Task BrowseExportFolder_WithSelection_UpdatesSettingsAndEnablesExport()
    {
        var settings = new AppSettings();
        _settingsServiceMock.SetupGet(s => s.Settings).Returns(settings);
        _fileDialogServiceMock
            .Setup(s => s.BrowseForFolder(It.IsAny<string?>(), "Select pixel art export folder"))
            .Returns(@"C:\Icons\Exports");

        var vm = CreateViewModel();

        await vm.BrowseExportFolderCommand.ExecuteAsync(null);

        Assert.That(vm.ExportFolder, Is.EqualTo(@"C:\Icons\Exports"));
        Assert.That(settings.Application.ExportLocation, Is.EqualTo(@"C:\Icons\Exports"));
        Assert.That(vm.ExportCommand.CanExecute(null), Is.True);
        Assert.That(vm.ExportFolderActionText, Is.EqualTo("Change Folder"));
        _settingsServiceMock.Verify(s => s.SaveSettingsAsync(), Times.Once);
    }

    [Test]
    public void ExportFileSummary_UsesDocumentName()
    {
        var vm = CreateViewModel();

        vm.DocumentName = "My Icon";

        Assert.That(vm.ExportFileSummary, Is.EqualTo("My Icon.png + My Icon.ico"));
    }

    [Test]
    public void UndoRedo_AfterDrawing_RestoresPixelState()
    {
        var vm = CreateViewModel();
        vm.Rows = 4;
        vm.Columns = 4;
        vm.ApplyLayoutCommand.Execute(null);
        vm.SelectedColor = Colors.Red;

        vm.BeginStroke(0);
        vm.EndStroke();

        Assert.That(vm.Layers[0].PixelsArgb[0], Is.EqualTo(ToArgb(Colors.Red)));
        Assert.That(vm.UndoCommand.CanExecute(null), Is.True);

        vm.UndoCommand.Execute(null);

        Assert.That(vm.Layers[0].PixelsArgb[0], Is.EqualTo(0u));
        Assert.That(vm.RedoCommand.CanExecute(null), Is.True);

        vm.RedoCommand.Execute(null);

        Assert.That(vm.Layers[0].PixelsArgb[0], Is.EqualTo(ToArgb(Colors.Red)));
    }

    [Test]
    public void Eraser_ClearsLayerPixelToTransparent()
    {
        var vm = CreateViewModel();
        vm.Rows = 4;
        vm.Columns = 4;
        vm.ApplyLayoutCommand.Execute(null);
        vm.SelectedColor = Colors.Red;

        vm.BeginStroke(0);
        vm.EndStroke();
        vm.SelectedTool = PixelTool.Eraser;
        vm.BeginStroke(0);
        vm.EndStroke();

        Assert.That(vm.Layers[0].PixelsArgb[0], Is.EqualTo(0u));
    }

    [Test]
    public void NewCommand_ClearsLayerPixelsAndCanUndo()
    {
        var vm = CreateViewModel();
        vm.Rows = 4;
        vm.Columns = 4;
        vm.ApplyLayoutCommand.Execute(null);
        vm.SelectedColor = Colors.Red;
        vm.BeginStroke(0);
        vm.EndStroke();

        vm.NewCommand.Execute(null);

        Assert.That(vm.Layers[0].PixelsArgb.All(pixel => pixel == 0u), Is.True);

        vm.UndoCommand.Execute(null);

        Assert.That(vm.Layers[0].PixelsArgb[0], Is.EqualTo(ToArgb(Colors.Red)));
    }

    private PixelArtEditorViewModel CreateViewModel()
    {
        return new PixelArtEditorViewModel(
            _notificationServiceMock.Object,
            _settingsServiceMock.Object,
            _loggingServiceMock.Object,
            _fileDialogServiceMock.Object,
            _exportService
        );
    }

    private static uint ToArgb(Color c) => (uint)(c.A << 24 | c.R << 16 | c.G << 8 | c.B);
}
