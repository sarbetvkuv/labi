using System;

namespace Lab1.Task2;

public interface IButton
{
    void Render();
}

public interface ITextBox
{
    void Render();
}

public interface IWidgetFactory
{
    IButton CreateButton();
    ITextBox CreateTextBox();
}

public sealed class LightButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Button: light theme, color = white background / black text");
    }
}

public sealed class DarkButton : IButton
{
    public void Render()
    {
        Console.WriteLine("Button: dark theme, color = black background / white text");
    }
}

public sealed class LightTextBox : ITextBox
{
    public void Render()
    {
        Console.WriteLine("TextBox: light theme, border = gray");
    }
}

public sealed class DarkTextBox : ITextBox
{
    public void Render()
    {
        Console.WriteLine("TextBox: dark theme, border = light gray");
    }
}

public sealed class LightWidgetFactory : IWidgetFactory
{
    public IButton CreateButton() => new LightButton();

    public ITextBox CreateTextBox() => new LightTextBox();
}

public sealed class DarkWidgetFactory : IWidgetFactory
{
    public IButton CreateButton() => new DarkButton();

    public ITextBox CreateTextBox() => new DarkTextBox();
}

public sealed class UiScreen
{
    private readonly IButton _button;
    private readonly ITextBox _textBox;

    public UiScreen(IWidgetFactory factory)
    {
        _button = factory.CreateButton();
        _textBox = factory.CreateTextBox();
    }

    public void Render()
    {
        _button.Render();
        _textBox.Render();
    }
}

public static class Lab1Task2Program
{
    public static void Main()
    {
        Console.WriteLine("=== Lab 1 / Task 2 (Abstract Factory) ===");

        IWidgetFactory lightFactory = new LightWidgetFactory();
        IWidgetFactory darkFactory = new DarkWidgetFactory();

        Console.WriteLine("Light UI:");
        new UiScreen(lightFactory).Render();

        Console.WriteLine();
        Console.WriteLine("Dark UI:");
        new UiScreen(darkFactory).Render();
    }
}
