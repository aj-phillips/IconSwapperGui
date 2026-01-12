using Moq;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Settings;
using IconSwapperGui.Infrastructure.Services;

namespace IconSwapperGui.Infrastructure.UnitTests.Services;

[TestFixture]
public class ThemeServiceTests
{
    [Test]
    public void Ctor_SetsCurrentThemeFromSettings()
    {
        var settings = new AppSettings();
        settings.Appearance.Theme = ThemeMode.Dark;

        var mock = new Mock<ISettingsService>();
        mock.SetupGet(s => s.Settings).Returns(settings);

        var svc = new ThemeService(mock.Object);

        Assert.Multiple(() =>
        {
            Assert.That(svc.CurrentTheme, Is.EqualTo(ThemeMode.Dark));
            Assert.That(svc.IsDarkMode, Is.True);
        });
    }

    [Test]
    public void ApplyTheme_ChangesThemeAndFiresEvent()
    {
        var settings = new AppSettings();
        var mock = new Mock<ISettingsService>();
        mock.SetupGet(s => s.Settings).Returns(settings);

        var svc = new ThemeService(mock.Object);

        var called = false;
        svc.ThemeChanged += () => called = true;

        svc.ApplyTheme(ThemeMode.Dark);

        Assert.Multiple(() =>
        {
            Assert.That(svc.CurrentTheme, Is.EqualTo(ThemeMode.Dark));
            Assert.That(called, Is.True);
        });
    }

    [Test]
    public void ToggleTheme_TogglesBetweenLightAndDark()
    {
        var settings = new AppSettings();
        settings.Appearance.Theme = ThemeMode.Light;
        var mock = new Mock<ISettingsService>();

        mock.SetupGet(s => s.Settings).Returns(settings);

        var svc = new ThemeService(mock.Object);

        svc.ToggleTheme();
        Assert.That(svc.CurrentTheme, Is.EqualTo(ThemeMode.Dark));

        svc.ToggleTheme();
        Assert.That(svc.CurrentTheme, Is.EqualTo(ThemeMode.Light));
    }
}