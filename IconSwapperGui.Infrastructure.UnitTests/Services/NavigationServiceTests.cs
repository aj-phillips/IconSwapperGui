using Microsoft.Extensions.DependencyInjection;
using IconSwapperGui.Infrastructure.Services;

namespace IconSwapperGui.Infrastructure.UnitTests.Services;

public class DummyViewModel
{
}

[TestFixture]
public class NavigationServiceTests
{
    [Test]
    public void NavigateTo_ResolvesViewModelAndSetsCurrentView()
    {
        var services = new ServiceCollection();
        services.AddTransient<DummyViewModel>();
        var provider = services.BuildServiceProvider();

        var nav = new NavigationService(provider);

        var changed = false;
        nav.CurrentViewChanged += () => changed = true;

        nav.NavigateTo<DummyViewModel>();

        Assert.Multiple(() =>
        {
            Assert.That(nav.CurrentView, Is.TypeOf<DummyViewModel>());
            Assert.That(changed, Is.True);
        });
    }
}