using System.Numerics;
using Raylib_cs;

interface IGUI
{
    void Update();
}

class Menu : IGUI
{
    Vector2 position;
    float width;
    int fontSize;
    public List<MenuItem> menuItems = [];
    int index = 0;

    public Menu(Vector2 position, float width, int fontSize)
    {
        this.position = position;
        this.width = width;
        this.fontSize = fontSize;
    }

    public void Update()
    {
        float height = fontSize * menuItems.Count;
        var rect = new Rectangle(position.X, position.Y, width, height);
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Up))
        {
            index--;
            if (index < 0) index = menuItems.Count - 1;
        }
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Down))
        {
            index++;
            if (index >= menuItems.Count) index = 0;
        }
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
        {
            menuItems[index].action();
        }
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.White);
        for (var i = 0; i < menuItems.Count; i++)
        {
            var r = new Rectangle(rect.X, rect.Y + i * fontSize, rect.Width, fontSize);
            if (i == index)
            {
                Raylib.DrawRectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height, Color.Red);
            }
            Raylib.DrawText(menuItems[i].name, (int)rect.X, (int)(rect.Y + i * fontSize), fontSize, Color.Black);
        }
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
    }
}

class TextBox : IGUI
{
    int fontSize;
    Rectangle rect;
    public string text = "";
    public Action? onEnter;

    public TextBox(Rectangle rect, int fontSize)
    {
        this.rect = rect;
        this.fontSize = fontSize;
    }

    public void Update()
    {
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if ((key >= 32) && (key <= 125))
                text += (char)key;

            key = Raylib.GetCharPressed();
        }

        if (RaylibHelper.IsKeyPressed(KeyboardKey.Backspace) && text.Length > 0)
        {
            text = text[..^1];
        }
        if (RaylibHelper.IsKeyPressed(KeyboardKey.Enter))
        {
            onEnter?.Invoke();
        }
        Raylib.DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, Color.RayWhite);
        Raylib.DrawRectangleLinesEx(rect, 2, Color.Black);
        Raylib.DrawText(text, (int)rect.X, (int)rect.Y, fontSize, Color.Black);
    }
}