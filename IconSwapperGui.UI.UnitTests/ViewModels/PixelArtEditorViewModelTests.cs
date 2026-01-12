using Moq;
using IconSwapperGui.Core.Interfaces;
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
    private PixelArtExportService _exportService;

    [SetUp]
    public void SetUp()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _loggingServiceMock = new Mock<ILoggingService>();
        
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

    private PixelArtEditorViewModel CreateViewModel()
    {
        return new PixelArtEditorViewModel(
            _notificationServiceMock.Object,
            _settingsServiceMock.Object,
            _loggingServiceMock.Object,
            _exportService
        );
    }
}
